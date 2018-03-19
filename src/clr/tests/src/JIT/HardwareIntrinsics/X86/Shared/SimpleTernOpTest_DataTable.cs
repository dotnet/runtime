// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JIT.HardwareIntrinsics.X86
{
    public unsafe struct SimpleTernaryOpTest__DataTable<TResult, TOp1, TOp2, TOp3> : IDisposable
        where TResult : struct
        where TOp1 : struct
        where TOp2 : struct
        where TOp3 : struct
    {
        private byte[] inArray1;
        private byte[] inArray2;
        private byte[] inArray3;
        private byte[] outArray;

        private GCHandle inHandle1;
        private GCHandle inHandle2;
        private GCHandle inHandle3;
        private GCHandle outHandle;

        private byte simdSize;

        public SimpleTernaryOpTest__DataTable(TOp1[] inArray1, TOp2[] inArray2, TOp3[] inArray3, TResult[] outArray, int simdSize)
        {
            this.inArray1 = new byte[simdSize * 2];
            this.inArray2 = new byte[simdSize * 2];
            this.inArray3 = new byte[simdSize * 2];
            this.outArray = new byte[simdSize * 2];

            this.inHandle1 = GCHandle.Alloc(this.inArray1, GCHandleType.Pinned);
            this.inHandle2 = GCHandle.Alloc(this.inArray2, GCHandleType.Pinned);
            this.inHandle3 = GCHandle.Alloc(this.inArray3, GCHandleType.Pinned);
            this.outHandle = GCHandle.Alloc(this.outArray, GCHandleType.Pinned);

            this.simdSize = unchecked((byte)(simdSize));

            Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<TOp1, byte>(ref inArray1[0]), this.simdSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray2Ptr), ref Unsafe.As<TOp2, byte>(ref inArray2[0]), this.simdSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray3Ptr), ref Unsafe.As<TOp3, byte>(ref inArray3[0]), this.simdSize);
        }

        public void* inArray1Ptr => Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), simdSize);
        public void* inArray2Ptr => Align((byte*)(inHandle2.AddrOfPinnedObject().ToPointer()), simdSize);
        public void* inArray3Ptr => Align((byte*)(inHandle3.AddrOfPinnedObject().ToPointer()), simdSize);
        public void* outArrayPtr => Align((byte*)(outHandle.AddrOfPinnedObject().ToPointer()), simdSize);

        public void Dispose()
        {
            inHandle1.Free();
            inHandle2.Free();
            inHandle3.Free();
            outHandle.Free();
        }

        private static unsafe void* Align(byte* buffer, byte expectedAlignment)
        {
            // Compute how bad the misalignment is, which is at most (expectedAlignment - 1).
            // Then subtract that from the expectedAlignment and add it to the original address
            // to compute the aligned address.

            var misalignment = expectedAlignment - ((ulong)(buffer) % expectedAlignment);
            var result = (void*)(buffer + misalignment);
            
            Debug.Assert(((ulong)(result) % expectedAlignment) == 0);
            Debug.Assert((ulong)(result) <= ((ulong)(result) + expectedAlignment));

            return result;
        }
    }
}
