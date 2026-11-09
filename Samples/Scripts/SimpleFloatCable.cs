using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.UdonSharpPickupCable.Samples
{
    /// <summary>
    /// Wireが1本（Wire 0、float[1]）だけのCable。継承先の書き方のサンプル。
    /// </summary>
    public class SimpleFloatCable : Cable
    {
        private const int WireIndex = 0;

        /// <summary>
        /// trueならWriterとしてvalueToWriteを書き込む。
        /// </summary>
        public bool isWriter;

        /// <summary>
        /// Writerがいないときの値
        /// </summary>
        public float defaultValue;

        /// <summary>
        /// Writerのときに書き込む値。
        /// </summary>
        public float valueToWrite;

        /// <summary>
        /// 今のNetの疑似ポインタ。毎フレーム読むならこの参照を読む。Netが変わると差し替わる。
        /// </summary>
        [NonSerialized]public float[] pointer;

        void Start()
        {
            // 同じNetの別のCableが先に配り直していれば、もう一度やらなくてよい
            if (currentPointers != null && currentPointers.Length > WireIndex && currentPointers[WireIndex] != null) return;
            RequestRefresh(WireIndex);
        }

        public override bool IsWriter(int wire)
        {
            return isWriter;
        }

        public override void OnNetChangedWriter(int wire, Array pointer)
        {
            this.pointer = (float[])pointer;
            this.pointer[0] = valueToWrite;
        }

        public override void OnNetChangedReader(int wire, Array pointer)
        {
            this.pointer = (float[])pointer;
        }

        protected override void ResetToDefault(int wire, Array pointer)
        {
            ((float[])pointer)[0] = defaultValue;
        }

        protected override object[] CreateSourcePointers()
        {
            object[] sourcePointers = new object[1];
            sourcePointers[WireIndex] = new float[1];
            return sourcePointers;
        }
    }
}
