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
    public unsafe struct BooleanTwoComparisonOpTest__DataTable<TOp1, TOp2> : IDisposable
        where TOp1 : struct
        where TOp2 : struct
    {
        private byte[] inArray1;
        private byte[] inArray2;

        private GCHandle inHandle1;
        private GCHandle inHandle2;

        private byte simdSize;

        public BooleanTwoComparisonOpTest__DataTable(TOp1[] inArray1, TOp2[] inArray2, int simdSize)
        {
            this.inArray1 = new byte[simdSize * 2];
            this.inArray2 = new byte[simdSize * 2];

            this.inHandle1 = GCHandle.Alloc(this.inArray1, GCHandleType.Pinned);
            this.inHandle2 = GCHandle.Alloc(this.inArray2, GCHandleType.Pinned);

            this.simdSize = unchecked((byte)(simdSize));

            Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<TOp1, byte>(ref inArray1[0]), this.simdSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray2Ptr), ref Unsafe.As<TOp2, byte>(ref inArray2[0]), this.simdSize);
        }

        public void* inArray1Ptr => Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), simdSize);
        public void* inArray2Ptr => Align((byte*)(inHandle2.AddrOfPinnedObject().ToPointer()), simdSize);

        public void Dispose()
        {
            inHandle1.Free();
            inHandle2.Free();
        }

        private static unsafe void* Align(byte* buffer, byte expectedAlignment)
        {
            // Compute how bad the misalignment is, which is at most (expectedAlignment - 1).
            // Then subtract that from the expectedAlignment and add it to the original address
            // to compute the aligned address.

            var misalignment = expectedAlignment - ((ulong)(buffer) % expectedAlignment);
            return (void*)(buffer + misalignment);
        }
    }
}
