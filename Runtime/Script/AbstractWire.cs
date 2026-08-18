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
        class AbstractWire : UdonSharpBehaviour
    {
        /// <summary>
        /// 接続されたワイヤ。indexが端子に対応する。
        /// </summary>
        [SerializeField] protected AbstractWire[] DirectConnectedWireArray;

        /// <summary>
        /// 自身の端子ごとに、相手側の端子を保持する。
        /// </summary>
        [SerializeField] protected int[] TerminalOfConnectedWires;

        /// <summary>
        /// 直接続している機器
        /// </summary>
        [SerializeField] protected AbstractConnectableMachine[] ConnectedMachine;
        /// <summary>
        /// 直接続している機器上の端子インデックス
        /// </summary>
        [SerializeField] protected int[] MachineTerminalIndex;

        private void Start()
        {
            int idx = 0;
            foreach(AbstractWire connectedWire in DirectConnectedWireArray)
            {
                if(connectedWire != null) AddNewWireConnection(connectedWire, idx, TerminalOfConnectedWires[idx]);
                idx++;
            }
        }

        ///<summary>接続するためのメソッド</summary>
        ///<param name="newWire">新規に接続される線</param>
        ///<param name="terminalIndex">端子番号</param>
        public void AddNewWireConnection(AbstractWire newWire, int myTerminalIndex, int theyTerminalIndex)
        {
            SetNewWireConnection(newWire,myTerminalIndex, theyTerminalIndex);
            newWire.SetNewWireConnection(this, theyTerminalIndex, myTerminalIndex);
            UpdateAllConnectedWirePointer();
        }
        private void SetNewWireConnection(AbstractWire newWire, int myTerminalIndex, int theyTerminalIndex)
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
        ///<returns>接続の切断に成功したか</returns>
        private void RemoveWireConnection(int myTerminalIndex, int theyTerminalIndex)
        {
            RemovingWireConnection(myTerminalIndex, theyTerminalIndex);
            DirectConnectedWireArray[myTerminalIndex] = null;

            UpdateAllConnectedWirePointer();
        }

        public void UpdatePointer(System.Array Pointer)
        {
            int idx = 0;
            foreach (AbstractConnectableMachine machine in ConnectedMachine)
            {
                machine.SetPointer(MachineTerminalIndex[idx], Pointer);
                idx++;
            }
        }


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
                ((AbstractWire)dequeuedWire.Reference).AddDirectConnectedWires(in collected, in queue);
            }


            foreach (DataToken val in collected.ToArray())
            {
                AbstractWire val_wire = ((AbstractWire)val.Reference);
                Debug.Log(val_wire.name,this);

                val_wire.UpdatePointer(this.GetMyPointer());
            }
        }

        ///<summary>
        /// 自身に直接接続されている線の中から、collected 及び queue に含まれない物を抽出して、queueに追加
        ///</summary>
        ///<param name="collected">既に探索済みのWireのリスト</param>
        ///<param name="queue">次に探索するWireのリスト</param>
        public void AddDirectConnectedWires(in DataList collected, in DataList queue)
        {
            DataList newConnectedWireList = new DataList();
            foreach (AbstractWire ConnectedWire in DirectConnectedWireArray)
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
        ///<summary>接続可否を判断するメソッド</summary>
        ///<param name="newWire">新規に接続される線</param>
        ///<param name="terminalIndex">端子番号</param>
        protected abstract void AddingNewWire(AbstractWire newWire, int terminalIndex);

        ///<summary>接続を切って良いか判定する、もしくは付随処理を行うメソッド</summary>
        ///<param name="terminalIndex">端子番号</param>
        ///<returns>接続の切断に成功したか</returns>
        protected abstract void RemovingWireConnection(int terminalIndex,int theyTerminalIndex);

        protected abstract void GivePointerToMachine();

        protected abstract void applyPointerToMachine();
        public abstract System.Array GetMyPointer();
#else
        protected virtual void AddingNewWire(AbstractWire newWire, int terminalIndex){
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