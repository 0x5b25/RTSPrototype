using UnityEngine;
using System.Collections;

namespace RTS
{
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour
    {
        [HideInInspector]
        public static UIManager self;

        public static UIManager Get() {
            return self;
        }

        private void Awake()
        {
            self = this;
        }

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
