using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.UdonSharpPickupCable
{
    public

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        abstract
#endif
        class AbstractConnectableMachine : UdonSharpBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public abstract void SetPointer(int TerminalIndex, System.Array pointer);
#else
        public virtual void SetPointer(int TerminalIndex , System.Array pointer)
        {

        }
#endif
    }
}