// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._Avx2
{
    public partial class Program
    {
        [Fact]
        public static unsafe void MoveMask()
        {
            int testResult = Pass;

            if (Avx2.IsSupported)
            {
                using (TestTable_SingleArray<byte> byteTable = new TestTable_SingleArray<byte>(new byte[32] { 255, 2, 0, 80, 0, 7, 0, 1, 2, 7, 80, 0, 123, 127, 5, 255, 255, 2, 0, 80, 0, 7, 0, 1, 2, 7, 80, 0, 123, 127, 5, 255 }))
                {

                    var vf1 = Unsafe.Read<Vector256<byte>>(byteTable.inArray1Ptr);
                    var res = Avx2.MoveMask(vf1);

                    if (res != -2147385343)
                    {
                        Console.WriteLine("AVX2 MoveMask failed on byte:");
                        Console.WriteLine(res);
                        testResult = Fail;
                    }
                }

                using (TestTable_SingleArray<sbyte> sbyteTable = new TestTable_SingleArray<sbyte>(new sbyte[32] { -1, 2, 0, 6, 0, 7, 111, 1, 2, 55, 80, 0, 11, 127, 5, -9, -1, 2, 0, 6, 0, 7, 111, 1, 2, 55, 80, 0, 11, 127, 5, -9 }))
                {

                    var vf1 = Unsafe.Read<Vector256<sbyte>>(sbyteTable.inArray1Ptr);
                    var res = Avx2.MoveMask(vf1);

                    if (res != -2147385343)
                    {
                        Console.WriteLine("AVX2 MoveMask failed on sbyte:");
                        Console.WriteLine(res);
                        testResult = Fail;
                    }
                }
            }


            Assert.Equal(Pass, testResult);
        }

        public unsafe struct TestTable_SingleArray<T> : IDisposable where T : struct
        {
            public T[] inArray1;
            public void* inArray1Ptr => inHandle1.AddrOfPinnedObject().ToPointer();
            GCHandle inHandle1;

            public TestTable_SingleArray(T[] a)
            {
                this.inArray1 = a;
                inHandle1 = GCHandle.Alloc(inArray1, GCHandleType.Pinned);
            }

            public void Dispose()
            {
                inHandle1.Free();
            }
        }

    }
}
