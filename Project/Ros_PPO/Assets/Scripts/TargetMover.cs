using UnityEngine;

public class TargetMover : MonoBehaviour
{
    public void SetRandomPosition()
    {
        Vector3 randomPos = new Vector3(
            Random.Range(-4f, 4f),
            transform.position.y,
            Random.Range(-4f, 4f)
        );
        transform.position = randomPos;
    }

    void Update()
    {
        // 不移动，什么都不写 或者注释掉旧的移动代码
    }
}
