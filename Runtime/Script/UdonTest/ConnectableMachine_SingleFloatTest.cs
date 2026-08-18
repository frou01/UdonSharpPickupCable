using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.UdonSharpPickupCable
{
    public class ConnectableMachine_SingleFloatTest : AbstractConnectableMachine
    {
        [SerializeField]float[] myPointer;
        public override void SetPointer(int TerminalIndex, Array pointer)
        {
            myPointer = (float[])pointer;
        }
    }
}