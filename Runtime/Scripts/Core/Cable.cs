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
        [HideInInspector] public Terminal[] terminals;//Auto setup by buildprocess

        /// <summary>
        /// Wireごとの配送元用疑似ポインタ（各Wire専用の配列）。Wireの数はこの長さで表す。
        /// </summary>
        [NonSerialized] protected object[] sourcePointersCache;

        /// <summary>
        /// 接続された結果配送された疑似ポインタ（Netの中の誰かのSourcePointer）
        /// </summary>
        [NonSerialized] protected object[] currentPointers;

        /// <summary>
        /// Wireごとに、書き込みの通知を受けるReaderのCable。
        /// </summary>
        [NonSerialized] protected DataList perWireListenerWireTokens;//index this.wire, WireToken

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
            // 基底では何もしない。繋がる機器を規定するクラスで、pointerの要素をwireのデフォルト値で上書きする。
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
        /// Reader/Writerへの共通通知。Netが確定したタイミングで呼ばれる。
        /// </summary>
        public virtual void PostNetChanged(int wire)
        {
        }

        /// <summary>
        /// wireがReaderのとき、Writerの書き込みの通知を受けるかどうか。基底はfalse。配り直しのときに読まれる。
        /// </summary>
        public virtual bool WantsWrittenNotification(int wire)
        {
            return false;
        }

        /// <summary>
        /// Readerへの通知。同じNetのWriterがNotifyWrittenを呼んだときに呼ばれる。基底は何もしない。
        /// </summary>
        public virtual void OnWritten(int wire)
        {
        }

        /// <summary>
        /// 継承先のWriterが、疑似ポインタに書き込んだあとに呼ぶ。通知を希望したReaderにOnWrittenを呼ぶ。
        /// </summary>
        [RecursiveMethod]
        protected void NotifyWritten(int wire)
        {
            if (perWireListenerWireTokens == null || perWireListenerWireTokens.Count <= wire) return;
            DataList listenerWireTokens = perWireListenerWireTokens[wire].DataList;
            for (int idx = 0; idx < listenerWireTokens.Count; idx++)
            {
                DataList wireToken = listenerWireTokens[idx].DataList;
                Cable readerCable = (Cable)wireToken[0].Reference;
                int readerWire = wireToken[1].Int;
                readerCable.OnWritten(readerWire);
            }
        }

        /// <summary>
        /// thisのwireの、書き込みの通知を受けるReaderの一覧を差し替える。他のCableから呼ばれるのでpublic。
        /// </summary>
        public void SetWrittenListeners(int wire, DataList listenerWireTokens)
        {
            if (perWireListenerWireTokens == null) perWireListenerWireTokens = new DataList();
            if (perWireListenerWireTokens.Count <= wire)
            {
                for (int idx = perWireListenerWireTokens.Count; idx <= wire; idx++)
                {
                    perWireListenerWireTokens.Add(new DataList());//空のDataListで埋める
                }
            }
            perWireListenerWireTokens[wire] = listenerWireTokens;
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
        /// currentPointersの内容をログに出す。Debug用。hashが同じWireは同じ配列を指している。
        /// 引数がないので、ネットワークイベントで呼ばれないよう_を付けている。
        /// </summary>
        public void _DebugLogCurrentPointers()
        {
            if (currentPointers == null)
            {
                LogUtil.Log(this, nameof(_DebugLogCurrentPointers), "currentPointers is null.");
                return;
            }

            string message = "wires=" + currentPointers.Length;
            for (int wire = 0; wire < currentPointers.Length; wire++)
            {
                message += "\n  wire " + wire + ": ";
                if (currentPointers[wire] == null)
                {
                    message += "null";
                    continue;
                }

                Array pointer = (Array)currentPointers[wire];
                message += "hash=" + pointer.GetHashCode() + " values=[";
                for (int idx = 0; idx < pointer.Length; idx++)
                {
                    if (idx > 0) message += ", ";
                    message += pointer.GetValue(idx);
                }
                message += "]";
            }
            LogUtil.Log(this, nameof(_DebugLogCurrentPointers), message);
        }

        /// <summary>
        /// wireのNetを探索し直して、疑似ポインタを配り直す。
        /// </summary>
        /// <returns>見つけたNetの一覧。要素は[0]がCable、[1]がWire番号のDataList。主にテストと確認用で、使わなくてよい。</returns>
        [RecursiveMethod]
        public DataList RequestRefresh(int wire)
        {
            DataList startWireToken = new DataList();//0: Cable , 1: wire index
            startWireToken.Add(this);
            startWireToken.Add(wire);


            DataList visitedWireTokens = new DataList();
            DataList queuedWireTokens = new DataList();
            DataList readerPointerTokens = new DataList();
            DataList sourcePointerOwnerCableTokens = new DataList();
            DataList listenerWireTokens = new DataList();

            visitedWireTokens.Add(startWireToken);

            queuedWireTokens.Add(startWireToken);
            object nextPointer = null;

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
                    int[] partnerPinToWireMap = partnerTerminal.pinToWireMap;
                    int[][] wireToPinsMap = terminal.WireToPinsMap;
                    if (currentWire >= wireToPinsMap.Length) continue;
                    foreach (int wireCorrespondPin in wireToPinsMap[currentWire])
                    {
                        if (wireCorrespondPin >= partnerPinToWireMap.Length) continue;
                        int partnerWire = partnerPinToWireMap[wireCorrespondPin];
                        if (partnerWire < 0) continue;

                        

                        if (!WireVisitedCheck(visitedWireTokens, partnerTerminal.cable, partnerWire))
                        {
                            DataList newWireToken = new DataList();
                            newWireToken.Add(partnerTerminal.cable);
                            newWireToken.Add(partnerWire);
                            queuedWireTokens.Add(newWireToken);
                            visitedWireTokens.Add(newWireToken);
                        }
                    }
                }

                queuedWireTokens.RemoveAt(0);

                //配送元用ポインタの取得を試行、存在すればReader/Writerに振り分け。
                object[] sourcePointers = currentCable._GetSourcePointers();
                if (sourcePointers.Length <= currentWire) continue;
                if (sourcePointers[currentWire] == null) continue;

                if (nextPointer == null)
                {
                    nextPointer = sourcePointers[currentWire];//最初に見つけた配送元用ポインタを保持、初期化しておく。
                    currentCable.ResetToDefault(currentWire, (Array)nextPointer);
                }
                sourcePointerOwnerCableTokens.Add(currentWireToken);
                if (currentCable.IsWriter(currentWire))
                {
                    currentCable.OnNetChangedWriter(currentWire, (Array)nextPointer);//nextPointerはこの時点で確定しているので、この時点で次に振るポインタに書き込みを実施してもらう
                }
                else
                {
                    readerPointerTokens.Add(currentWireToken);
                }

            }

            //事前に抽出しておいたReaderのNet初期化後読み出し用Hookを呼ぶ。
            for (int idx = 0; idx < readerPointerTokens.Count; idx++)
            {
                DataList currentWireToken = (DataList)readerPointerTokens[idx];
                Cable currentCable = (Cable)currentWireToken[0].Reference;
                int currentWire = (int)currentWireToken[1];
                currentCable.OnNetChangedReader(currentWire, (Array)nextPointer);
                if(currentCable.WantsWrittenNotification(currentWire))
                {
                    listenerWireTokens.Add(currentWireToken);
                }
            }
            //Net全体にポインタを配る。
            for (int idx = 0; idx < visitedWireTokens.Count; idx++)
            {
                DataList currentWireToken = (DataList)visitedWireTokens[idx];
                Cable currentCable = (Cable)currentWireToken[0].Reference;
                int currentWire = (int)currentWireToken[1];
                currentCable.SetCurrentPointer(currentWire, nextPointer);
            }

            //SetWrittenListenersで書き込み通知を受けるWireを配る。これにより、WriterがNotifyWrittenを呼ぶと、それぞれのOnWrittenが呼ばれる。
            for (int idx = 0; idx < sourcePointerOwnerCableTokens.Count; idx++)
            {
                DataList currentWireToken = (DataList)sourcePointerOwnerCableTokens[idx];
                Cable currentCable = (Cable)currentWireToken[0].Reference;
                int currentWire = (int)currentWireToken[1];
                currentCable.SetWrittenListeners(currentWire, listenerWireTokens);
            }

            //Net確定後に処理するためのHookを呼ぶ。これにより、Net確定後の処理をCable側で行える。
            for (int idx = 0; idx < sourcePointerOwnerCableTokens.Count; idx++)
            {
                DataList currentWireToken = (DataList)sourcePointerOwnerCableTokens[idx];
                Cable currentCable = (Cable)currentWireToken[0].Reference;
                int currentWire = (int)currentWireToken[1];
                currentCable.PostNetChanged(currentWire);
            }



            return visitedWireTokens;
        }

        private bool WireVisitedCheck(DataList visitedWireTokens, Cable cable, int wire)
        {
            for(int idx = 0; idx < visitedWireTokens.Count; idx++)
            {
                DataList currentWireToken = (DataList)visitedWireTokens[idx];
                if (currentWireToken.Count == 2)
                {
                    if ((Cable)currentWireToken[0].Reference == cable && currentWireToken[1].Int == wire)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
