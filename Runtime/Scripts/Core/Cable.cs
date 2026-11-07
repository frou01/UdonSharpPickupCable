using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.UdonSharpPickupCable
{
    public class Cable : UdonSharpBehaviour
    {
        public Terminal[] terminals;
        protected virtual void Start()
        {
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

                val_wire.UpdatePointer(null);
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
            //foreach (Cable ConnectedWire in DirectConnectedWireArray)
            //{
            //    if (ConnectedWire)
            //    {
            //        if (!collected.Contains(ConnectedWire) && !queue.Contains(ConnectedWire))
            //        {
            //            newConnectedWireList.Add(ConnectedWire);
            //        }
            //    }
            //}

            queue.AddRange(newConnectedWireList);
            return;
        }


    }
}