using System.Runtime.InteropServices;

using SharedTypes;

using Xunit;

namespace DllImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "blittablestructs_return_instance")]
        public static partial IntFields DoubleIntFields(IntFields result);

        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "blittablestructs_double_intfields_byref")]
        public static partial void DoubleIntFieldsByRef(ref IntFields result);

        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "blittablestructs_double_intfields_byref")]
        public static partial void DoubleIntFieldsByRefIn(in IntFields result);

        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "blittablestructs_double_intfields_refreturn")]
        public static partial void DoubleIntFieldsRefReturn(
            IntFields input,
            ref IntFields result);

        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "blittablestructs_double_intfields_refreturn")]
        public static partial void DoubleIntFieldsOutReturn(
            IntFields input,
            out IntFields result);
    }

    public class BlittableStructTests
    {
        [Fact]
        public void ValidateBlittableStruct()
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
    }
}
