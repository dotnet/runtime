// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest.Avx1
{
    public partial class Program
    {
        const int Pass = 100;
        const int Fail = 0;

        static unsafe void* Align(byte* buffer, byte expectedAlignment)
        {
            // Compute how bad the misalignment is, which is at most (expectedAlignment - 1).
            // Then subtract that from the expectedAlignment and add it to the original address
            // to compute the aligned address.

            var misalignment = expectedAlignment - ((ulong)(buffer) % expectedAlignment);
            return (void*)(buffer + misalignment);
        }

        public unsafe struct TestTable<T> : IDisposable where T : struct
        {
            public T[] inArray;
            public T[] outArray;

            public void* inArrayPtr => inHandle.AddrOfPinnedObject().ToPointer();
            public void* outArrayPtr => outHandle.AddrOfPinnedObject().ToPointer();

            GCHandle inHandle;
            GCHandle outHandle;
            public TestTable(T[] a, T[] b)
            {
                this.inArray = a;
                this.outArray = b;

                inHandle = GCHandle.Alloc(inArray, GCHandleType.Pinned);
                outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
            }
            public bool CheckResult(Func<T[], T[], bool> check)
            {
                return check(inArray, outArray);
            }
            public bool CheckResult(Func<T, T, bool> check)
            {
                for (int i = 0; i < inArray.Length; i++)
                {
                    if (!check(inArray[i], outArray[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            public void Dispose()
            {
                inHandle.Free();
                outHandle.Free();
            }
        }


        public unsafe struct AlignedTestTable<T> : IDisposable where T : struct
        {
            private byte[] inArray;
            public T[] outArray;

            private GCHandle inHandle;
            private GCHandle outHandle;

            private byte simdSize;

            public AlignedTestTable(T[] a, T[] b)
            {
                this.inArray = new byte[32];
                this.outArray = b;

                this.inHandle = GCHandle.Alloc(this.inArray, GCHandleType.Pinned);
                this.outHandle = GCHandle.Alloc(this.outArray, GCHandleType.Pinned);

                this.simdSize = 16;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArrayPtr), ref Unsafe.As<T, byte>(ref a[0]), this.simdSize);
            }

            public void* inArrayPtr => Align((byte*)(inHandle.AddrOfPinnedObject().ToPointer()), simdSize);
            public void* outArrayPtr => outHandle.AddrOfPinnedObject().ToPointer();

            public bool CheckResult(Func<T, T, bool> check)
            {
                for (int i = 0; i < outArray.Length; i++)
                {
                    if (!check(Unsafe.Add<T>(ref Unsafe.AsRef<T>(inArrayPtr), i), outArray[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

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

        public unsafe struct TestTable_2Input<T> : IDisposable where T : struct
        {
            public T[] inArray1;
            public T[] inArray2;
            public T[] outArray;

            public void* inArray1Ptr => inHandle1.AddrOfPinnedObject().ToPointer();
            public void* inArray2Ptr => inHandle2.AddrOfPinnedObject().ToPointer();
            public void* outArrayPtr => outHandle.AddrOfPinnedObject().ToPointer();

            GCHandle inHandle1;
            GCHandle inHandle2;
            GCHandle outHandle;
            public TestTable_2Input(T[] a, T[] b, T[] c)
            {
                this.inArray1 = a;
                this.inArray2 = b;
                this.outArray = c;

                inHandle1 = GCHandle.Alloc(inArray1, GCHandleType.Pinned);
                inHandle2 = GCHandle.Alloc(inArray2, GCHandleType.Pinned);
                outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
            }
            public bool CheckResult(Func<T[], T[], T[], bool> check)
            {
                return check(inArray1, inArray2, outArray);
            }
            public bool CheckResult(Func<T, T, T, bool> check)
            {
                for (int i = 0; i < inArray1.Length; i++)
                {
                    if (!check(inArray1[i], inArray2[i], outArray[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            public void Dispose()
            {
                inHandle1.Free();
                inHandle2.Free();
                outHandle.Free();
            }
        }


    }
}