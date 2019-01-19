using UnityEngine;
using System.Collections.Generic;

namespace RTS
{
    [System.Serializable]
    public struct PlayerInfo
    {
        FactionInfo choosedFaction;
    }

    [System.Serializable]
    public struct FactionInfo
    {

    }

    [System.Serializable]
    public struct UnitBP
    {
        FactionInfo factionBelonged;
    }

    [DisallowMultipleComponent]
    public class SceneManager : MonoBehaviour
    {
        #region singleton
        [HideInInspector]
        public static SceneManager self;

        public static SceneManager Get()
        {
            return self;
        }

        private void Awake()
        {
            self = this;
        }
        #endregion

        public List<UnitBP> unitBlueprints;

        /*Player and group manage*/

        /*Unit factory*/

        /*Event dispatch*/

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
