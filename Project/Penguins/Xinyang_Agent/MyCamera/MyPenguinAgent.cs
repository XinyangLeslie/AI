using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.Networking;

public class MyPenguinAgent : Agent
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float turnSpeed = 180f;

    [Header("Camera")]
    public Camera sensorCamera;
    public int cameraWidth = 224;
    public int cameraHeight = 224;

    [Header("YOLO Server")]
    public string yoloServerUrl = "http://127.0.0.1:5000/detect";

    private RenderTexture renderTex;
    private Texture2D screenTex;
    private Rigidbody rb;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();

        //renderTex = new RenderTexture(cameraWidth, cameraHeight, 24);
        screenTex = new Texture2D(cameraWidth, cameraHeight, TextureFormat.RGB24, false);
        //sensorCamera.targetTexture = renderTex;
    }

    public override void OnEpisodeBegin()
    {
        // 你自己的重置逻辑
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 无需手动添加视觉输入
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int forward = actions.DiscreteActions[0]; // 0: 停, 1: 前
        int turn = actions.DiscreteActions[1];    // 0: 不转, 1: 左, 2: 右

        if (forward == 1)
            rb.MovePosition(transform.position + transform.forward * moveSpeed * Time.fixedDeltaTime);

        if (turn == 1)
            transform.Rotate(Vector3.up, -turnSpeed * Time.fixedDeltaTime);
        else if (turn == 2)
            transform.Rotate(Vector3.up, turnSpeed * Time.fixedDeltaTime);

        // 小惩罚
        AddReward(-1f / MaxStep);

        // 每隔 10 步执行一次视觉识别
        if (StepCount % 10 == 0)
        {
            StartCoroutine(SendCameraToYolo());
        }
    }

    IEnumerator SendCameraToYolo()
    {
        yield return new WaitForEndOfFrame(); // 等待渲染完成

        RenderTexture.active = renderTex;
        sensorCamera.Render();
        screenTex.ReadPixels(new Rect(0, 0, cameraWidth, cameraHeight), 0, 0);
        screenTex.Apply();
        RenderTexture.active = null;

        byte[] jpgBytes = screenTex.EncodeToJPG();

        UnityWebRequest request = UnityWebRequest.Put(yoloServerUrl, jpgBytes);
        request.method = UnityWebRequest.kHttpVerbPOST;
        request.SetRequestHeader("Content-Type", "application/octet-stream");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            ProcessYoloResponse(json);
        }
        else
        {
            Debug.LogWarning("YOLO 请求失败: " + request.error);
        }
    }

    [System.Serializable]
    public class YoloResult
    {
        public List<List<float>> boxes;
        public List<string> labels;
        public List<float> scores;
    }

    void ProcessYoloResponse(string json)
{
    try
    {
        Debug.Log("YOLO 返回结果 JSON: " + json);
        YoloResult result = JsonUtility.FromJson<YoloResult>(json);

        for (int i = 0; i < result.labels.Count; i++)
        {
            if (result.labels[i] == "fish" && result.scores[i] > 0.5f)
            {
                List<float> box = result.boxes[i]; // [xmin, ymin, xmax, ymax]

                float x_center = (box[0] + box[2]) / 2f;
                float imgWidth = cameraWidth;
                float centerRatio = Mathf.Clamp01(x_center / imgWidth);  // ✅ 建议 3


                if (centerRatio < 0.4f)
                {
                    // 鱼在左边
                    transform.Rotate(Vector3.up, -turnSpeed * Time.fixedDeltaTime);
                    Debug.Log("⬅️ 鱼在左边，向左转");
                }
                else if (centerRatio > 0.6f)
                {
                    // 鱼在右边
                    transform.Rotate(Vector3.up, turnSpeed * Time.fixedDeltaTime);
                    Debug.Log("➡️ 鱼在右边，向右转");
                }
                else
                {
                    // 鱼在中间，向前冲
                    transform.position += transform.forward * moveSpeed * Time.fixedDeltaTime;
                    Debug.Log("⬆️ 鱼在前面，向前走");
                }

                // 给奖励鼓励靠近
                AddReward(0.2f);
                break; // 只处理第一个 fish 即可
            }
        }
    }
    catch (System.Exception e)
    {
        Debug.LogWarning("YOLO JSON 解析失败: " + e.Message);
    }
}


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = Input.GetKey(KeyCode.W) ? 1 : 0;
        discreteActionsOut[1] = Input.GetKey(KeyCode.A) ? 1 :
                                 Input.GetKey(KeyCode.D) ? 2 : 0;
    }
}
