using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using TMPro;

public class PenguinArea : MonoBehaviour
{
    // The agent inside the area
    public PenguinAgent penguinAgent;
    // The baby penguin inside the area
    public GameObject penguinBaby;
    // The text that shows the cumulative reward of the agent
    public TextMeshPro cumulativeRewardText;
    // Prefab of a live fish
    public Fish fishPrefab;

    private List<GameObject> fishList;

    // Reset the area which includes the fish and penguin placement
    public void ResetArea()
    {
        // Make sure no fish are in the area before spawning new fish
        RemoveAllFish();
        PlacePenguin();
        PlaceBaby();
        SpawnFish(4, .5f);
    }

    // Remove a specific fish from the area when it is eaten
    public void RemoveSpecificFish(GameObject fishObject)
    {
        fishList.Remove(fishObject);
        Destroy(fishObject);
    }

    // The number of fish remaining
    public int FishRemaining
    {
        get { return fishList.Count; }
    }

    // Choose a random position within a partial donut shape
    // Radius is distances from the center
    // Angle represents the angles of the wedge
    // Purpose: Use special radius and angle limits to pick a random position within wedges around the central point in the area
    public static Vector3 ChooseRandomPosition(Vector3 center, float minAngle, float maxAngle, float minRadius, float maxRadius)
    {
        float radius = minRadius;
        float angle = minAngle;

        if (maxRadius > minRadius)
        {
            // Pick a random radius
            radius = UnityEngine.Random.Range(minRadius, maxRadius);
        }

        if (maxAngle > minAngle)
        {
            // Pick a random angle
            angle = UnityEngine.Random.Range(minAngle, maxAngle);
        }

        // Center position and forward vector rotated around the Y axis
        return center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
    }

    public void RemoveAllFish()
    {
        for (int i = 0; i < fishList.Count; i++)
        {
            if (fishList[i] != null)
            {
                Destroy(fishList[i]);
            }
        }

        fishList.Clear();

        fishList = new List<GameObject>();
    }

    // Place a penguin in the area
    public void PlacePenguin()
    {
        Rigidbody rigidbody = penguinAgent.GetComponent<Rigidbody>();
        // Prevents the penguin from falling through the ground since unexpected occurrences can come about when training for long periods of time
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        penguinAgent.transform.position = ChooseRandomPosition(transform.position, 0f, 360f, 0f, 9f) + Vector3.up * .5f;
        penguinAgent.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
    }

    // Place the baby in the area
    public void PlaceBaby()
    {
        Rigidbody rigidbody = penguinBaby.GetComponent<Rigidbody>();
        // Prevents the baby penguin from falling through the ground since unexpected occurrences can come about when training for long periods of time
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        penguinBaby.transform.position = ChooseRandomPosition(transform.position, -45f, 45f, 4f, 9f) + Vector3.up * .5f;
        penguinBaby.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    public void SpawnFish(int count, float fishSpeed)
    {
        for (int i = 0; i < count; i++)
        {
            // Spawn and place the fish
            GameObject fishObject = Instantiate<GameObject>(fishPrefab.gameObject);
            fishObject.transform.position = ChooseRandomPosition(transform.position, 100f, 260f, 2f, 13f) + Vector3.up * .5f;
            fishObject.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

            // Set the fish's parent to this area's transform
            fishObject.transform.SetParent(transform);

            // Keep track of the fish
            fishList.Add(fishObject);

            // Set the fish speed
            fishObject.GetComponent<Fish>().fishSpeed = fishSpeed;
        }

    }

    // When game starts
    private void Start()
    {
        fishList = new List<GameObject>();
        ResetArea();
    }

    // Called every frame
    private void Update()
    {
        // Update the reward text to see how well the penguins are performing
        cumulativeRewardText.text = penguinAgent.GetCumulativeReward().ToString("0.00");
    }
}
