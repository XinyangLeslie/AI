using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MyPenguinAgent : Agent
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float turnSpeed = 180f;

    [Header("Prefabs")]
    public GameObject heartPrefab;
    public GameObject regurgitatedFishPrefab;

    [Header("Camera Sensor Settings")]
    public Camera sensorCamera;
    public int cameraWidth = 256;
    public int cameraHeight = 256;
    public bool grayscale = false;

    private MyPenguinArea myPenguinArea;
    private Rigidbody agentRigidbody;
    private GameObject penguinBaby;
    private bool isFull;

    public override void Initialize()
    {
        base.Initialize();

        if (Display.displays.Length > 1)
        {
            Display.displays[1].Activate();
            Debug.Log("Display 2 activated.");
        }
        else
        {
            Debug.Log("Only one display detected.");
        }

        myPenguinArea = GetComponentInParent<MyPenguinArea>();
        penguinBaby = myPenguinArea.penguinBaby;
        agentRigidbody = GetComponent<Rigidbody>();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Empty for vision-only agent
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float forward = actionBuffers.DiscreteActions[0];
        float turn = 0f;
        if (actionBuffers.DiscreteActions[1] == 1)
        {
            turn = -1f;
        }
        else if (actionBuffers.DiscreteActions[1] == 2)
        {
            turn = 1f;
        }

        agentRigidbody.MovePosition(transform.position + transform.forward * forward * moveSpeed * Time.fixedDeltaTime);
        transform.Rotate(transform.up, turn * turnSpeed * Time.fixedDeltaTime);

        if (MaxStep > 0)
        {
            AddReward(-1f / MaxStep);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        int forwardAction = 0;
        int turnAction = 0;
        if (Input.GetKey(KeyCode.W)) forwardAction = 1;
        if (Input.GetKey(KeyCode.A)) turnAction = 1;
        else if (Input.GetKey(KeyCode.D)) turnAction = 2;
        actionsOut.DiscreteActions.Array[0] = forwardAction;
        actionsOut.DiscreteActions.Array[1] = turnAction;
    }

    public override void OnEpisodeBegin()
    {
        isFull = false;
        myPenguinArea.ResetArea();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("fish"))
        {
            EatFish(collision.gameObject);
        }
        else if (collision.transform.CompareTag("baby"))
        {
            RegurgitateFish();
        }
    }

    private void EatFish(GameObject fishObject)
    {
        if (isFull) return;
        isFull = true;
        myPenguinArea.RemoveSpecificFish(fishObject);
        AddReward(1f);
    }

    private void RegurgitateFish()
    {
        if (!isFull) return;
        isFull = false;
        GameObject regurgitatedFish = Instantiate(regurgitatedFishPrefab);
        regurgitatedFish.transform.parent = transform.parent;
        regurgitatedFish.transform.position = penguinBaby.transform.position;
        Destroy(regurgitatedFish, 4f);

        GameObject heart = Instantiate(heartPrefab);
        heart.transform.parent = transform.parent;
        heart.transform.position = penguinBaby.transform.position + Vector3.up;
        Destroy(heart, 4f);

        AddReward(1f);
        if (myPenguinArea.FishRemaining <= 0)
        {
            EndEpisode();
        }
    }
}
