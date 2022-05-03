// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

using SharedTypes;

using Xunit;

namespace LibraryImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "blittablestructs_return_instance")]
        public static partial IntFields DoubleIntFields(IntFields result);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "blittablestructs_double_intfields_byref")]
        public static partial void DoubleIntFieldsByRef(ref IntFields result);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "blittablestructs_double_intfields_byref")]
        public static partial void DoubleIntFieldsByRefIn(in IntFields result);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "blittablestructs_double_intfields_refreturn")]
        public static partial void DoubleIntFieldsRefReturn(
            IntFields input,
            ref IntFields result);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "blittablestructs_double_intfields_refreturn")]
        public static partial void DoubleIntFieldsOutReturn(
            IntFields input,
            out IntFields result);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "blittablestructs_increment_invert_ptrfields_byref")]
        public static partial void IncrementInvertPointerFieldsByRef(ref PointerFields result);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "blittablestructs_increment_invert_ptrfields_byref")]
        public static partial void IncrementInvertPointerFieldsByRefIn(in PointerFields result);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "blittablestructs_increment_invert_ptrfields_refreturn")]
        public static partial void IncrementInvertPointerFieldsRefReturn(
            PointerFields input,
            ref PointerFields result);
    }

    public class BlittableStructTests
    {
        [Fact]
        public void ValidateIntFields()
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
                var result = NativeExportsNE.DoubleIntFields(input);
                Assert.Equal(initial, input);
                Assert.Equal(expected, result);
            }
            {
                var result = new IntFields();
                NativeExportsNE.DoubleIntFieldsRefReturn(input, ref result);
                Assert.Equal(initial, input);
                Assert.Equal(expected, result);
            }

            {
                IntFields result;
                NativeExportsNE.DoubleIntFieldsOutReturn(input, out result);
                Assert.Equal(initial, input);
                Assert.Equal(expected, result);
            }

            {
                input = initial;
                NativeExportsNE.DoubleIntFieldsByRef(ref input);
                Assert.Equal(expected, input);
            }

            {
                input = initial;
                NativeExportsNE.DoubleIntFieldsByRefIn(in input);
                Assert.Equal(expected, input); // Updated even when passed with in keyword (matches built-in system)
            }
        }

        [Fact]
        public unsafe void ValidatePointerFields()
        {
            int iInitial = 31;
            bool bInitial = false;
            char cInitial = 'A';

            int iExpected = iInitial + 1;
            bool bExpected = !bInitial;
            char cExpected = (char)(cInitial + 1);

            int i = iInitial;
            bool b = bInitial;
            char c = cInitial;
            var initial = new PointerFields()
            {
                i = &i,
                b = &b,
                c = &c,
            };

            PointerFields input = initial;
            {
                int iResult;
                bool bResult;
                char cResult;
                var result = new PointerFields()
                {
                    i = &iResult,
                    b = &bResult,
                    c = &cResult
                };
                NativeExportsNE.IncrementInvertPointerFieldsRefReturn(input, ref result);
                Assert.Equal(initial, input);
                ValidateFieldValues(result);
            }

            {
                ResetFieldValues(input);
                NativeExportsNE.IncrementInvertPointerFieldsByRef(ref input);
                Assert.Equal(initial, input);
                ValidateFieldValues(input);
            }

            {
                ResetFieldValues(input);
                NativeExportsNE.IncrementInvertPointerFieldsByRefIn(in input);
                Assert.Equal(initial, input);
                ValidateFieldValues(input);
            }

            void ResetFieldValues(PointerFields input)
            {
                *(input.i) = iInitial;
                *(input.b) = bInitial;
                *(input.c) = cInitial;
            }

            void ValidateFieldValues(PointerFields result)
            {
                Assert.Equal(iExpected, *result.i);
                Assert.Equal(bExpected, *result.b);
                Assert.Equal(cExpected, *result.c);
            }
        }
    }
}
