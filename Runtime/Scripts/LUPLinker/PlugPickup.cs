using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.UdonSharpPickupCable
{
    public class PlugPickup : LUPickUpRC_RootChangeable
    {
        [SerializeField] Terminal linkedTerminal;

        public Terminal GetTerminal()
        {
            return linkedTerminal;
        }

        public virtual string GetPlugTypeName()
        {
            return null;
        }
    }
}