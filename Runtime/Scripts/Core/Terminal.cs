using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.UdonSharpPickupCable
{
    public class Terminal : UdonSharpBehaviour
    {
        /// <summary>
        /// 親のCable
        /// </summary>
        public Cable cable;

        /// <summary>
        /// Pin→親のCableのWire番号。そのPinがなければ-1。
        /// 1つのWireを複数のPinに割り当ててよい。
        /// Pin番号は相手と向かい合う位置の番号で、相手の同じ番号のPinとつながる（物理的なピン番号とは違うことがある）。
        /// 相手とPinの数が違うときは、相手にない番号のPinは相手がいないものとして扱う。
        /// </summary>
        public int[] pinToWire;

        private int[][] wireToPin;
        public int[][] WireToPins
        {
            get
            {
                if (wireToPin == null)
                {
                    int maxWire = -1;
                    foreach (int wire in pinToWire)
                    {
                        if (wire > maxWire) maxWire = wire;
                    }
                    wireToPin = new int[maxWire + 1][];
                    for (int i = 0; i < wireToPin.Length; i++)
                    {
                        wireToPin[i] = new int[0];
                    }
                    for (int pin = 0; pin < pinToWire.Length; pin++)
                    {
                        int wire = pinToWire[pin];
                        if (wire >= 0)
                        {
                            int[] oldArray = wireToPin[wire];
                            int[] newArray = new int[oldArray.Length + 1];
                            oldArray.CopyTo(newArray, 0);
                            newArray[oldArray.Length] = pin;
                            wireToPin[wire] = newArray;
                        }
                    }
                }
                return wireToPin;
            }
        } 

        private int[] shouldUpdateWires;
        public int[] ShouldUpdateWires
        {
            get
            {
                if (shouldUpdateWires == null)
                {
                    shouldUpdateWires = new int[pinToWire.Length];
                    for (int i = 0; i < pinToWire.Length; i++)
                    {
                        shouldUpdateWires[i] = pinToWire[i];
                    }
                }
                return shouldUpdateWires;
            }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
            set
            {
                shouldUpdateWires = value;
            }
#endif
        }

        /// <summary>
        /// 相手のTerminal（実行時に設定）。必ず両側で対称に持つ。
        /// </summary>
        public Terminal partner;

        void Start()
        {

        }

        /// <summary>
        /// otherとつなぐ。どちらかがすでにつながっていれば、先に切る。
        /// </summary>
        ///<param name="other">相手のTerminal（同じCableのTerminalでもよい）</param>
        public void Connect(Terminal other)
        {
            if (other == null)
            {
                LogUtil.Warning(this, nameof(Connect), "Connection target is null.");
                return;
            }
            if (this.partner)
            {
                this._Disconnect();
            }
            if(other.partner)
            {
                other._Disconnect();
            }
            this.partner = other; other.partner = this;

            foreach (int wire in this.ShouldUpdateWires)
            {
                if (wire >= 0) this.cable.RequestRefresh(wire);
            }
        }

        /// <summary>
        /// 相手との接続を切る。引数がないので、ネットワークイベントで呼ばれないよう_を付けている。
        /// </summary>
        public void _Disconnect()
        {
            if (partner == null) return;
            Terminal disconnectingTerminal = partner;
            this.partner = null;
            disconnectingTerminal.partner = null;
            foreach (int wire in this.ShouldUpdateWires)
            {
                if (wire >= 0) this.cable.RequestRefresh(wire);
            }
            foreach (int wire in disconnectingTerminal.ShouldUpdateWires)
            {
                if (wire >= 0) disconnectingTerminal.cable.RequestRefresh(wire);
            }
        }
    }
}
