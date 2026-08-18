using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.UdonSharpPickupCable
{
    public class Wire_SingleFloatTest : AbstractWire
    {
        float[] myPointer = new float[1];
        protected override void applyPointerToMachine()
        {
        }

        protected override void GivePointerToMachine()
        {
        }

        protected override void AddingNewWire(AbstractWire newWire, int terminalIndex)
        {
        }

        protected override void RemovingWireConnection(int terminalIndexint, int theyTerminalIndex)
        {

        }

        public override System.Array GetMyPointer()
        {
            return myPointer;
        }
    }
}