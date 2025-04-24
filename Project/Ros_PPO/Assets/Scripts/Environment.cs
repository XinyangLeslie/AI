using UnityEngine;

public class Environment : MonoBehaviour
{
    public GameObject[] obstacles;
    public Transform obstacleArea;  // 地面或障碍区域父物体
    public TargetMover target;

    public float areaRadius = 5f;

    public void ResetEnvironment()
    {
        // 重置目标位置
        if (target != null)
        {
            target.SetRandomPosition();
        }

        // 随机放置障碍物
        foreach (GameObject obstacle in obstacles)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(-areaRadius, areaRadius),
                0,
                Random.Range(-areaRadius, areaRadius)
            );
            obstacle.transform.localPosition = randomPos;

            float randomRot = Random.Range(0, 360);
            obstacle.transform.localRotation = Quaternion.Euler(0, randomRot, 0);
        }
    }
}
