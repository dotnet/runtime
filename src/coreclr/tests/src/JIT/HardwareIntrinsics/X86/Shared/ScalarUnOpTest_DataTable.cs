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
    public unsafe struct SimpleScalarUnaryOpTest__DataTable<TResult> : IDisposable
        where TResult : struct
    {
        public byte[] outArray;

        private GCHandle outHandle;

        private byte simdSize;

        public SimpleScalarUnaryOpTest__DataTable(TResult[] outArray, int simdSize)
        {
            this.outArray = new byte[simdSize * 2];

            this.outHandle = GCHandle.Alloc(this.outArray, GCHandleType.Pinned);

            this.simdSize = unchecked((byte)(simdSize));
        }

        public void* outArrayPtr => Align((byte*)(outHandle.AddrOfPinnedObject().ToPointer()), simdSize);

        public void Dispose()
        {
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
