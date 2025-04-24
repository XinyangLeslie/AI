using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections;
using TMPro;

public class FourWheelAgent : Agent
{
    // 新增变量（类中定义）
    private float timeSinceTargetSeen = 0f;
    private const float maxTimeNearTarget = 3f;

    private Vector3 lastPosition;
    private float timeStuck = 0f;
    private float stuckThreshold = 3f;      // 若3秒不动，则判定为卡住
    private float minMoveDistance = 0.05f;  // 认为“移动”的最小距离



    private float episodeTimer = 0f;
    public float maxEpisodeTime = 99999f;

    [Header("Text UI")]
    public TextMeshProUGUI infoText;

    [Header("调试控制")]
    public bool pauseAfterEpisode = true;

    private int successCount = 0;
    private int wallHitCount = 0;
    private int obstacleHitCount = 0;

    private float cumulativeReward = 0f;
    private float currentDistance = 0f;

    private bool justPaused = false;
    private bool hasMovedTowardTarget = false;

    [Header("轮子")]
    public Transform leftFrontWheel;
    public Transform leftBackWheel;
    public Transform rightFrontWheel;
    public Transform rightBackWheel;

    [Header("目标和环境")]
    public Transform target;
    public Environment environment;

    [Header("控制参数")]
    public float wheelForce = 50f;
    public float wheelRadius = 0.075f;
    public float trackWidth = 0.4f;

    private Rigidbody rb;
    private Vector3 prevToTarget;

    void Start()
    {
        rb = GetComponent<Rigidbody>();


    }

    private float targetOffsetX = 0f;  // 范围 [-1, 1]，0 表示目标在正前方
    public bool lastTargetSeen = false;

    public void UpdateTargetOffset(float offset, bool seen)
    {
        targetOffsetX = offset;
        lastTargetSeen = seen;
    }





    void OnDrawGizmos()
    {
        if (target == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, target.position);
    }

    private bool collisionEnabled = false;

    private IEnumerator EnableCollisionAfterDelay(float delay)
    {
        collisionEnabled = false;
        yield return new WaitForSeconds(delay);
        collisionEnabled = true;
    }

    public override void OnEpisodeBegin()
    {

        lastPosition = transform.position;
        timeStuck = 0f;

        Debug.Log("🔁 Episode Begin");
        episodeTimer = 0f;
        hasMovedTowardTarget = false;
        Time.timeScale = 1f;
        justPaused = false;
        

        environment.ResetEnvironment();
        transform.position = new Vector3(0f, 0.5f, -0.4f);
        transform.rotation = Quaternion.identity;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        prevToTarget = target.position - transform.position;
        StartCoroutine(EnableCollisionAfterDelay(0.5f)); // 延迟 0.5 秒开启碰撞触发

    }


    public override void OnActionReceived(ActionBuffers actions)
    {
        float forwardSpeed = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
        float turnRate = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        float leftPower = Mathf.Clamp(forwardSpeed - turnRate, 0f, 1f);
        float rightPower = Mathf.Clamp(forwardSpeed + turnRate, 0f, 1f);

        Vector3 leftForce = transform.forward * leftPower * wheelForce;
        Vector3 rightForce = transform.forward * rightPower * wheelForce;

        rb.AddForceAtPosition(leftForce, leftFrontWheel.position, ForceMode.Force);
        rb.AddForceAtPosition(rightForce, rightFrontWheel.position, ForceMode.Force);

        // 轮子可视旋转
        float avgSpeed = (leftPower + rightPower) * 0.5f;
        float deg = avgSpeed / wheelRadius * Mathf.Rad2Deg * Time.deltaTime;
        leftFrontWheel.Rotate(transform.right, deg, Space.World);
        leftBackWheel.Rotate(transform.right, deg, Space.World);
        rightFrontWheel.Rotate(transform.right, deg, Space.World);
        rightBackWheel.Rotate(transform.right, deg, Space.World);

        // === 🎯 奖励函数 ===
        Vector3 toTarget = target.position - transform.position;
        float distNow = toTarget.magnitude;
        float distPrev = prevToTarget.magnitude;
        float distanceDelta = Mathf.Clamp(distPrev - distNow, -1f, 1f);
        float angle = Vector3.Angle(transform.forward, toTarget.normalized);
        float dot = Vector3.Dot(transform.forward, toTarget.normalized);

        // 奖励：靠近目标 + 靠中 + 不偏转 + 惩罚时间 + 惩罚反方向
        float rewardDist = (distanceDelta > 0) ? distanceDelta * 0.5f : 0f;
        float rewardAlign = (1f - Mathf.Abs(targetOffsetX)) * 0.1f;
        float rewardAngle = -Mathf.Clamp01(angle / 90f) * 0.02f;
        float rewardTime = -0.001f;
        float rewardReverse = (dot < 0f) ? -0.05f : 0f;

        AddReward(rewardDist + rewardAlign + rewardAngle + rewardTime + rewardReverse);


        prevToTarget = toTarget;
        episodeTimer += Time.deltaTime;

        // ✅ 3. ✅ 成功靠近目标，终止 episode
        if (distNow < 1.0f && lastTargetSeen && Mathf.Abs(targetOffsetX) < 0.3f)
        {
            successCount++;  // ✅ 加上这一句
            AddReward(+2f);
            Debug.Log("✅ 成功靠近目标，EndEpisode");
            EndEpisode();
            return;
        }


        // ✅ 4. ⏰ 超时
        if (episodeTimer >= maxEpisodeTime)
        {
            AddReward(-1f);
            Debug.Log("⏰ 超时，EndEpisode");
            EndEpisode();
            return;
        }

        // 在 OnActionReceived() 中：如果在目标视野内，但 X 秒内未真正靠近，就终止并惩罚
        if (lastTargetSeen)
        {
            timeSinceTargetSeen += Time.deltaTime;

            // 如果在视野内，但长时间不靠近，强制终止
            if (timeSinceTargetSeen > maxTimeNearTarget && distNow > 1.5f)
            {
                AddReward(-1f);  // 惩罚原地磨蹭
                Debug.Log("❌ 识别到目标但没靠近，强制结束 Episode");

                EndEpisode();
                return;
            }
        }
        else
        {
            timeSinceTargetSeen = 0f; // 看不到目标，计时器清零
        }

        // === 🚫 防卡机制：如果原地太久未移动，强制终止 ===
        float movedDistance = Vector3.Distance(transform.position, lastPosition);

        if (movedDistance < minMoveDistance)
        {
            timeStuck += Time.deltaTime;

            if (timeStuck >= stuckThreshold)
            {
                AddReward(-1f);
                Debug.Log($"🛑 原地卡住超过 {stuckThreshold:F1}s，强制结束 Episode！总奖励: {GetCumulativeReward():F3}");
                EndEpisode();
                return;
            }
        }
        else
        {
            // ✅ 有移动 → 重置计时器和位置
            timeStuck = 0f;
            lastPosition = transform.position;
        }



        // UI 更新（可选）
        cumulativeReward = GetCumulativeReward();
        currentDistance = distNow;
        if (infoText != null)
        {
            infoText.text = $"Distance: {currentDistance:F2}m\n" +
                            $"Reward: {cumulativeReward:F3}\n" +
                            $"Success: {successCount}\n" +
                            $"Hit wall: {wallHitCount}\n" +
                            $"Hit Obstacle: {obstacleHitCount}";
        }
    }




    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Debug.Log("手动模式启动！！");
        var ca = actionsOut.ContinuousActions;
        float move = Input.GetAxis("Vertical");
        float turn = Input.GetAxis("Horizontal");
        ca[0] = move - turn;
        ca[1] = move + turn;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. 加入 YOLO 偏移和可见性
        sensor.AddObservation(targetOffsetX);                   // 目标偏移
        sensor.AddObservation(lastTargetSeen ? 1f : 0f);        // 目标是否可见

        // 2. 加入雷达距离
        float[] angles = { -60f, -30f, 0f, 30f, 60f };
        foreach (float angle in angles)
        {
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, 5f))
                sensor.AddObservation(hit.distance / 5f);
            else
                sensor.AddObservation(1f);
        }

        // 3. 自身朝向 + 速度
        sensor.AddObservation(transform.forward);               // (3)
        sensor.AddObservation(rb.velocity.magnitude / 5f);      // (1)
    }


    private void OnCollisionEnter(Collision col)
    {
        if (!collisionEnabled || justPaused) return;

        string tag = col.collider.tag;

        switch (tag)
        {
            case "Wall":
                wallHitCount++;
                AddReward(-1f);
                Debug.Log($"❌ 撞墙！奖励: {GetCumulativeReward():F3}");
                break;

            case "Obstacle":
                obstacleHitCount++;
                AddReward(-1f);
                Debug.Log($"❌ 撞到障碍物！奖励: {GetCumulativeReward():F3}");
                break;

            case "Target":
                successCount++;
                AddReward(+2f);
                Debug.Log($"✅ 撞到了目标标签物体！奖励: {GetCumulativeReward():F3}");
                break;

            default:
                // 非目标物体不处理
                return;
        }

        // ✅ 撞到目标或障碍后统一终止 episode
        if (pauseAfterEpisode)
        {
            Time.timeScale = 0f;
            justPaused = true;
        }

        EndEpisode();
    }




}
