// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public partial class Object
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ObjectNative_GetTypeSlow")]
        private static unsafe partial void GetTypeSlow(MethodTable* methodTable, ObjectHandleOnStack ret);

        // Returns a Type object which represent this object instance.
        [Intrinsic]
        public unsafe Type GetType()
        {
            MethodTable* pMT = RuntimeHelpers.GetMethodTable(this);
            Type type = pMT->AuxiliaryData->ExposedClassObject ?? GetTypeWorker(pMT);
            GC.KeepAlive(this);
            return type;

            [MethodImpl(MethodImplOptions.NoInlining)]
            static Type GetTypeWorker(MethodTable* pMT)
            {
                Type? type = null;
                GetTypeSlow(pMT, ObjectHandleOnStack.Create(ref type));
                return type!;
            }
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

            if (RuntimeHelpers.GetMethodTable(clone)->ContainsGCPointers)
                Buffer.BulkMoveWithWriteBarrier(ref dst, ref src, byteCount);
            else
                SpanHelpers.Memmove(ref dst, ref src, byteCount);

            return clone;
        }
    }
}
