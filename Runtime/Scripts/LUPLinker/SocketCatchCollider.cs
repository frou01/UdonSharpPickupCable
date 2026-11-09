using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.UdonSharpPickupCable
{
    public
        class SocketCatchCollider : LUP_RC_CatcherCollider
    {
        [SerializeField] Terminal linkedTerminal;
        public override bool validationPickup(LUPickUpRC_RootChangeable pickup)
        {
            PlugPickup plug = pickup.GetComponent<PlugPickup>();
            if (plug && CatchedPickups.Count == 0)
            {
                return true;
            }
            return false;
        }

        public override void PickupEnter(LUPickUpRC_RootChangeable pickup)
        {
            base.PickupEnter(pickup);
            PlugPickup plug = pickup.GetComponent<PlugPickup>();
            this.linkedTerminal.Connect(plug.GetTerminal());
        }
        public override void PickupExit(LUPickUpRC_RootChangeable pickup)
        {
            base.PickupExit(pickup);
            PlugPickup plug = pickup.GetComponent<PlugPickup>();
            this.linkedTerminal._Disconnect();
            plug.GetTerminal()._Disconnect();
        }
    }
}