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
    public unsafe struct SimpleUnaryOpTest__DataTable<TResult, TOp1> : IDisposable
        where TResult : struct
        where TOp1 : struct
    {
        private byte[] inArray;
        private byte[] outArray;

        private GCHandle inHandle;
        private GCHandle outHandle;

        private ulong alignment;

        public SimpleUnaryOpTest__DataTable(TOp1[] inArray, TResult[] outArray, int alignment)
        {
            int sizeOfinArray = inArray.Length * Unsafe.SizeOf<TOp1>();
            int sizeOfoutArray = outArray.Length * Unsafe.SizeOf<TResult>();
            if (((alignment != 64) && (alignment != 32) && (alignment != 16)) || (alignment * 2) < sizeOfinArray || (alignment * 2) < sizeOfoutArray)
            {
                throw new ArgumentException("Invalid value of alignment");
            }

            this.inArray = new byte[alignment * 2];
            this.outArray = new byte[alignment * 2];

            this.inHandle = GCHandle.Alloc(this.inArray, GCHandleType.Pinned);
            this.outHandle = GCHandle.Alloc(this.outArray, GCHandleType.Pinned);

            this.alignment = (ulong)alignment;

            Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArrayPtr), ref Unsafe.As<TOp1, byte>(ref inArray[0]), (uint)sizeOfinArray);
        }

        public void* inArrayPtr => Align((byte*)(inHandle.AddrOfPinnedObject().ToPointer()), alignment);
        public void* outArrayPtr => Align((byte*)(outHandle.AddrOfPinnedObject().ToPointer()), alignment);

        public void Dispose()
        {
            inHandle.Free();
            outHandle.Free();
        }

        private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
        {
            return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
        }
    }
}
