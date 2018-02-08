// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JIT.HardwareIntrinsics.X86
{
    public unsafe struct SimpleUnaryOpTest__DataTable<T> : IDisposable where T : struct
    {
        private GCHandle inHandle;
        private GCHandle outHandle;

        public T[] inArray;
        public T[] outArray;

        public SimpleUnaryOpTest__DataTable(T[] inArray, T[] outArray)
        {
            this.inArray = inArray;
            this.outArray = outArray;

            this.inHandle = GCHandle.Alloc(inArray, GCHandleType.Pinned);
            this.outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
        }

        public void* inArrayPtr => inHandle.AddrOfPinnedObject().ToPointer();
        public void* outArrayPtr => outHandle.AddrOfPinnedObject().ToPointer();

        public void Dispose()
        {
            inHandle.Free();
            outHandle.Free();
        }
    }
}
