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

        private ulong alignment;

        public SimpleTernaryOpTest__DataTable(TOp1[] inArray1, TOp2[] inArray2, TOp3[] inArray3, TResult[] outArray, int alignment)
        {
            int sizeOfinArray1 = inArray1.Length * Unsafe.SizeOf<TOp1>();
            int sizeOfinArray2 = inArray2.Length * Unsafe.SizeOf<TOp2>();
            int sizeOfinArray3 = inArray3.Length * Unsafe.SizeOf<TOp3>();
            int sizeOfoutArray = outArray.Length * Unsafe.SizeOf<TResult>();
            if (((alignment != 64) && (alignment != 32) && (alignment != 16)) || (alignment * 2) < sizeOfinArray1 || (alignment * 2) < sizeOfinArray2 || (alignment * 2) < sizeOfinArray3 || (alignment * 2) < sizeOfoutArray)
            {
                throw new ArgumentException("Invalid value of alignment");
            }

            this.inArray1 = new byte[alignment * 2];
            this.inArray2 = new byte[alignment * 2];
            this.inArray3 = new byte[alignment * 2];
            this.outArray = new byte[alignment * 2];

            this.inHandle1 = GCHandle.Alloc(this.inArray1, GCHandleType.Pinned);
            this.inHandle2 = GCHandle.Alloc(this.inArray2, GCHandleType.Pinned);
            this.inHandle3 = GCHandle.Alloc(this.inArray3, GCHandleType.Pinned);
            this.outHandle = GCHandle.Alloc(this.outArray, GCHandleType.Pinned);

            this.alignment = (ulong)alignment;

            Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<TOp1, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
            Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray2Ptr), ref Unsafe.As<TOp2, byte>(ref inArray2[0]), (uint)sizeOfinArray2);
            Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray3Ptr), ref Unsafe.As<TOp3, byte>(ref inArray3[0]), (uint)sizeOfinArray3);
        }

        public void* inArray1Ptr => Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), alignment);
        public void* inArray2Ptr => Align((byte*)(inHandle2.AddrOfPinnedObject().ToPointer()), alignment);
        public void* inArray3Ptr => Align((byte*)(inHandle3.AddrOfPinnedObject().ToPointer()), alignment);
        public void* outArrayPtr => Align((byte*)(outHandle.AddrOfPinnedObject().ToPointer()), alignment);

        public void Dispose()
        {
            inHandle1.Free();
            inHandle2.Free();
            inHandle3.Free();
            outHandle.Free();
        }

        private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
        {
            return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
        }
    }
}
