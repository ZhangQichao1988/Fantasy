//将以下代码绑定到相机上
using UnityEngine;
using System.Collections;

public class LookatScipt : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Quaternion q = Quaternion.identity;
        q.SetLookRotation(Camera.main.transform.forward, Camera.main.transform.up);
        transform.rotation = q;

    }
}