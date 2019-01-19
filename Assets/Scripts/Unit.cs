using UnityEngine;
using System.Collections;

namespace RTS
{
    enum UnitState
    {
        idle
    }

    enum UnitStrategy
    {
        passive,   //Do nothing unless being instructed to
        defensive, //Return fire when possible(no attack command or main target is not in range) and being attacked
        aggressive //Automatically search nearby enemy and attack those in range
    }

    public class Unit : MonoBehaviour
    {
        

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
