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
    public unsafe struct SimpleUnaryOpTest__DataTable<TResult, TOp1> : IDisposable
        where TResult : struct
        where TOp1 : struct
    {
        private byte[] inArray;
        private byte[] outArray;

        private GCHandle inHandle;
        private GCHandle outHandle;

        private byte simdSize;

        public SimpleUnaryOpTest__DataTable(TOp1[] inArray, TResult[] outArray, int simdSize)
        {
            this.inArray = new byte[simdSize * 2];
            this.outArray = new byte[simdSize * 2];

            this.inHandle = GCHandle.Alloc(this.inArray, GCHandleType.Pinned);
            this.outHandle = GCHandle.Alloc(this.outArray, GCHandleType.Pinned);

            this.simdSize = unchecked((byte)(simdSize));

            Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArrayPtr), ref Unsafe.As<TOp1, byte>(ref inArray[0]), this.simdSize);
        }

        public void* inArrayPtr => Align((byte*)(inHandle.AddrOfPinnedObject().ToPointer()), simdSize);
        public void* outArrayPtr => Align((byte*)(outHandle.AddrOfPinnedObject().ToPointer()), simdSize);

        public void Dispose()
        {
            inHandle.Free();
            outHandle.Free();
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
