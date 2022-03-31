// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript.Private;

namespace System.Runtime.InteropServices.JavaScript
{
    public partial struct JavaScriptMarshalerArg
    {
        internal unsafe JSMarshalerArg* Value;

        public override unsafe string ToString()
        {
            return Value == null ? "null" : Value[0].ToString();
        }
    }
}

namespace System.Runtime.InteropServices.JavaScript.Private
{
    /// <summary>
    /// Discriminated union
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 8)]
    public struct JSMarshalerArg
    {
        #region Primitive
        [FieldOffset(0)]
        public bool BooleanValue;
        [FieldOffset(0)]
        public byte ByteValue;
        [FieldOffset(0)]
        public short Int16Value;
        [FieldOffset(0)]
        public int Int32Value;
        [FieldOffset(0)]
        public long Int64Value;// must be alligned to 8 because of HEAPI64 alignment
        [FieldOffset(0)]
        public float SingleValue;
        [FieldOffset(0)]
        public double DoubleValue;// must be alligned to 8 because of Module.HEAPF64 view alignment
        [FieldOffset(0)]
        public IntPtr IntPtrValue;
        #endregion

        #region Custom
        [FieldOffset(0)]
        public IntPtr ExtraBufferPtr;
        #endregion

        #region JSObject, JSException
        [FieldOffset(4)]
        public IntPtr JSHandle;
        #endregion

        #region System.Object, System.Exception
        [FieldOffset(4)]
        public IntPtr GCHandle;
        #endregion

        #region JSException, System.Exception
        [FieldOffset(8)]
        public IntPtr RootRef;
        #endregion

        /// <summary>
        /// Discriminator
        /// </summary>
        [FieldOffset(12)]
        public IntPtr TypeHandle;

        public override unsafe string ToString()
        {
            return $"JSArg -  TypeHandle:{TypeHandle} RootRef/Int32Value:{Int32Value} DoubleValue:{DoubleValue} JSHandle/GCHandle{JSHandle}";
        }
    }
}
