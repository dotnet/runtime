// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JIT.HardwareIntrinsics.X86
{
    public unsafe struct ScalarSimdUnaryOpTest__DataTable<TResult> : IDisposable
        where TResult : struct
    {
        public byte[] outArray;

        private GCHandle outHandle;

        private ulong alignment;

        public ScalarSimdUnaryOpTest__DataTable(TResult[] outArray, int alignment)
        {
            int sizeOfoutArray = outArray.Length * Unsafe.SizeOf<TResult>();
            if (((alignment != 64) && (alignment != 32) && (alignment != 16)) || (alignment * 2) < sizeOfoutArray)
            {
                throw new ArgumentException("Invalid value of alignment");
            }
            this.outArray = new byte[alignment * 2];

            this.outHandle = GCHandle.Alloc(this.outArray, GCHandleType.Pinned);

            this.alignment = (ulong)alignment;
        }

        public void* outArrayPtr => Align((byte*)(outHandle.AddrOfPinnedObject().ToPointer()), alignment);

        public void Dispose()
        {
            outHandle.Free();
        }

        private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
        {
            return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
        }
    }
}
