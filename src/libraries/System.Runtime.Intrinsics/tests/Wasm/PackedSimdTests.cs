using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Wasm;
using System.Tests;
using Xunit;

namespace System.Runtime.Intrinsics.Wasm.Tests
{
    [PlatformSpecific(TestPlatforms.Browser)]
    public sealed class PackedSimdTests
    {
        [Fact]
        public unsafe void BasicArithmeticTest()
        {
            var v1 = Vector128.Create(1, 2, 3, 4);
            var v2 = Vector128.Create(5, 6, 7, 8);

            var addResult = PackedSimd.Add(v1, v2);
            var subResult = PackedSimd.Subtract(v1, v2);
            var mulResult = PackedSimd.Multiply(v1, v2);

            Assert.Equal(Vector128.Create(6, 8, 10, 12), addResult);
            Assert.Equal(Vector128.Create(-4, -4, -4, -4), subResult);
            Assert.Equal(Vector128.Create(5, 12, 21, 32), mulResult);
        }

        [Fact]
        public unsafe void BitwiseOperationsTest()
        {
            var v1 = Vector128.Create(0b1100, 0b1010, 0b1110, 0b1111);
            var v2 = Vector128.Create(0b1010, 0b1100, 0b0011, 0b0101);

            var andResult = PackedSimd.And(v1, v2);
            var orResult = PackedSimd.Or(v1, v2);
            var xorResult = PackedSimd.Xor(v1, v2);

            Assert.Equal(Vector128.Create(0b1000, 0b1000, 0b0010, 0b0101), andResult);
            Assert.Equal(Vector128.Create(0b1110, 0b1110, 0b1111, 0b1111), orResult);
            Assert.Equal(Vector128.Create(0b0110, 0b0110, 0b1101, 0b1010), xorResult);
        }

        [Fact]
        public unsafe void ShiftOperationsTest()
        {
            var v = Vector128.Create(16, -16, 32, -32);

            var leftShift = PackedSimd.ShiftLeft(v, 2);
            var rightShiftArith = PackedSimd.ShiftRightArithmetic(v, 2);
            var rightShiftLogical = PackedSimd.ShiftRightLogical(v, 2);

            Assert.Equal(Vector128.Create(64, -64, 128, -128), leftShift);
            Assert.Equal(Vector128.Create(4, -4, 8, -8), rightShiftArith);
            Assert.Equal(Vector128.Create(4, 1073741820, 8, 1073741816), rightShiftLogical);
        }

        [Fact]
        public unsafe void ComparisonOperationsTest()
        {
            var v1 = Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f);
            var v2 = Vector128.Create(4.0f, 3.0f, 2.0f, 1.0f);

            var minResult = PackedSimd.Min(v1, v2);
            var maxResult = PackedSimd.Max(v1, v2);

            Assert.Equal(Vector128.Create(1.0f, 2.0f, 2.0f, 1.0f), minResult);
            Assert.Equal(Vector128.Create(4.0f, 3.0f, 3.0f, 4.0f), maxResult);
        }

        [Fact]
        public unsafe void FloatingPointOperationsTest()
        {
            var v = Vector128.Create(4.0f, 9.0f, 16.0f, 25.0f);

            var sqrtResult = PackedSimd.Sqrt(v);
            var ceilResult = PackedSimd.Ceiling(v);
            var floorResult = PackedSimd.Floor(v);

            Assert.Equal(Vector128.Create(2.0f, 3.0f, 4.0f, 5.0f), sqrtResult);
            Assert.Equal(Vector128.Create(4.0f, 9.0f, 16.0f, 25.0f), ceilResult);
            Assert.Equal(Vector128.Create(4.0f, 9.0f, 16.0f, 25.0f), floorResult);
        }

        [Fact]
        public unsafe void LoadStoreTest()
        {
            int[] values = new int[] { 1, 2, 3, 4 };
            fixed (int* ptr = values)
            {
                var loaded = PackedSimd.LoadVector128(ptr);
                Assert.Equal(Vector128.Create(1, 2, 3, 4), loaded);

                int[] storeTarget = new int[4];
                fixed (int* storePtr = storeTarget)
                {
                    PackedSimd.Store(storePtr, loaded);
                    Assert.Equal(values, storeTarget);
                }
            }
        }

        [Fact]
        public unsafe void ExtractInsertScalarTest()
        {
            var v = Vector128.Create(1, 2, 3, 4);

            int extracted = PackedSimd.ExtractScalar(v, 2);
            Assert.Equal(3, extracted);

            var modified = PackedSimd.ReplaceScalar(v, 2, 10);
            Assert.Equal(Vector128.Create(1, 2, 10, 4), modified);
        }

        [Fact]
        public unsafe void SaturatingArithmeticTest()
        {
            var v1 = Vector128.Create((byte)250, (byte)251, (byte)252, (byte)253, (byte)254, (byte)255, (byte)255, (byte)255,
                                    (byte)250, (byte)251, (byte)252, (byte)253, (byte)254, (byte)255, (byte)255, (byte)255);
            var v2 = Vector128.Create((byte)10, (byte)10, (byte)10, (byte)10, (byte)10, (byte)10, (byte)10, (byte)10,
                                    (byte)10, (byte)10, (byte)10, (byte)10, (byte)10, (byte)10, (byte)10, (byte)10);

            var addSat = PackedSimd.AddSaturate(v1, v2);
            var subSat = PackedSimd.SubtractSaturate(v1, v2);

            // Verify saturation at 255 for addition
            Assert.Equal(Vector128.Create((byte)255, (byte)255, (byte)255, (byte)255, (byte)255, (byte)255, (byte)255, (byte)255,
                                        (byte)255, (byte)255, (byte)255, (byte)255, (byte)255, (byte)255, (byte)255, (byte)255), addSat);

            // Verify expected subtraction results
            Assert.Equal(Vector128.Create((byte)240, (byte)241, (byte)242, (byte)243, (byte)244, (byte)245, (byte)245, (byte)245,
                                        (byte)240, (byte)241, (byte)242, (byte)243, (byte)244, (byte)245, (byte)245, (byte)245), subSat);
        }

        [Fact]
        public unsafe void WideningOperationsTest()
        {
            var v = Vector128.Create((short)1000, (short)2000, (short)3000, (short)4000,
                                   (short)5000, (short)6000, (short)7000, (short)8000);

            var lowerWidened = PackedSimd.SignExtendWideningLower(v);
            var upperWidened = PackedSimd.SignExtendWideningUpper(v);

            Assert.Equal(Vector128.Create(1000, 2000, 3000, 4000), lowerWidened);
            Assert.Equal(Vector128.Create(5000, 6000, 7000, 8000), upperWidened);
        }

        [Fact]
        public unsafe void SwizzleTest()
        {
            var v = Vector128.Create((byte)1, (byte)2, (byte)3, (byte)4, (byte)5, (byte)6, (byte)7, (byte)8,
                                   (byte)9, (byte)10, (byte)11, (byte)12, (byte)13, (byte)14, (byte)15, (byte)16);
            var indices = Vector128.Create((byte)3, (byte)2, (byte)1, (byte)0, (byte)7, (byte)6, (byte)5, (byte)4,
                                        (byte)11, (byte)10, (byte)9, (byte)8, (byte)15, (byte)14, (byte)13, (byte)12);

            var swizzled = PackedSimd.Swizzle(v, indices);

            Assert.Equal(Vector128.Create((byte)4, (byte)3, (byte)2, (byte)1, (byte)8, (byte)7, (byte)6, (byte)5,
                                        (byte)12, (byte)11, (byte)10, (byte)9, (byte)16, (byte)15, (byte)14, (byte)13), swizzled);
        }

        [Fact]
        public unsafe void LoadScalarAndSplatTest()
        {
            int value = 42;
            float fValue = 3.14f;

            int* intPtr = &value;
            float* floatPtr = &fValue;

            var intSplat = PackedSimd.LoadScalarAndSplatVector128(intPtr);
            var floatSplat = PackedSimd.LoadScalarAndSplatVector128(floatPtr);

            Assert.Equal(Vector128.Create(42, 42, 42, 42), intSplat);
            Assert.Equal(Vector128.Create(3.14f, 3.14f, 3.14f, 3.14f), floatSplat);
        }

        [Fact]
        public unsafe void LoadWideningTest()
        {
            byte[] bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            fixed (byte* ptr = bytes)
            {
                var widened = PackedSimd.LoadWideningVector128(ptr);
                Assert.Equal(Vector128.Create((ushort)1, (ushort)2, (ushort)3, (ushort)4,
                                           (ushort)5, (ushort)6, (ushort)7, (ushort)8), widened);
            }
        }

        [Fact]
        public unsafe void StoreSelectedScalarTest()
        {
            var v = Vector128.Create(1, 2, 3, 4);
            int value = 0;
            int* ptr = &value;

            PackedSimd.StoreSelectedScalar(ptr, v, 2);
            Assert.Equal(3, value);
        }

        [Fact]
        public unsafe void LoadScalarAndInsertTest()
        {
            var v = Vector128.Create(1, 2, 3, 4);
            int newValue = 42;
            int* ptr = &newValue;

            var result = PackedSimd.LoadScalarAndInsert(ptr, v, 2);
            Assert.Equal(Vector128.Create(1, 2, 42, 4), result);
        }

        [Fact]
        public unsafe void ConversionTest()
        {
            var intVector = Vector128.Create(1, 2, 3, 4);
            var floatVector = Vector128.Create(1.5f, 2.5f, 3.5f, 4.5f);
            var doubleVector = Vector128.Create(1.5, 2.5);

            var intToFloat = PackedSimd.ConvertToSingle(intVector);
            var floatToDouble = PackedSimd.ConvertToDoubleLower(floatVector);

            Assert.Equal(Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f), intToFloat);
            Assert.Equal(Vector128.Create(1.5, 2.5), floatToDouble);
        }

        [Fact]
        public unsafe void AddPairwiseWideningTest()
        {
            var bytes = Vector128.Create((byte)1, (byte)2, (byte)3, (byte)4,
                                       (byte)5, (byte)6, (byte)7, (byte)8,
                                       (byte)9, (byte)10, (byte)11, (byte)12,
                                       (byte)13, (byte)14, (byte)15, (byte)16);

            var widened = PackedSimd.AddPairwiseWidening(bytes);

            Assert.Equal(Vector128.Create((ushort)3, (ushort)7, (ushort)11, (ushort)15,
                                        (ushort)19, (ushort)23, (ushort)27, (ushort)31), widened);
        }

        [Fact]
        public unsafe void MultiplyWideningTest()
        {
            var shorts = Vector128.Create((short)10, (short)20, (short)30, (short)40,
                                        (short)50, (short)60, (short)70, (short)80);
            var multiplier = Vector128.Create((short)2, (short)2, (short)2, (short)2,
                                            (short)2, (short)2, (short)2, (short)2);

            var lowerResult = PackedSimd.MultiplyWideningLower(shorts, multiplier);
            var upperResult = PackedSimd.MultiplyWideningUpper(shorts, multiplier);

            Assert.Equal(Vector128.Create(20, 40, 60, 80), lowerResult);
            Assert.Equal(Vector128.Create(100, 120, 140, 160), upperResult);
        }

        [Fact]
        public unsafe void DotProductTest()
        {
            var v1 = Vector128.Create((short)1, (short)2, (short)3, (short)4,
                                    (short)5, (short)6, (short)7, (short)8);
            var v2 = Vector128.Create((short)2, (short)2, (short)2, (short)2,
                                    (short)2, (short)2, (short)2, (short)2);

            var dot = PackedSimd.Dot(v1, v2);

            // Each pair of values is multiplied and added:
            // (1*2 + 2*2) = 6 for first int
            // (3*2 + 4*2) = 14 for second int
            // (5*2 + 6*2) = 22 for third int
            // (7*2 + 8*2) = 30 for fourth int
            Assert.Equal(Vector128.Create(6, 14, 22, 30), dot);
        }

        [Fact]
        public unsafe void FloatingPointNegationTest()
        {
            var v = Vector128.Create(1.0f, -2.0f, 3.0f, -4.0f);
            var d = Vector128.Create(1.0, -2.0);

            var negatedFloat = PackedSimd.Negate(v);
            var negatedDouble = PackedSimd.Negate(d);

            Assert.Equal(Vector128.Create(-1.0f, 2.0f, -3.0f, 4.0f), negatedFloat);
            Assert.Equal(Vector128.Create(-1.0, 2.0), negatedDouble);
        }

        [Fact]
        public unsafe void FloatingPointAbsTest()
        {
            var v = Vector128.Create(-1.0f, 2.0f, -3.0f, 4.0f);
            var d = Vector128.Create(-1.0, 2.0);

            var absFloat = PackedSimd.Abs(v);
            var absDouble = PackedSimd.Abs(d);

            Assert.Equal(Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f), absFloat);
            Assert.Equal(Vector128.Create(1.0, 2.0), absDouble);
        }

        [Fact]
        public unsafe void FloatingPointDivisionTest()
        {
            var v1 = Vector128.Create(2.0f, 4.0f, 6.0f, 8.0f);
            var v2 = Vector128.Create(2.0f, 2.0f, 2.0f, 2.0f);
            var d1 = Vector128.Create(2.0, 4.0);
            var d2 = Vector128.Create(2.0, 2.0);

            var divFloat = PackedSimd.Divide(v1, v2);
            var divDouble = PackedSimd.Divide(d1, d2);

            Assert.Equal(Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f), divFloat);
            Assert.Equal(Vector128.Create(1.0, 2.0), divDouble);
        }

        [Fact]
        public unsafe void IntegerAbsTest()
        {
            var bytes = Vector128.Create((sbyte)-1, (sbyte)2, (sbyte)-3, (sbyte)4,
                                       (sbyte)-5, (sbyte)6, (sbyte)-7, (sbyte)8,
                                       (sbyte)-9, (sbyte)10, (sbyte)-11, (sbyte)12,
                                       (sbyte)-13, (sbyte)14, (sbyte)-15, (sbyte)16);
            var shorts = Vector128.Create((short)-1, (short)2, (short)-3, (short)4,
                                        (short)-5, (short)6, (short)-7, (short)8);
            var ints = Vector128.Create(-1, 2, -3, 4);

            var absBytes = PackedSimd.Abs(bytes);
            var absShorts = PackedSimd.Abs(shorts);

            Assert.Equal(Vector128.Create((sbyte)1, (sbyte)2, (sbyte)3, (sbyte)4,
                                        (sbyte)5, (sbyte)6, (sbyte)7, (sbyte)8,
                                        (sbyte)9, (sbyte)10, (sbyte)11, (sbyte)12,
                                        (sbyte)13, (sbyte)14, (sbyte)15, (sbyte)16), absBytes);
            Assert.Equal(Vector128.Create((short)1, (short)2, (short)3, (short)4,
                                        (short)5, (short)6, (short)7, (short)8), absShorts);
        }

        [Fact]
        public unsafe void AverageRoundedTest()
        {
            var bytes1 = Vector128.Create((byte)1, (byte)3, (byte)5, (byte)7,
                                        (byte)9, (byte)11, (byte)13, (byte)15,
                                        (byte)17, (byte)19, (byte)21, (byte)23,
                                        (byte)25, (byte)27, (byte)29, (byte)31);
            var bytes2 = Vector128.Create((byte)3, (byte)5, (byte)7, (byte)9,
                                        (byte)11, (byte)13, (byte)15, (byte)17,
                                        (byte)19, (byte)21, (byte)23, (byte)25,
                                        (byte)27, (byte)29, (byte)31, (byte)33);

            var avgBytes = PackedSimd.AverageRounded(bytes1, bytes2);

            // Average is rounded up: (a + b + 1) >> 1
            Assert.Equal(Vector128.Create((byte)2, (byte)4, (byte)6, (byte)8,
                                        (byte)10, (byte)12, (byte)14, (byte)16,
                                        (byte)18, (byte)20, (byte)22, (byte)24,
                                        (byte)26, (byte)28, (byte)30, (byte)32), avgBytes);
        }

        [Fact]
        public unsafe void MinMaxSignedUnsignedTest()
        {
            var signedBytes = Vector128.Create((sbyte)-1, (sbyte)2, (sbyte)-3, (sbyte)4,
                                             (sbyte)-5, (sbyte)6, (sbyte)-7, (sbyte)8,
                                             (sbyte)-9, (sbyte)10, (sbyte)-11, (sbyte)12,
                                             (sbyte)-13, (sbyte)14, (sbyte)-15, (sbyte)16);

            var unsignedBytes = Vector128.Create((byte)255, (byte)2, (byte)253, (byte)4,
                                               (byte)251, (byte)6, (byte)249, (byte)8,
                                               (byte)247, (byte)10, (byte)245, (byte)12,
                                               (byte)243, (byte)14, (byte)241, (byte)16);

            var signedMin = PackedSimd.Min(signedBytes, signedBytes.WithElement(0, (sbyte)0));
            var unsignedMin = PackedSimd.Min(unsignedBytes, unsignedBytes.WithElement(0, (byte)0));

            // Verify different comparison behavior for signed vs unsigned
            Assert.Equal((sbyte)-1, signedMin.GetElement(0));
            Assert.Equal((byte)0, unsignedMin.GetElement(0));
        }

        [Fact]
        public unsafe void SplatTypes()
        {
            Assert.Equal(Vector128.Create(2.5f, 2.5f, 2.5f, 2.5f), PackedSimd.Splat(2.5f));
            Assert.Equal(Vector128.Create(-2, -2, -2, -2), PackedSimd.Splat(-2));
            Assert.Equal(Vector128.Create(2U, 2U, 2U, 2U), PackedSimd.Splat(2U));
            Assert.Equal(Vector128.Create(2.5, 2.5), PackedSimd.Splat(2.5));
            Assert.Equal(Vector128.Create(-2L, -2L), PackedSimd.Splat(-2L));
            Assert.Equal(Vector128.Create(2UL, 2UL), PackedSimd.Splat(2UL));
            Assert.Equal(Vector128.Create(
                (byte)2, (byte)2, (byte)2, (byte)2,
                (byte)2, (byte)2, (byte)2, (byte)2,
                (byte)2, (byte)2, (byte)2, (byte)2,
                (byte)2, (byte)2, (byte)2, (byte)2), PackedSimd.Splat((byte)2));
            Assert.Equal(Vector128.Create(
                (sbyte)-2, (sbyte)-2, (sbyte)-2, (sbyte)-2,
                (sbyte)-2, (sbyte)-2, (sbyte)-2, (sbyte)-2,
                (sbyte)-2, (sbyte)-2, (sbyte)-2, (sbyte)-2,
                (sbyte)-2, (sbyte)-2, (sbyte)-2, (sbyte)-2), PackedSimd.Splat((sbyte)-2));
            Assert.Equal(Vector128.Create(
                (short)-2, (short)-2, (short)-2, (short)-2,
                (short)-2, (short)-2, (short)-2, (short)-2), PackedSimd.Splat((short)-2));
            Assert.Equal(Vector128.Create(
                (ushort)2, (ushort)2, (ushort)2, (ushort)2,
                (ushort)2, (ushort)2, (ushort)2, (ushort)2), PackedSimd.Splat((ushort)2));
            Assert.Equal(Vector128.Create([(nint)2, (nint)2, (nint)2, (nint)2]), PackedSimd.Splat((nint)2));
            Assert.Equal(Vector128.Create([(nuint)2, (nuint)2, (nuint)2, (nuint)2]), PackedSimd.Splat((nuint)2));
        }

        [Fact]
        public unsafe void LoadScalarAndSplatInfinityTest()
        {
            float fInf = float.PositiveInfinity;
            double dInf = double.PositiveInfinity;

            float* fPtr = &fInf;
            double* dPtr = &dInf;

            var floatSplat = PackedSimd.LoadScalarAndSplatVector128(fPtr);
            var doubleSplat = PackedSimd.LoadScalarAndSplatVector128(dPtr);

            for (int i = 0; i < 4; i++)
            {
                Assert.True(float.IsPositiveInfinity(floatSplat.GetElement(i)));
            }

            for (int i = 0; i < 2; i++)
            {
                Assert.True(double.IsPositiveInfinity(doubleSplat.GetElement(i)));
            }
        }

        [Fact]
        public unsafe void FloatingPointTruncateTest()
        {
            var v1 = Vector128.Create(1.7f, -2.3f, 3.5f, -4.8f);
            var d1 = Vector128.Create(1.7, -2.3);

            var truncFloat = PackedSimd.Truncate(v1);
            var truncDouble = PackedSimd.Truncate(d1);

            Assert.Equal(Vector128.Create(1.0f, -2.0f, 3.0f, -4.0f), truncFloat);
            Assert.Equal(Vector128.Create(1.0, -2.0), truncDouble);
        }

        [Fact]
        public unsafe void ComparisonWithNaNTest()
        {
            var v1 = Vector128.Create(1.0f, float.NaN, 3.0f, float.PositiveInfinity);
            var v2 = Vector128.Create(float.NegativeInfinity, 2.0f, float.NaN, 4.0f);

            var minResult = PackedSimd.Min(v1, v2);
            var maxResult = PackedSimd.Max(v1, v2);

            // IEEE 754 rules: if either operand is NaN, the result should be NaN
            Assert.True(float.IsNaN(minResult.GetElement(1)));
            Assert.True(float.IsNaN(maxResult.GetElement(2)));
            Assert.Equal(float.NegativeInfinity, minResult.GetElement(0));
            Assert.Equal(float.PositiveInfinity, maxResult.GetElement(3));
        }

        [Fact]
        public unsafe void NativeIntegerArithmeticTest()
        {
            var v1 = Vector128.Create([(nint)1, (nint)2, (nint)3, (nint)4]);
            var v2 = Vector128.Create([(nint)5, (nint)6, (nint)7, (nint)8]);

            var addResult = PackedSimd.Add(v1, v2);
            var subResult = PackedSimd.Subtract(v1, v2);
            var mulResult = PackedSimd.Multiply(v1, v2);

            Assert.Equal(Vector128.Create([(nint)6, (nint)8, (nint)10, (nint)12]), addResult);
            Assert.Equal(Vector128.Create([(nint)(-4), (nint)(-4), (nint)(-4), (nint)(-4)]), subResult);
            Assert.Equal(Vector128.Create([(nint)5, (nint)12, (nint)21, (nint)32]), mulResult);
        }

        [Fact]
        public unsafe void NativeUnsignedIntegerArithmeticTest()
        {
            var v1 = Vector128.Create([(nuint)1, (nuint)2, (nuint)3, (nuint)4]);
            var v2 = Vector128.Create([(nuint)5, (nuint)6, (nuint)7, (nuint)8]);

            var addResult = PackedSimd.Add(v1, v2);
            var subResult = PackedSimd.Subtract(v1, v2);
            var mulResult = PackedSimd.Multiply(v1, v2);

            Assert.Equal(Vector128.Create([(nuint)6, (nuint)8, (nuint)10, (nuint)12]), addResult);
            Assert.Equal(Vector128.Create([unchecked((nuint)(-4)), unchecked((nuint)(-4)), unchecked((nuint)(-4)), unchecked((nuint)(-4))]), subResult);
            Assert.Equal(Vector128.Create([(nuint)5, (nuint)12, (nuint)21, (nuint)32]), mulResult);
        }

        [Fact]
        public unsafe void NativeIntegerLoadStoreTest()
        {
            nint[] values = new nint[] { 1, 2, 3, 4 };
            fixed (nint* ptr = values)
            {
                var loaded = PackedSimd.LoadVector128(ptr);
                Assert.Equal(Vector128.Create(values.AsSpan()), loaded);

                nint[] storeTarget = new nint[4];
                fixed (nint* storePtr = storeTarget)
                {
                    PackedSimd.Store(storePtr, loaded);
                    Assert.Equal(values, storeTarget);
                }
            }
        }

        [Fact]
        public unsafe void NativeUnsignedIntegerLoadStoreTest()
        {
            nuint[] values = new nuint[] { 1, 2, 3, 4 };
            fixed (nuint* ptr = values)
            {
                var loaded = PackedSimd.LoadVector128(ptr);
                Assert.Equal(Vector128.Create(values.AsSpan()), loaded);

                nuint[] storeTarget = new nuint[4];
                fixed (nuint* storePtr = storeTarget)
                {
                    PackedSimd.Store(storePtr, loaded);
                    Assert.Equal(values, storeTarget);
                }
            }
        }

        [Fact]
        public void NativeIntegerShiftTest()
        {
            var v = Vector128.Create([(nint)16, (nint)(-16), (nint)32, (nint)(-32)]);

            var leftShift = PackedSimd.ShiftLeft(v, 2);
            var rightShiftArith = PackedSimd.ShiftRightArithmetic(v, 2);
            var rightShiftLogical = PackedSimd.ShiftRightLogical(v, 2);

            Assert.Equal(Vector128.Create([(nint)64, (nint)(-64), (nint)128, (nint)(-128)]), leftShift);
            Assert.Equal(Vector128.Create([(nint)4, (nint)(-4), (nint)8, (nint)(-8)]), rightShiftArith);
            Assert.Equal(Vector128.Create([(nint)4, (nint)1073741820, (nint)8, (nint)1073741816]), rightShiftLogical);
        }

        [Fact]
        public void NativeUnsignedIntegerShiftTest()
        {
            var v = Vector128.Create([(nuint)16, unchecked((nuint)(-16)), (nuint)32, unchecked((nuint)(-32))]);

            var leftShift = PackedSimd.ShiftLeft(v, 2);
            var rightShiftLogical = PackedSimd.ShiftRightLogical(v, 2);

            Assert.Equal(Vector128.Create([(nuint)64, unchecked((nuint)(-64)), (nuint)128, unchecked((nuint)(-128))]), leftShift);
            Assert.Equal(Vector128.Create([(nuint)4, (nuint)1073741820, (nuint)8, (nuint)1073741816]), rightShiftLogical);
        }
    }
}