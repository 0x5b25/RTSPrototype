using UnityEngine;
using System.Collections;

namespace RTS
{
    public enum ModType
    {
        Weapon,Utility,Special,Root
    }

    public class UnitModInfo : ScriptableObject
    {
        public ModType componentType;
    }
}
