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
    public unsafe struct SimdScalarUnaryOpTest__DataTable<TOp1> : IDisposable
        where TOp1 : struct
    {
        public byte[] inArray;

        private GCHandle inHandle;

        private ulong alignment;

        public SimdScalarUnaryOpTest__DataTable(TOp1[] inArray, int alignment)
        {
            int sizeOfinArray = inArray.Length * Unsafe.SizeOf<TOp1>();
            if (((alignment != 64) && (alignment != 32) && (alignment != 16)) || (alignment * 2) < sizeOfinArray)
            {
                throw new ArgumentException("Invalid value of alignment");
            }
            this.inArray = new byte[alignment * 2];

            this.inHandle = GCHandle.Alloc(this.inArray, GCHandleType.Pinned);

            this.alignment = (ulong)alignment;

            Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArrayPtr), ref Unsafe.As<TOp1, byte>(ref inArray[0]), (uint)sizeOfinArray);
        }

        public void* inArrayPtr => Align((byte*)(inHandle.AddrOfPinnedObject().ToPointer()), alignment);

        public void Dispose()
        {
            inHandle.Free();
        }

        private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
        {
            return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
        }
    }
}
