using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fish : MonoBehaviour
{
    // Swim speed
    public float fishSpeed;
    
    private float randomizedSpeed = 0f;
    private float nextActionTime = -1f;
    private Vector3 targetPosition;

    // Independent of frame rate which is called every timestep
    // Allow us to interact even when the agent is training at an increased game speed
    private void FixedUpdate() {
        if (fishSpeed > 0f) {
            Swim();
        }
    }

    // Swim between random positions
    private void Swim() {
        // If time for next action, pick new speed and destination, else swim toward the destination
        if (Time.fixedTime >= nextActionTime) {
            // Randomize speed
            randomizedSpeed = fishSpeed * UnityEngine.Random.Range(.5f, 1.5f);

            // Pick a random target position
            targetPosition = PenguinArea.ChooseRandomPosition(transform.parent.position, 100f, 260f, 2f, 13f);

            // Rotate toward the target
            transform.rotation = Quaternion.LookRotation(targetPosition - transform.position, Vector3.up);

            // Calculate the time to get there
            // v = x/t, t = x/v
            float timeToGetThere = Vector3.Distance(transform.position, targetPosition) / randomizedSpeed;
            nextActionTime = Time.fixedTime + timeToGetThere;
        }
        else {
            // Make sure that the fish does not swim past the target
            Vector3 moveVector = randomizedSpeed * transform.forward * Time.fixedDeltaTime;
            
            if (moveVector.magnitude <= Vector3.Distance(transform.position, targetPosition)) {
                transform.position += moveVector;
            }
            else {
                transform.position = targetPosition;
                nextActionTime = Time.fixedTime;
            }
        }
    }
}
