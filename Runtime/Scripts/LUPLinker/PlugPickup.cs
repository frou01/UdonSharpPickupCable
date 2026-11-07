using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.UdonSharpPickupCable
{
    public class PlugPickup : LUPickUpRC_RootChangeable
    {
        [SerializeField] Cable[] wires;

        public Cable[] GetWires()
        {
            return wires;
        }
        [SerializeField] int[] terminalOnWire;
        public int[] GetTerminals()
        {
            return terminalOnWire;
        }
    }
}