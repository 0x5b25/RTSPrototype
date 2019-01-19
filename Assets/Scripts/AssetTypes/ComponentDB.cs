using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEditor;
using UnityEngine;

namespace RTS.AssetTypes
{
    public class ComponentDB:ScriptableObject
    {
        public List<ComponentDBPage> pages = new List<ComponentDBPage>();

    }

    [System.Serializable]
    public class ComponentDBPage
    {
        public string pageName;
        public int DBIndex;
        public List<RTSComponentInfoHolder> componentList = new List<RTSComponentInfoHolder>();
    }

    [System.Serializable]
    public class RTSComponentInfoHolder
    {
        public string ComponentName;
        public GameObject prefab;
    }

}
