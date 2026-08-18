using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.UdonSharpPickupCable
{
    public
        class SocketCatchCollider : LUP_RC_CatcherCollider
    {
        [SerializeField] AbstractWire[] wires;
        [SerializeField] int[] terminalOnWire;
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
            if (plug)
            {
                int wireIdx = 0;
                foreach (AbstractWire wire in wires)
                {
                    int plugWireIdx = 0;
                    foreach (AbstractWire plugWire in plug.GetWires())
                    {
                        wire.AddNewWireConnection(plugWire, terminalOnWire[wireIdx], plug.GetTerminals()[plugWireIdx]);
                        plugWireIdx++;
                    }
                    wireIdx++;
                }
            }
            //TODO: 同期入れ違いにより複数本入ってしまった場合は、IDの若い方を優先して解決する。
        }
        public override void PickupExit(LUPickUpRC_RootChangeable pickup)
        {
            base.PickupExit(pickup);
            PlugPickup plug = pickup.GetComponent<PlugPickup>();
            if (plug)
            {
                int wireIdx = 0;
                foreach (AbstractWire wire in wires)
                {
                    wire.RemoveWireConnection(terminalOnWire[wireIdx]);
                    wireIdx++;
                }
            }
            //TODO: plugのワイヤと、自身のワイヤを切断する。
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR

#else

#endif
    }
}