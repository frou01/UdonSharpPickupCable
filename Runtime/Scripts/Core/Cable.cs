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
        /// Wireごとの配送元用疑似ポインタ（各Wire専用の配列）。Wireの数はこの長さで表す。
        /// </summary>
        [NonSerialized] protected object[] sourcePointersCache;

        /// <summary>
        /// 接続された結果配送された疑似ポインタ（Netの中の誰かのpseudoPointers）
        /// </summary>
        [NonSerialized] protected object[] currentPointers;

        /// <summary>
        /// wireがWriterかどうか。基底はfalse。
        /// </summary>
        public virtual bool IsWriter(int wire)
        {
            return false;
        }


        /// <summary>
        /// Reader/Writer共通 pointerの要素を、このCableのwireのデフォルト値で上書きする。
        /// </summary>
        protected virtual void ResetToDefault(int wire, Array pointer)
        {
            // TODO: 基底では何もしない。繋がる機器を規定するクラスで、pointerの要素をwireのデフォルト値で上書きする。
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
        /// 自身の配送元用ポインタを返す。キャッシュされてなければ生成する。
        /// </summary>
        public object[] _GetSourcePointers()
        {
            if(sourcePointersCache == null)
            {
                sourcePointersCache = CreateSourcePointers();
            }

            return sourcePointersCache;
        }

        protected virtual object[] CreateSourcePointers()
        {
            return new object[0];//基底では空配列を返す。繋がる機器を規定するクラスでポインタを作る。
        }

        /// <summary>
        /// thisのwireのcurrentPointersをpointerにする。Netの全メンバーについて、他のCableから呼ばれるのでpublic。
        /// </summary>
        public void SetCurrentPointer(int wire, object pointer)
        {
            if (currentPointers == null) currentPointers = new object[wire + 1];
            else if (currentPointers.Length <= wire)
            {
                object[] newCurrentPointers = new object[wire + 1];
                currentPointers.CopyTo(newCurrentPointers, 0);
                currentPointers = newCurrentPointers;
            }
            currentPointers[wire] = pointer;
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

            //配送元用ポインタを取得する
            object sourcePointer = null;
            for (int idx = 0; idx < visitedWireTokens.Count; idx++)
            {
                DataList currentWireToken = (DataList)visitedWireTokens[idx];
                Cable currentCable = (Cable)currentWireToken[0].Reference;
                int currentWire = (int)currentWireToken[1];
                object[] sourcePointers = currentCable._GetSourcePointers();
                if (sourcePointers.Length <= currentWire) continue;
                if (sourcePointers[currentWire] == null) continue;
                sourcePointer = sourcePointers[currentWire];
                currentCable.ResetToDefault(currentWire, (Array)sourcePointer);
                break;
            }

            //ポインタを配る。Writerには配った時点で書き込みフック用関数を呼ぶ。
            for (int idx = 0; idx < visitedWireTokens.Count; idx++)
            {
                DataList currentWireToken = (DataList)visitedWireTokens[idx];
                Cable currentCable = (Cable)currentWireToken[0].Reference;
                int currentWire = (int)currentWireToken[1];
                currentCable.SetCurrentPointer(currentWire, sourcePointer);
                if (sourcePointer == null) continue;
                if (currentCable.IsWriter(currentWire)){
                    currentCable.OnNetChangedWriter(currentWire, (Array)sourcePointer);
                }
            }
            //Readerには配り終えて=Writerが書き込みを行った後にフック用関数を呼ぶ。
            if(sourcePointer != null) for (int idx = 0; idx < visitedWireTokens.Count; idx++)
            {
                DataList currentWireToken = (DataList)visitedWireTokens[idx];
                Cable currentCable = (Cable)currentWireToken[0].Reference;
                int currentWire = (int)currentWireToken[1];
                currentCable.SetCurrentPointer(currentWire, sourcePointer);
                if (!currentCable.IsWriter(currentWire))
                {
                    currentCable.OnNetChangedReader(currentWire, (Array)sourcePointer);
                }
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
    }
}
