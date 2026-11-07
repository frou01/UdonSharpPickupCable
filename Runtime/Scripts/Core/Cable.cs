using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.UdonSharpPickupCable
{
    public class Cable : UdonSharpBehaviour
    {
        /// <summary>
        /// このCableのTerminal
        /// </summary>
        public Terminal[] terminals;

        /// <summary>
        /// Wireごとの疑似ポインタ（各Wire専用の配列）。Wireの数はこの長さで表す。
        /// </summary>
        [NonSerialized] protected object[] pseudoPointers;

        /// <summary>
        /// Wireごとのデフォルト値。Netが変わったときに、疑似ポインタの要素をこの値で上書きする。
        /// </summary>
        [NonSerialized] protected object[] defaultValues;

        /// <summary>
        /// Wireごとに、今そのWireのNetが使っている疑似ポインタ（Netの中の誰かのpseudoPointers）
        /// </summary>
        [NonSerialized] protected object[] currentPointers;


        /// <summary>
        /// 初期化でNetを更新したWireのフラグ。Netの中の誰かのpseudoPointersを使うようにしたら、他のCableはもう更新しない。
        /// </summary>
        [NonSerialized] public bool[] pointerSetupFlags;

        /// <summary>
        /// wireがWriterかどうか。基底はfalse。
        /// </summary>
        public virtual bool IsWriter(int wire)
        {
            return false;
        }

        /// <summary>
        /// Writerへの通知。pointerへの参照を差し替えて、すぐ書き込む。基底は何もしない。
        /// </summary>
        public virtual void OnNetChangedWriter(int wire, Array pointer)
        {
        }

        /// <summary>
        /// Readerへの通知。pointerへの参照を差し替える。基底は何もしない。
        /// </summary>
        public virtual void OnNetChangedReader(int wire, Array pointer)
        {
        }

        /// <summary>
        /// wireのNetを探索し直して、疑似ポインタを配り直す。
        /// </summary>
        public DataList RequestRefresh(int wire)
        {
            DataList startWireToken = new DataList();//0: Cable , 1: wire index
            startWireToken.Add(this);
            startWireToken.Add(wire);


            DataList visitedWireTokens = new DataList();
            DataList queuedWireTokens = new DataList();
            visitedWireTokens.Add(startWireToken);
            queuedWireTokens.Add(startWireToken);
            while (queuedWireTokens.Count > 0)
            {
                DataList currentWireToken = (DataList)queuedWireTokens[0];
                Cable currentCable = (Cable)currentWireToken[0].Reference;
                int currentWire = (int)currentWireToken[1];

                //繋がっているWireへアクセスする
                foreach (Terminal terminal in currentCable.terminals)
                {
                    if (terminal.partner == null)continue;
                    Terminal partnerTerminal = terminal.partner;
                    int[] partnerPinToWire = partnerTerminal.pinToWire;
                    int[][] wireToPins = terminal.WireToPins;
                    if (currentWire >= wireToPins.Length) continue;
                    foreach (int wireCorrespondPin in wireToPins[currentWire])
                    {
                        if (wireCorrespondPin >= partnerPinToWire.Length) continue;
                        int partnerWire = partnerPinToWire[wireCorrespondPin];
                        if (partnerWire < 0) continue;

                        DataList newWireToken = new DataList();
                        newWireToken.Add(partnerTerminal.cable);
                        newWireToken.Add(partnerWire);
                        if (!WireVisitedCheck(visitedWireTokens, newWireToken))
                        {
                            queuedWireTokens.Add(newWireToken);
                            visitedWireTokens.Add(newWireToken);
                        }
                    }
                }
                queuedWireTokens.RemoveAt(0);
            }

            return visitedWireTokens;//For Testing
        }

        private bool WireVisitedCheck(DataList visitedWireTokens, DataList wireToken)
        {
            for(int idx = 0; idx < visitedWireTokens.Count; idx++)
            {
                DataList currentWireToken = (DataList)visitedWireTokens[idx];
                if (currentWireToken.Count == 2)
                {
                    if (currentWireToken[0].Reference == wireToken[0].Reference && currentWireToken[1].Int == wireToken[1].Int)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// pointerの要素を、このCableのwireのデフォルト値で上書きする。
        /// </summary>
        private void ResetToDefault(int wire, Array pointer)
        {
            // TODO: defaultValues[wire]をpointerの各要素に書き込む
        }

        /// <summary>
        /// NetのメンバーのうちWriterのものにOnNetChangedWriterを呼ぶ。
        /// </summary>
        private static void NotifyWriters(DataList netCables, DataList netWires, Array pointer)
        {
            // TODO: 各(cable, wire)について、cable.IsWriter(wire)ならcable.OnNetChangedWriter(wire, pointer)
        }

        /// <summary>
        /// NetのメンバーのうちWriterでないものにOnNetChangedReaderを呼ぶ。
        /// </summary>
        private static void NotifyReaders(DataList netCables, DataList netWires, Array pointer)
        {
            // TODO: 各(cable, wire)について、cable.IsWriter(wire)でなければcable.OnNetChangedReader(wire, pointer)
        }

        /// <summary>
        /// thisのwireのcurrentPointersをpointerにする。Netの全メンバーについて、他のCableから呼ばれるのでpublic。
        /// </summary>
        public void SetCurrentPointer(int wire, object pointer)
        {
            // TODO: currentPointers[wire] = pointer
        }
    }
}
