using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace RTS.AssetTypes
{
    public class RTSConfig : ScriptableObject
    {

        public List<string> events = new List<string>();
        public List<string> tags = new List<string>();

    }
}