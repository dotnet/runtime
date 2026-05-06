// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

using SharedTypes;

using Xunit;

namespace LibraryImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "subtract_ref_int")]
        public static unsafe partial void Subtract_Int_Ptr(int a, int* b);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "subtract_ref_byte")]
        public static unsafe partial void Subtract_Byte_Ptr(byte a, byte* b);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "blittablestructs_double_intfields_byref")]
        public static unsafe partial void DoubleIntFields_Ptr(IntFields* result);
    }

    public class PointerTests
    {
        [Fact]
        public unsafe void BlittablePrimitive()
        {
            {
                int a = int.MaxValue;
                int b = 10;
                int expected = a - b;
                NativeExportsNE.Subtract_Int_Ptr(a, &b);
                Assert.Equal(expected, b);
            }
            {
                byte a = byte.MaxValue;
                byte b = 10;
                byte expected = (byte)(a - b);
                NativeExportsNE.Subtract_Byte_Ptr(a, &b);
                Assert.Equal(expected, b);
            }
        }

        [Fact]
        public unsafe void BlittableStruct()
        {
            const int A = 24, B = 37, C = 59;
            var initial = new IntFields()
            {
                a = A,
                b = B,
                c = C,
            };
            var expected = new IntFields()
            {
                a = initial.a * 2,
                b = initial.b * 2,
                c = initial.c * 2,
            };

            var input = initial;
            {
                NativeExportsNE.DoubleIntFields_Ptr(&input);
                Assert.Equal(expected, input);
            }
        }
    }
}
