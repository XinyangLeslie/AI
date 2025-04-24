using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MyPenguinArea : MonoBehaviour
{
    // 场景中使用的新企鹅代理
    public MyPenguinAgent myPenguinAgent;
    // 宝宝企鹅
    public GameObject penguinBaby;
    // 显示累计奖励的文本
    public TextMeshPro cumulativeRewardText;
    // 鱼 prefab（如果你已将 Fish 改名为 MyFish，确保这里引用的是正确的 prefab）
    public GameObject myFishPrefab;

    private List<GameObject> fishList;

    public void ResetArea()
    {
        RemoveAllFish();
        PlacePenguin();
        PlaceBaby();
        SpawnFish(4, 0.5f);
    }

    // 删除所有鱼
    public void RemoveAllFish()
    {
        if(fishList != null)
        {
            for (int i = 0; i < fishList.Count; i++)
            {
                if (fishList[i] != null)
                {
                    Destroy(fishList[i]);
                }
            }
            fishList.Clear();
        }
        else
        {
            fishList = new List<GameObject>();
        }
    }

    // 移除单个鱼（新添加的方法）
    public void RemoveSpecificFish(GameObject fishObject)
    {
        if(fishList != null && fishList.Contains(fishObject))
        {
            fishList.Remove(fishObject);
            Destroy(fishObject);
        }
    }

    // 在区域内放置企鹅
    public void PlacePenguin()
    {
        Rigidbody rb = myPenguinAgent.GetComponent<Rigidbody>();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        myPenguinAgent.transform.position = ChooseRandomPosition(transform.position, 0f, 360f, 0f, 9f) + Vector3.up * 0.5f;
        myPenguinAgent.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
    }

    // 放置宝宝企鹅
    public void PlaceBaby()
    {
        Rigidbody rb = penguinBaby.GetComponent<Rigidbody>();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        penguinBaby.transform.position = ChooseRandomPosition(transform.position, -45f, 45f, 4f, 9f) + Vector3.up * 0.5f;
        penguinBaby.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    // 生成鱼，并将其存入 fishList
    public void SpawnFish(int count, float fishSpeed)
    {
        if(fishList == null)
        {
            fishList = new List<GameObject>();
        }

        for (int i = 0; i < count; i++)
        {
            GameObject fishObject = Instantiate(myFishPrefab);
            fishObject.transform.position = ChooseRandomPosition(transform.position, 100f, 260f, 2f, 13f) + Vector3.up * 0.5f;
            fishObject.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            fishObject.transform.SetParent(transform);
            fishList.Add(fishObject);
            // 确保你的鱼脚本里有公开 fishSpeed 属性
            fishObject.GetComponent<MyFish>().fishSpeed = fishSpeed;
        }
    }

    // 随机位置生成函数
    public static Vector3 ChooseRandomPosition(Vector3 center, float minAngle, float maxAngle, float minRadius, float maxRadius)
    {
        float radius = (maxRadius > minRadius) ? Random.Range(minRadius, maxRadius) : minRadius;
        float angle = (maxAngle > minAngle) ? Random.Range(minAngle, maxAngle) : minAngle;
        return center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
    }

    private void Start()
    {
        fishList = new List<GameObject>();
        ResetArea();
    }

    private void Update()
    {
        cumulativeRewardText.text = myPenguinAgent.GetCumulativeReward().ToString("0.00");
    }
    public int FishRemaining
    {
        get { return fishList != null ? fishList.Count : 0; }
    }


}
