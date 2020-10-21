using System.Runtime.InteropServices;

using SharedTypes;

using Xunit;

namespace DllImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "blittablestructs_double_intfields_byref")]
        public static partial void DoubleIntFieldsByRef(ref IntFields result);

        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "blittablestructs_double_intfields_refreturn")]
        public static partial void DoubleIntFieldsRefReturn(
            IntFields input,
            ref IntFields result);

        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "blittablestructs_double_intfields_refreturn")]
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
        }
    }
}
