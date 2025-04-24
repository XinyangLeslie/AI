using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyFish : MonoBehaviour
{
    // 鱼的游动速度
    public float fishSpeed;

    private float randomizedSpeed = 0f;
    private float nextActionTime = -1f;
    private Vector3 targetPosition;

    private void FixedUpdate() {
        if (fishSpeed > 0f) {
            Swim();
        }
    }

    private void Swim() {
        if (Time.fixedTime >= nextActionTime) {
            // 随机化速度
            randomizedSpeed = fishSpeed * Random.Range(0.5f, 1.5f);

            // 选取随机目标位置
            targetPosition = MyPenguinArea.ChooseRandomPosition(
                transform.parent.position, 100f, 260f, 2f, 13f
            );

            // 朝向目标旋转
            transform.rotation = Quaternion.LookRotation(targetPosition - transform.position, Vector3.up);

            // 根据距离与速度计算时间
            float timeToGetThere = Vector3.Distance(transform.position, targetPosition) / randomizedSpeed;
            nextActionTime = Time.fixedTime + timeToGetThere;
        }
        else {
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
