using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class PenguinSimpleAgent : Agent
{
    // How fast the agent moves forward
    public float moveSpeed = 5f;
    // How fast the agent turns
    public float turnSpeed = 180f;
    // Heart prefab that appears when the baby is fed
    public GameObject heartPrefab;
    // Regurgitated fish prefab that appears when the baby is fed
    public GameObject regurgitatedFishPrefab;
    
    private PenguinArea penguinArea;
    new private Rigidbody rigidbody;
    private GameObject baby;
    // Depicts if the penguin has a full stomach
    private bool isFull;

    // When the agent wakes up (enables), not when it resets
    public override void Initialize() {
        base.Initialize();
        penguinArea = GetComponentInParent<PenguinArea>();
        baby = penguinArea.penguinBaby;
        rigidbody = GetComponent<Rigidbody>();
    }

    // When the agent receives and responds to commands. These commands can originate from a neural network or a human player
    // actionBuffers is a struct that contains an array of numerical values that correspond to actions the agent should take
    // Discrete indicates that each numerical value corresponds to a singular action
    // DiscreteActions[0]: 0 or 1 ; 0 to remain in place and 1 to move forward at full speed
    // DiscreteActions[1]: 0, 1, 2 ; 0 to not turn, 1 to turn in the negative direction, and 2 to turn in the positive direction
    // However, neural network has no concept of what these actions do. All it does know that some actions tend to result in more reward points
    // Applies movement, rotation, and adds a small negative reward. The reward encourages the agent to complete its task ASAP
    // Application example: -(steps / 5000). Longer it takes the agent, bigger the (-) value

    // Performs actions based on vector of numbers
    public override void OnActionReceived(ActionBuffers actionBuffers) {
        // Convert the first action to forward movement
        float forwardAmount = actionBuffers.DiscreteActions[0];

        // Convert the second action to turning left or right
        float turnAmount = 0f;

        if (actionBuffers.DiscreteActions[1] == 1f) {
            turnAmount = -1f;
        }
        else if (actionBuffers.DiscreteActions[1] == 2f) {
            turnAmount = 1f;
        }

        // Apply movement
        rigidbody.MovePosition(transform.position + transform.forward * forwardAmount * moveSpeed * Time.fixedDeltaTime);
        transform.Rotate(transform.up * turnAmount * turnSpeed * Time.fixedDeltaTime);

        // Apply a tiny negative reward every step to encourage action
        if (MaxStep > 0) AddReward(-1f / MaxStep);
    }

    // Control the agent without the neural network: Reads inputs from human player via keyboard, and converts and places those actions into the DiscreteActions array
    // Default forwardAction: 0 (0 or 1)
    // Default turnAction: 0 (0, 1, 2)
    // Only called when behavior type is set to 'heuristic only' in Behavior Parameters inspector
    public override void Heuristic(in ActionBuffers actionsOut) {
        int forwardAction = 0;
        int turnAction = 0;

        if (Input.GetKey(KeyCode.W)) {
            // Move forward
            forwardAction = 1;
        }

        if (Input.GetKey(KeyCode.A)) {
            // Turn left
            turnAction = 1;
        }
        else if (Input.GetKey(KeyCode.D)) {
            // Turn right
            turnAction = 2;
        }

        // Assign actions
        actionsOut.DiscreteActions.Array[0] = forwardAction;
        actionsOut.DiscreteActions.Array[1] = turnAction;
    }

    // Called when the agent is done feeding the baby all of the fish or reaches the max number of steps
    // Empty penguin's belly and reset the agent and area
    public override void OnEpisodeBegin() {
        isFull = false; 
        penguinArea.ResetArea();
    }

    /* Penguin agent observes environment through:
    (1) Raycasts: 'Shining a bunch of laser pointers out from the penguin and seeing if they hit anything
    (2) Numerical values: Convert an observation into a list of numbers and add that as an observation for the agent
    Imagine your agent is floating in space blindfolded. What would it need to be told about its environment to make an intelligent decision?
    */
    public override void CollectObservations(VectorSensor sensor) {
        // Whether the penguin has eaten a fish (1 float = 1 value)
        sensor.AddObservation(isFull);

        // Distance to the baby (1 float = 1 value)
        sensor.AddObservation(Vector3.Distance(baby.transform.position, transform.position));

        // Direction to the baby (1 Vector3 = 3 values)
        sensor.AddObservation((baby.transform.position - transform.position).normalized);

        // Direction penguin is facing (1 Vector3 = 3 values)
        sensor.AddObservation(transform.forward);

        // 1 + 1 + 3 + 3 = 8 total values
    }

    // When agent collides with fish or baby
    private void OnCollisionEnter(Collision collision) {
        if (collision.transform.CompareTag("fish")) {
            // Try to eat the fish
            EatFish(collision.gameObject);
        }
        else if (collision.transform.CompareTag("baby")) {
            // Try to feed the baby
            RegurgitateFish();
        }
    }

    // Eat fish assuming penguin does not already have a full stomach. If not, removes the fish from that area and gets a reward
    private void EatFish(GameObject fishObject) {
        if (isFull) return;
        isFull = true;

        penguinArea.RemoveSpecificFish(fishObject);

        AddReward(1f);
    }

    // Regurgitate fish and feed the baby if the agent is full
    private void RegurgitateFish() {
        // Nothing to regurgitate
        if (!isFull) return;
        isFull = false;

        // Spawn regurgitated fish
        GameObject regurgitatedFish = Instantiate<GameObject>(regurgitatedFishPrefab);
        regurgitatedFish.transform.parent = transform.parent;
        regurgitatedFish.transform.position = baby.transform.position;
        Destroy(regurgitatedFish, 4f);

        // Spawn heart
        GameObject heart = Instantiate<GameObject>(heartPrefab);
        heart.transform.parent = transform.parent;
        heart.transform.position = baby.transform.position + Vector3.up;
        Destroy(heart, 4f);

        AddReward(1f);

        if (penguinArea.FishRemaining <= 0) {
            EndEpisode();
        }
    }
}
