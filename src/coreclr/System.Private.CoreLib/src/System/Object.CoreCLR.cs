// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    public partial class Object
    {
        // Returns a Type object which represent this object instance.
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern Type GetType();

        // Returns a new object instance that is a memberwise copy of this
        // object.  This is always a shallow copy of the instance. The method is protected
        // so that other object may only call this method on themselves.  It is intended to
        // support the ICloneable interface.
        [Intrinsic]
        protected internal unsafe object MemberwiseClone()
        {
            object clone = RuntimeHelpers.AllocateUninitializedClone(this);

            // copy contents of "this" to the clone

            nuint byteCount = RuntimeHelpers.GetRawObjectDataSize(clone);
            ref byte src = ref this.GetRawData();
            ref byte dst = ref clone.GetRawData();

            if (RuntimeHelpers.GetMethodTable(clone)->ContainsGCPointers)
                Buffer.BulkMoveWithWriteBarrier(ref dst, ref src, byteCount, IntPtr.Zero);
            else
                Buffer.Memmove(ref dst, ref src, byteCount);

            return clone;
        }
    }
}
