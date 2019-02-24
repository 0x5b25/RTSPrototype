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
        public List<RTSComponentInfo> componentList = new List<RTSComponentInfo>();
    }



    [System.Serializable]
    public class RTSComponentInfo
    {
        public enum ComponentType
        {
            Root,
            Special,
            Utility,
            Weapon
        }

        public ComponentType type;

        public RTSComponentAimingData aimingData;

        public string name;
        public GameObject prefab;
    }

    public enum ComponentAimingType
    {
        Fixed,
        Turret,
        Omnidirectional
    }

    public class RTSComponentAimingData
    {
        public ComponentAimingType aimingType;
        public float aimingAngle;
        public float horizontalRotationSpeed;
        public float verticalRotationSpeed;
        public object movementMetadata;
    }

}
