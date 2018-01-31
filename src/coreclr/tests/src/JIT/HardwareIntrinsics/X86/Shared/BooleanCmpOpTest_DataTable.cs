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
    public unsafe struct BooleanComparisonOpTest__DataTable<T> : IDisposable where T : struct
    {
        private GCHandle inHandle1;
        private GCHandle inHandle2;

        public T[] inArray1;
        public T[] inArray2;

        public BooleanComparisonOpTest__DataTable(T[] inArray1, T[] inArray2)
        {
            this.inArray1 = inArray1;
            this.inArray2 = inArray2;

            this.inHandle1 = GCHandle.Alloc(inArray1, GCHandleType.Pinned);
            this.inHandle2 = GCHandle.Alloc(inArray2, GCHandleType.Pinned);
        }

        public void* inArray1Ptr => inHandle1.AddrOfPinnedObject().ToPointer();
        public void* inArray2Ptr => inHandle2.AddrOfPinnedObject().ToPointer();

        public void Dispose()
        {
            inHandle1.Free();
            inHandle2.Free();
        }
    }
}
