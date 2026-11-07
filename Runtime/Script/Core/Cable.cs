using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.UdonSharpPickupCable
{
    public
#if !COMPILER_UDONSHARP && UNITY_EDITOR
       abstract
#endif
        class Cable : UdonSharpBehaviour
    {
        /// <summary>
        /// 接続されたワイヤ。indexが端子に対応する。
        /// </summary>
        [SerializeField] protected Cable[] DirectConnectedWireArray;

        /// <summary>
        /// 自身の端子ごとに、相手側の端子を保持する。
        /// </summary>
        [SerializeField] protected int[] TerminalOfConnectedWires;

        /// <summary>
        /// 直接続している機器上の端子インデックス
        /// </summary>
        [SerializeField] protected int[] MachineTerminalIndex;

        private void Start()
        {
            int idx = 0;
            foreach(Cable connectedWire in DirectConnectedWireArray)
            {
                if(connectedWire != null) AddNewWireConnection(connectedWire, idx, TerminalOfConnectedWires[idx]);
                idx++;
            }
        }

        ///<summary>接続するためのメソッド</summary>
        ///<param name="newWire">新規に接続される線</param>
        ///<param name="terminalIndex">端子番号</param>
        public void AddNewWireConnection(Cable newWire, int myTerminalIndex, int theyTerminalIndex)
        {
            SetNewWireConnection(newWire,myTerminalIndex, theyTerminalIndex);
            newWire.SetNewWireConnection(this, theyTerminalIndex, myTerminalIndex);
            UpdateAllConnectedWirePointer();
        }
        private void SetNewWireConnection(Cable newWire, int myTerminalIndex, int theyTerminalIndex)
        {
            AddingNewWire(newWire, myTerminalIndex);

            if (DirectConnectedWireArray[myTerminalIndex] != null)
            {
                RemoveWireConnection(myTerminalIndex, TerminalOfConnectedWires[myTerminalIndex]);
            }
            TerminalOfConnectedWires[myTerminalIndex] = theyTerminalIndex;
            DirectConnectedWireArray[myTerminalIndex] = newWire;
        }


        public void RemoveWireConnection(int myTerminalIndex)
        {
            DirectConnectedWireArray[myTerminalIndex].RemoveWireConnection(TerminalOfConnectedWires[myTerminalIndex],myTerminalIndex);
            RemoveWireConnection(myTerminalIndex, TerminalOfConnectedWires[myTerminalIndex]);
        }
        ///<summary>接続を切る為のメソッド</summary>
        ///<param name="myTerminalIndex">自分側の端子番号</param>
        ///<param name="theyTerminalIndex">相手側の端子番号</param>
        private void RemoveWireConnection(int myTerminalIndex, int theyTerminalIndex)
        {
            RemovingWireConnection(myTerminalIndex, theyTerminalIndex);
            DirectConnectedWireArray[myTerminalIndex] = null;

            UpdateAllConnectedWirePointer();
        }


        /// <summary>
        /// 機器のポインタ置き換えを行う
        /// </summary>
        ///<param name="Pointer">ポインタ</param>
        private void UpdatePointer(System.Array Pointer)
        {
        }

        /// <summary>
        /// 接続されている全てのワイヤを辿り、接続されている機器のポインタを自身の物に置き換える
        /// </summary>
        public void UpdateAllConnectedWirePointer()
        {
            DataList collected = new DataList();
            DataList queue = new DataList();
            AddDirectConnectedWires(in collected,in queue);

            while (queue.Count > 0)
            {
                DataToken dequeuedWire;
                queue.TryGetValue(0, TokenType.Reference, out dequeuedWire);
                queue.RemoveAt(0);
                collected.Add(dequeuedWire);
                ((Cable)dequeuedWire.Reference).AddDirectConnectedWires(in collected, in queue);
            }


            foreach (DataToken val in collected.ToArray())
            {
                Cable val_wire = ((Cable)val.Reference);
                Debug.Log(val_wire.name,this);

                val_wire.UpdatePointer(this.GetMyPointer());
            }
        }

        ///<summary>
        /// 探索処理。自身に直接接続されている線の中から、collected 及び queue に含まれない物を抽出して、queueに追加
        ///</summary>
        ///<param name="collected">既に探索済みのWireのリスト</param>
        ///<param name="queue">次に探索するWireのリスト</param>
        private void AddDirectConnectedWires(in DataList collected, in DataList queue)
        {
            DataList newConnectedWireList = new DataList();
            foreach (Cable ConnectedWire in DirectConnectedWireArray)
            {
                if (ConnectedWire)
                {
                    if (!collected.Contains(ConnectedWire) && !queue.Contains(ConnectedWire))
                    {
                        newConnectedWireList.Add(ConnectedWire);
                    }
                }
            }

            queue.AddRange(newConnectedWireList);
            return;
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        ///<summary>接続前処理を行うためのメソッド</summary>
        ///<param name="newWire">新規に接続される線</param>
        ///<param name="terminalIndex">端子番号</param>
        protected abstract void AddingNewWire(Cable newWire, int terminalIndex);

        ///<summary>切断前処理を行うためのメソッド</summary>
        ///<param name="terminalIndex">自分側の端子番号</param>
        ///<param name="theyTerminalIndex">相手側の端子番号</param>
        protected abstract void RemovingWireConnection(int terminalIndex,int theyTerminalIndex);

        protected abstract void GivePointerToMachine();//TODO 機器ポインタ置き換え処理について、Machineの関数を呼び出すやり方に変更する

        protected abstract void applyPointerToMachine();
        public abstract System.Array GetMyPointer();
#else
        protected virtual void AddingNewWire(Cable newWire, int terminalIndex){
        }
        protected virtual void RemovingWireConnection(int terminalIndex,int theyTerminalIndex){
        }
        protected virtual void GivePointerToMachine()
        {
        }
        protected virtual void applyPointerToMachine()
        {
        }
        public virtual System.Array GetMyPointer()
        {
            return null;
        }
#endif


    }
}