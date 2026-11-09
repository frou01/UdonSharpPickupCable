using Codice.CM.Client.Differences.Graphic;
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.UdonSharpPickupCable.Samples
{
    /// <summary>
    /// Wireが4本（float,float,int,reference）のCable。多芯ケーブルのサンプル。
    /// </summary>
    public class SimpleMultiCable : Cable
    {
        private const int WireCount = 4;
        /// <summary>
        /// trueならWriterとしてvalueToWriteを書き込む。
        /// </summary>
        public bool isWriter_0;
        public bool isWriter_1;
        public bool isWriter_2;
        public bool isWriter_3;

        /// <summary>
        /// Writerがいないときの値
        /// </summary>
        public float defaultValue_0;
        public float defaultValue_1;
        public int defaultValue_2;
        public UdonSharpBehaviour defaultValue_3;

        /// <summary>
        /// Writerのときに書き込む値。
        /// </summary>
        public float valueToWrite_0;
        public float valueToWrite_1;
        public int valueToWrite_2;
        public UdonSharpBehaviour valueToWrite_3;

        /// <summary>
        /// 今のNetの疑似ポインタ。毎フレーム読むならこの参照を読む。Netが変わると差し替わる。
        /// </summary>
        [NonSerialized] public float[] pointer_0;
        [NonSerialized] public float[] pointer_1;
        [NonSerialized] public int[] pointer_2;
        [NonSerialized] public UdonSharpBehaviour[] pointer_3;

        void Start()
        {
            for (int i = 0; i < 4; i++)
            {
                // 同じNetの別のCableが先に配り直していれば、もう一度やらなくてよい
                if (currentPointers != null && currentPointers.Length > i && currentPointers[i] != null) continue;
                RequestRefresh(i);
            }
        }

        public override bool IsWriter(int wire)
        {
            switch (wire)
            {
                case 0:
                    return isWriter_0;
                case 1:
                    return isWriter_1;
                case 2:
                    return isWriter_2;
                case 3:
                    return isWriter_3;
            }
            return false;
        }

        public override void OnNetChangedWriter(int wire, Array pointer)
        {
            switch (wire)
            {
                case 0:
                    ((float[])pointer)[0] = valueToWrite_0;
                    this.pointer_0 = (float[])pointer;
                    break;
                case 1:
                    ((float[])pointer)[0] = valueToWrite_1;
                    this.pointer_1 = (float[])pointer;
                    break;
                case 2:
                    ((int[])pointer)[0] = valueToWrite_2;
                    this.pointer_2 = (int[])pointer;
                    break;
                case 3:
                    ((UdonSharpBehaviour[])pointer)[0] = valueToWrite_3;
                    this.pointer_3 = (UdonSharpBehaviour[])pointer;
                    break;
            }
        }

        public override void OnNetChangedReader(int wire, Array pointer)
        {
            switch (wire)
            {
                case 0:
                    this.pointer_0 = (float[])pointer;
                    break;
                case 1:
                    this.pointer_1 = (float[])pointer;
                    break;
                case 2:
                    this.pointer_2 = (int[])pointer;
                    break;
                case 3:
                    this.pointer_3 = (UdonSharpBehaviour[])pointer;
                    break;
            }
        }

        protected override void ResetToDefault(int wire, Array pointer)
        {
            switch (wire)
            {
                case 0:
                    ((float[])pointer)[0] = defaultValue_0;
                    break;
                case 1:
                    ((float[])pointer)[0] = defaultValue_1;
                    break;
                case 2:
                    ((int[])pointer)[0] = defaultValue_2;
                    break;
                case 3:
                    ((UdonSharpBehaviour[])pointer)[0] = defaultValue_3;
                    break;
            }
        }

        protected override object[] CreateSourcePointers()
        {
            object[] sourcePointers = new object[WireCount];
            sourcePointers[0] = new float[1];
            sourcePointers[1] = new float[1];
            sourcePointers[2] = new int[1];
            sourcePointers[3] = new UdonSharpBehaviour[1];
            return sourcePointers;
        }
    }
}