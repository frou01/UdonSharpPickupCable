using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
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
        [FormerlySerializedAs("pinToWire")] public int[] pinToWireMap;

        private int[][] wireToPinsMap;
        public int[][] WireToPinsMap
        {
            get
            {
                if (wireToPinsMap == null)
                {
                    int maxWire = -1;
                    foreach (int wire in pinToWireMap)
                    {
                        if (wire > maxWire) maxWire = wire;
                    }
                    wireToPinsMap = new int[maxWire + 1][];
                    for (int i = 0; i < wireToPinsMap.Length; i++)
                    {
                        wireToPinsMap[i] = new int[0];
                    }
                    for (int pin = 0; pin < pinToWireMap.Length; pin++)
                    {
                        int wire = pinToWireMap[pin];
                        if (wire >= 0)
                        {
                            int[] oldArray = wireToPinsMap[wire];
                            int[] newArray = new int[oldArray.Length + 1];
                            oldArray.CopyTo(newArray, 0);
                            newArray[oldArray.Length] = pin;
                            wireToPinsMap[wire] = newArray;
                        }
                    }
                }
                return wireToPinsMap;
            }
        } 

        private int[] assignedWires;

        /// <summary>
        /// pinToWireに出てくるWireの番号（重複と-1を除く、昇順）。接続・切断のときに配り直すWire。
        /// </summary>
        public int[] AssignedWires
        {
            get
            {
                if (assignedWires == null)
                {
                    int[][] pinsOfWires = WireToPinsMap;
                    int assignedCount = 0;
                    for (int wire = 0; wire < pinsOfWires.Length; wire++)
                    {
                        if (pinsOfWires[wire].Length > 0) assignedCount++;
                    }
                    assignedWires = new int[assignedCount];
                    int assignedIdx = 0;
                    for (int wire = 0; wire < pinsOfWires.Length; wire++)
                    {
                        if (pinsOfWires[wire].Length > 0)
                        {
                            assignedWires[assignedIdx] = wire;
                            assignedIdx++;
                        }
                    }
                }
                return assignedWires;
            }
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

            foreach (int wire in this.AssignedWires)
            {
                this.cable.RequestRefresh(wire);
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
            foreach (int wire in this.AssignedWires)
            {
                this.cable.RequestRefresh(wire);
            }
            foreach (int wire in disconnectingTerminal.AssignedWires)
            {
                disconnectingTerminal.cable.RequestRefresh(wire);
            }
        }
    }
}
