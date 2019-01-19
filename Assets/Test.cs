using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

using RTS.AssetTypes;

public class TestInfoHolder
{
    public string someStr;
}

public class Test : MonoBehaviour {

    public GameObject prefab;

    public TestInfoHolder holder;

    public void Init()
    {
        if (holder == null)
        {
            holder = new TestInfoHolder();
        }
        holder.someStr = "This is original info";
 
    }

    public void Inst()
    {
        if(holder == null)
        {
            holder = new TestInfoHolder();
            holder.someStr = "This is original info";
        }
        Instantiate(this.gameObject);
    }

    public void Display()
    {
        Debug.Log(holder.someStr);
    }
}
