// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public partial class Object
    {
        // Returns a Type object which represent this object instance.
        [Intrinsic]
        public unsafe Type GetType()
        {
            MethodTable* pMT = RuntimeHelpers.GetMethodTable(this);
            RuntimeType type = RuntimeTypeHandle.GetRuntimeType(pMT);
            GC.KeepAlive(this);
            return type;
        }

        // Returns a new object instance that is a memberwise copy of this
        // object.  This is always a shallow copy of the instance. The method is protected
        // so that other object may only call this method on themselves.  It is intended to
        // support the ICloneable interface.
        [Intrinsic]
        protected internal unsafe object MemberwiseClone()
        {
            object clone = this;
            RuntimeHelpers.AllocateUninitializedClone(ObjectHandleOnStack.Create(ref clone));
            Debug.Assert(clone != this);

            // copy contents of "this" to the clone

            nuint byteCount = RuntimeHelpers.GetRawObjectDataSize(clone);
            ref byte src = ref this.GetRawData();
            ref byte dst = ref clone.GetRawData();

            MethodTable* pMT = RuntimeHelpers.GetMethodTable(clone);
            if (pMT->ContainsGCPointers)
            {
                if (RuntimeHelpers.RequiresOldValueWriteBarrier())
                {
                    if (pMT->IsArray)
                    {
                        Array array = Unsafe.As<Array>(clone);
                        MethodTable* elementType = pMT->GetArrayElementTypeHandle().AsMethodTable();
                        nuint dataOffset = pMT->BaseSize - (nuint)(2 * sizeof(IntPtr));
                        SpanHelpers.Memmove(ref dst, ref src, dataOffset);
                        Buffer.BulkMoveWithOldValueWriteBarrier(
                            ref Unsafe.AddByteOffset(ref dst, dataOffset),
                            ref Unsafe.AddByteOffset(ref src, dataOffset),
                            elementType->IsValueType ? elementType : null,
                            pMT->ComponentSize,
                            array.NativeLength);
                    }
                    else
                    {
                        Buffer.BulkMoveWithOldValueWriteBarrier(ref dst, ref src, pMT, byteCount, 1);
                    }
                }
                else
                {
                    Buffer.BulkMoveWithWriteBarrier(ref dst, ref src, byteCount);
                }
            }
            else
            {
                SpanHelpers.Memmove(ref dst, ref src, byteCount);
            }

            return clone;
        }
    }
}
