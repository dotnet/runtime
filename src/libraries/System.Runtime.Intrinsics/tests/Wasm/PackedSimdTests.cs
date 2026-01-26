// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(PackedSimd))]
        public unsafe void PackedSimdIsSupported()
        {
            MethodInfo methodInfo = typeof(PackedSimd).GetMethod("get_IsSupported");
            Assert.Equal(PackedSimd.IsSupported, methodInfo.Invoke(null, null));
            Assert.Equal(PackedSimd.IsSupported, Vector128.IsHardwareAccelerated);
            Assert.True(PackedSimd.IsSupported);
        }

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
        public unsafe void NotTests()
        {
            var v16 = Vector128.Create((byte)0b11001100);
            var v8 = Vector128.Create((ushort)0b11110000_11001100);
            var v4 = Vector128.Create((uint)0b11111111_00000000_11110000_00000000);

            var notResult16 = PackedSimd.Not(v16);
            var notResult8 = PackedSimd.Not(v8);
            var notResult4 = PackedSimd.Not(v4);

            Assert.Equal(Vector128.Create((byte)0b00110011), notResult16);
            Assert.Equal(Vector128.Create((ushort)0b00001111_00110011), notResult8);
            Assert.Equal(Vector128.Create((uint)0b00000000_11111111_00001111_11111111), notResult4);
            var oc16 = ~v16;
            var oc8 = ~v8;
            var oc4 = ~v4;

            Assert.Equal(oc4, notResult4);
            Assert.Equal(oc8, notResult8);
            Assert.Equal(oc16, notResult16);

            Assert.Equal(Vector128.OnesComplement(v4), notResult4);
            Assert.Equal(Vector128.OnesComplement(v8), notResult8);
            Assert.Equal(Vector128.OnesComplement(v16), notResult16);
        }

        [Fact]
        public unsafe void BitwiseSelectTest()
        {
            // Test with integers
            var mask = Vector128.Create(unchecked((int)0xFFFFFFFF), 0, unchecked((int)0xFFFFFFFF), 0);
            var a = Vector128.Create(1, 2, 3, 4);
            var b = Vector128.Create(5, 6, 7, 8);

            // Use the correct parameter order: left(a), right(b), select(mask)
            var result = PackedSimd.BitwiseSelect(a, b, mask);
            var v128result = Vector128.ConditionalSelect(mask, a, b);
            // Where mask is all 1s, should select from a; where mask is all 0s, should select from b
            Assert.Equal(Vector128.Create(1, 6, 3, 8), v128result);
            Assert.Equal(v128result, result);

            // Test with bytes for more granular bit-level control
            var byteMask = Vector128.Create((byte)0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00);
            var byteA = Vector128.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            var byteB = Vector128.Create((byte)17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32
            );

            // Use the correct parameter order: left(byteA), right(byteB), select(byteMask)
            var byteResult = PackedSimd.BitwiseSelect(byteA, byteB, byteMask);
            Assert.Equal(Vector128.Create(
                (byte)1, 18, 3, 20, 5, 22, 7, 24,
                9, 26, 11, 28, 13, 30, 15, 32
            ), byteResult);

            // Test with floats to ensure proper handling of floating-point data
            var floatMask = Vector128.Create(
                BitConverter.Int32BitsToSingle(unchecked((int)0xFFFFFFFF)),
                BitConverter.Int32BitsToSingle(0),
                BitConverter.Int32BitsToSingle(unchecked((int)0xFFFFFFFF)),
                BitConverter.Int32BitsToSingle(0)
            );
            var floatA = Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f);
            var floatB = Vector128.Create(5.0f, 6.0f, 7.0f, 8.0f);

            // Use the correct parameter order: left(floatA), right(floatB), select(floatMask)
            var floatResult = PackedSimd.BitwiseSelect(floatA, floatB, floatMask);
            // Verify expected selection pattern (though we need to interpret bits, not values)
            for (int i = 0; i < 4; i++)
            {
                if (i % 2 == 0) // Mask has all 1s for even elements
                {
                    Assert.Equal(floatA.GetElement(i), floatResult.GetElement(i));
                }
                else // Mask has all 0s for odd elements
                {
                    Assert.Equal(floatB.GetElement(i), floatResult.GetElement(i));
                }
            }

            // Test with mixed bit patterns for more complex selection
            var partialMask = Vector128.Create(unchecked((int)0x0F0F0F0F), unchecked((int)0xF0F0F0F0), unchecked((int)0xAAAAAAAA), unchecked((int)0x55555555));
            // Use the correct parameter order: left(a), right(b), select(partialMask)
            var intResult = PackedSimd.BitwiseSelect(a, b, partialMask);

            // For this mixed mask test, verify the result by manually calculating the expected values
            int expectedValue0 = (unchecked((int)0x0F0F0F0F) & 1) | (~unchecked((int)0x0F0F0F0F) & 5);
            int expectedValue1 = (unchecked((int)0xF0F0F0F0) & 2) | (~unchecked((int)0xF0F0F0F0) & 6);
            int expectedValue2 = (unchecked((int)0xAAAAAAAA) & 3) | (~unchecked((int)0xAAAAAAAA) & 7);
            int expectedValue3 = (unchecked((int)0x55555555) & 4) | (~unchecked((int)0x55555555) & 8);

            Assert.Equal(Vector128.Create(expectedValue0, expectedValue1, expectedValue2, expectedValue3), intResult);

            // Test edge cases: all bits from one source
            var allOnes = Vector128.Create(unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF));
            var allZeros = Vector128.Create(0, 0, 0, 0);

            // All bits from a
            // Use the correct parameter order: left(a), right(b), select(allOnes)
            var allFromA = PackedSimd.BitwiseSelect(a, b, allOnes);
            Assert.Equal(a, allFromA);

            // All bits from b
            // Use the correct parameter order: left(a), right(b), select(allZeros)
            var allFromB = PackedSimd.BitwiseSelect(a, b, allZeros);
            Assert.Equal(b, allFromB);
        }

        [Fact]
        public unsafe void LoadStoreTest()
        {
            int[] values = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            fixed (int* ptr = values)
            {
                var loaded = PackedSimd.LoadVector128(ptr);
                var loaded2 = PackedSimd.LoadVector128(ptr + 4);

                Assert.Equal(Vector128.Create(1, 2, 3, 4), loaded);
                Assert.Equal(Vector128.Create(5, 6, 7, 8), loaded2);

                var vl = Vector128.LoadUnsafe(ref values[0], (nuint)0);
                var vl2 = Vector128.LoadUnsafe(ref values[0], (nuint)4);

                Assert.Equal(loaded, vl);
                Assert.Equal(loaded2, vl2);

                vl = Vector128.Load(ptr);
                vl2 = Vector128.Load(ptr + 4);

                Assert.Equal(loaded, vl);
                Assert.Equal(loaded2, vl2);

                Assert.Equal(Vector128.Create(1, 2, 3, 4), loaded);
                Assert.Equal(Vector128.Create(5, 6, 7, 8), loaded2);


                int[] storeTarget = new int[8];
                fixed (int* storePtr = storeTarget)
                {
                    PackedSimd.Store(storePtr, loaded);
                    PackedSimd.Store(storePtr + 4, loaded2);
                    Assert.Equal(values, storeTarget);
                }
            }
        }

        [Fact]
        public unsafe void ExtractInsertScalarTest()
        {
            var v = Vector128.Create(1, 2, 3, 4);

            int extracted = PackedSimd.ExtractScalar(v, 2);
            var modified = PackedSimd.ReplaceScalar(v, 2, 10);

            Assert.Equal(3, extracted);
            Assert.Equal(Vector128.Create(1, 2, 10, 4), modified);
        }

        [Fact]
        public unsafe void SaturatingArithmeticTest()
        {
            var v1 = Vector128.Create((byte)250, 251, 252, 253, 254, 255, 255, 255, 250, 251, 252, 253, 254, 255, 255, 255);
            var v2 = Vector128.Create((byte)10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10);

            var addSat = PackedSimd.AddSaturate(v1, v2);
            var subSat = PackedSimd.SubtractSaturate(v2, v1);

            // Verify saturation at 255 for addition
            Assert.Equal(Vector128.Create((byte)255), addSat);

            // Verify expected subtraction results
            Assert.Equal(Vector128.Create((byte)0), subSat);

            var v3 = Vector128.Create((ushort)65530, 65531, 65532, 65533, 65534, 65535, 65535, 65535);
            var v4 = Vector128.Create((ushort)10, 10, 10, 10, 10, 10, 10, 10);

            var addSatUShort = PackedSimd.AddSaturate(v3, v4);
            var subSatUShort = PackedSimd.SubtractSaturate(v4, v3);

            // Verify saturation at 65535 for addition
            Assert.Equal(Vector128.Create((ushort)65535), addSatUShort);

            // Verify expected subtraction results
            Assert.Equal(Vector128.Create((ushort)0), subSatUShort);
        }

        [Fact]
        public unsafe void SaturatingArithmeticSignedTest()
        {
            var v1 = Vector128.Create((sbyte)120, 121, 122, 123, 124, 125, 126, 127, 120, 121, 122, 123, 124, 125, 126, 127);
            var v2 = Vector128.Create((sbyte)10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10);

            var addSat = PackedSimd.AddSaturate(v1, v2);
            var subSat = PackedSimd.SubtractSaturate(v1, v2);

            // Verify saturation at 127 (max sbyte value) for addition
            Assert.Equal(Vector128.Create((sbyte)127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127), addSat);

            // Verify expected subtraction results - should be original values minus 10
            Assert.Equal(Vector128.Create((sbyte)110, 111, 112, 113, 114, 115, 116, 117, 110, 111, 112, 113, 114, 115, 116, 117), subSat);

            // Test negative saturation - when results would be below sbyte.MinValue
            var v3 = Vector128.Create((sbyte)-120, -121, -122, -123, -124, -125, -126, -127, -120, -121, -122, -123, -124, -125, -126, -128);
            var v4 = Vector128.Create((sbyte)10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10);

            var subSat2 = PackedSimd.SubtractSaturate(v3, v4);

            // Verify saturation at -128 (min sbyte value) for subtraction
            Assert.Equal(Vector128.Create((sbyte)-128, -128, -128, -128, -128, -128, -128, -128, -128, -128, -128, -128, -128, -128, -128, -128), subSat2);

            // Test shorts
            var s1 = Vector128.Create((short)32000, 32001, 32002, 32003, 32004, 32005, 32006, 32007);
            var s2 = Vector128.Create((short)1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000);

            var shortAddSat = PackedSimd.AddSaturate(s1, s2);
            var shortSubSat = PackedSimd.SubtractSaturate(s1, s2);

            // Verify saturation at 32767 (max short value) for addition
            Assert.Equal(Vector128.Create((short)32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767), shortAddSat);

            // Verify expected subtraction results - should be original values minus 1000
            Assert.Equal(Vector128.Create((short)31000, 31001, 31002, 31003, 31004, 31005, 31006, 31007), shortSubSat);

            // Test negative saturation
            var s3 = Vector128.Create((short)-32000, -32001, -32002, -32003, -32004, -32005, -32006, -32007);
            var s4 = Vector128.Create((short)1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000);

            var shortSubSat2 = PackedSimd.SubtractSaturate(s3, s4);

            // Verify saturation at -32768 (min short value) for subtraction
            Assert.Equal(Vector128.Create((short)-32768, -32768, -32768, -32768, -32768, -32768, -32768, -32768), shortSubSat2);
        }

        [Fact]
        public unsafe void SaturatingArithmeticForIntegersTest()
        {
            // Test for sbyte (saturates at -128 and 127)
            var sb1 = Vector128.Create((sbyte)120, (sbyte)120, (sbyte)-120, (sbyte)-120,
                                     (sbyte)120, (sbyte)120, (sbyte)-120, (sbyte)-120,
                                     (sbyte)120, (sbyte)120, (sbyte)-120, (sbyte)-120,
                                     (sbyte)120, (sbyte)120, (sbyte)-120, (sbyte)-120);
            var sb2 = Vector128.Create((sbyte)10, (sbyte)20, (sbyte)-10, (sbyte)-20,
                                     (sbyte)10, (sbyte)20, (sbyte)-10, (sbyte)-20,
                                     (sbyte)10, (sbyte)20, (sbyte)-10, (sbyte)-20,
                                     (sbyte)10, (sbyte)20, (sbyte)-10, (sbyte)-20);

            var sbAddSat = PackedSimd.AddSaturate(sb1, sb2);
            var sbSubSat = PackedSimd.SubtractSaturate(sb1, sb2);

            Assert.Equal(Vector128.Create((sbyte)127, (sbyte)127, (sbyte)-128, (sbyte)-128,
                                        (sbyte)127, (sbyte)127, (sbyte)-128, (sbyte)-128,
                                        (sbyte)127, (sbyte)127, (sbyte)-128, (sbyte)-128,
                                        (sbyte)127, (sbyte)127, (sbyte)-128, (sbyte)-128), sbAddSat);
            Assert.Equal(Vector128.Create((sbyte)110, (sbyte)100, (sbyte)-110, (sbyte)-100,
                                        (sbyte)110, (sbyte)100, (sbyte)-110, (sbyte)-100,
                                        (sbyte)110, (sbyte)100, (sbyte)-110, (sbyte)-100,
                                        (sbyte)110, (sbyte)100, (sbyte)-110, (sbyte)-100), sbSubSat);

            // Test for short (saturates at -32768 and 32767)
            var short1 = Vector128.Create((short)32000, (short)32000, (short)-32000, (short)-32000,
                                        (short)30000, (short)30000, (short)-30000, (short)-30000);
            var short2 = Vector128.Create((short)1000, (short)2000, (short)-1000, (short)-2000,
                                        (short)1000, (short)2000, (short)-1000, (short)-2000);

            var shortAddSat = PackedSimd.AddSaturate(short1, short2);
            var shortSubSat = PackedSimd.SubtractSaturate(short1, short2);

            Assert.Equal(Vector128.Create((short)32767, (short)32767, (short)-32768, (short)-32768,
                                        (short)31000, (short)32000, (short)-31000, (short)-32000), shortAddSat);
            Assert.Equal(Vector128.Create((short)31000, (short)30000, (short)-31000, (short)-30000,
                                        (short)29000, (short)28000, (short)-29000, (short)-28000), shortSubSat);
        }

        [Fact]
        public unsafe void SaturatingArithmeticForUnsignedIntegersTest()
        {
            // Test for byte (saturates at 0 and 255)
            var b1 = Vector128.Create((byte)250, (byte)250, (byte)5, (byte)5,
                                     (byte)250, (byte)250, (byte)5, (byte)5,
                                     (byte)250, (byte)250, (byte)5, (byte)5,
                                     (byte)250, (byte)250, (byte)5, (byte)5);
            var b2 = Vector128.Create((byte)10, (byte)20, (byte)10, (byte)20,
                                     (byte)10, (byte)20, (byte)10, (byte)20,
                                     (byte)10, (byte)20, (byte)10, (byte)20,
                                     (byte)10, (byte)20, (byte)10, (byte)20);

            var bAddSat = PackedSimd.AddSaturate(b1, b2);
            var bSubSat = PackedSimd.SubtractSaturate(b1, b2);

            Assert.Equal(Vector128.Create((byte)255, (byte)255, (byte)15, (byte)25,
                                        (byte)255, (byte)255, (byte)15, (byte)25,
                                        (byte)255, (byte)255, (byte)15, (byte)25,
                                        (byte)255, (byte)255, (byte)15, (byte)25), bAddSat);
            Assert.Equal(Vector128.Create((byte)240, (byte)230, (byte)0, (byte)0,
                                        (byte)240, (byte)230, (byte)0, (byte)0,
                                        (byte)240, (byte)230, (byte)0, (byte)0,
                                        (byte)240, (byte)230, (byte)0, (byte)0), bSubSat);

            // Test for ushort (saturates at 0 and 65535)
            var ushort1 = Vector128.Create((ushort)65000, (ushort)65000, (ushort)5, (ushort)5,
                                         (ushort)60000, (ushort)60000, (ushort)10, (ushort)10);
            var ushort2 = Vector128.Create((ushort)1000, (ushort)2000, (ushort)10, (ushort)20,
                                         (ushort)10000, (ushort)20000, (ushort)15, (ushort)25);

            var ushortAddSat = PackedSimd.AddSaturate(ushort1, ushort2);
            var ushortSubSat = PackedSimd.SubtractSaturate(ushort1, ushort2);

            Assert.Equal(Vector128.Create((ushort)65535, (ushort)65535, (ushort)15, (ushort)25,
                                        (ushort)65535, (ushort)65535, (ushort)25, (ushort)35), ushortAddSat);
            Assert.Equal(Vector128.Create((ushort)64000, (ushort)63000, (ushort)0, (ushort)0,
                                        (ushort)50000, (ushort)40000, (ushort)0, (ushort)0), ushortSubSat);
        }

        [Fact]
        public unsafe void SaturatingArithmeticEdgeCasesTest()
        {
            // Edge cases for signed bytes
            var sbMax = Vector128.Create(sbyte.MaxValue);
            var sbMin = Vector128.Create(sbyte.MinValue);
            var sbOne = Vector128<sbyte>.One;

            var sbOverflow = PackedSimd.AddSaturate(sbMax, sbOne);
            var sbUnderflow = PackedSimd.SubtractSaturate(sbMin, sbOne);

            Assert.Equal(Vector128.Create(sbyte.MaxValue), sbOverflow);
            Assert.Equal(Vector128.Create(sbyte.MinValue), sbUnderflow);

            // Edge cases for unsigned bytes
            var bMax = Vector128.Create(byte.MaxValue);
            var bMin = Vector128.Create(byte.MinValue);
            var bOne = Vector128<byte>.One;

            var bOverflow = PackedSimd.AddSaturate(bMax, bOne);
            var bUnderflow = PackedSimd.SubtractSaturate(bMin, bOne);

            Assert.Equal(Vector128.Create(byte.MaxValue), bOverflow);
            Assert.Equal(Vector128.Create(byte.MinValue), bUnderflow);

            // Edge cases for signed shorts
            var shortMax = Vector128.Create(short.MaxValue);
            var shortMin = Vector128.Create(short.MinValue);
            var shortOne = Vector128<short>.One;

            var shortOverflow = PackedSimd.AddSaturate(shortMax, shortOne);
            var shortUnderflow = PackedSimd.SubtractSaturate(shortMin, shortOne);

            Assert.Equal(Vector128.Create(short.MaxValue), shortOverflow);
            Assert.Equal(Vector128.Create(short.MinValue), shortUnderflow);

            // Edge cases for unsigned shorts
            var ushortMax = Vector128.Create(ushort.MaxValue);
            var ushortMin = Vector128.Create(ushort.MinValue);
            var ushortOne = Vector128<ushort>.One;

            var ushortOverflow = PackedSimd.AddSaturate(ushortMax, ushortOne);
            var ushortUnderflow = PackedSimd.SubtractSaturate(ushortMin, ushortOne);

            Assert.Equal(Vector128.Create(ushort.MaxValue), ushortOverflow);
            Assert.Equal(Vector128.Create(ushort.MinValue), ushortUnderflow);
        }

        [Fact]
        public unsafe void WideningOperationsTest()
        {
            var v = Vector128.Create((short)1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000);

            var lowerWidened = PackedSimd.SignExtendWideningLower(v);
            var upperWidened = PackedSimd.SignExtendWideningUpper(v);

            Assert.Equal(Vector128.Create(1000, 2000, 3000, 4000), lowerWidened);
            Assert.Equal(Vector128.Create(5000, 6000, 7000, 8000), upperWidened);
        }

        [Fact]
        public unsafe void SwizzleTest()
        {
            var v = Vector128.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            var indices = Vector128.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12);

            var swizzled = PackedSimd.Swizzle(v, indices);
            Assert.Equal(Vector128.Create((byte)4, 3, 2, 1, 8, 7, 6, 5, 12, 11, 10, 9, 16, 15, 14, 13), swizzled);
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
                Assert.Equal(Vector128.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8), widened);
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
            var bytes = Vector128.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);

            var widened = PackedSimd.AddPairwiseWidening(bytes);
            Assert.Equal(Vector128.Create((ushort)3, 7, 11, 15, 19, 23, 27, 31), widened);
        }

        [Fact]
        public unsafe void MultiplyWideningTest()
        {
            var shorts = Vector128.Create((short)10, 20, 30, 40, 50, 60, 70, 80);
            var multiplier = Vector128.Create((short)2, 2, 2, 2, 2, 2, 2, 2);

            var lowerResult = PackedSimd.MultiplyWideningLower(shorts, multiplier);
            var upperResult = PackedSimd.MultiplyWideningUpper(shorts, multiplier);

            Assert.Equal(Vector128.Create(20, 40, 60, 80), lowerResult);
            Assert.Equal(Vector128.Create(100, 120, 140, 160), upperResult);
        }

        [Fact]
        public unsafe void DotProductTest()
        {
            var v1 = Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 8);
            var v2 = Vector128.Create((short)2, 2, 2, 2, 2, 2, 2, 2);

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
            var bytes = Vector128.Create((sbyte)-1, 2, -3, 4, -5, 6, -7, 8, -9, 10, -11, 12, -13, 14, -15, 16);
            var shorts = Vector128.Create((short)-1, 2, -3, 4, 5, 6, -7, 8);
            var ints = Vector128.Create(-1, 2, -3, 4);

            var absBytes = PackedSimd.Abs(bytes);
            var absShorts = PackedSimd.Abs(shorts);
            Assert.Equal(Vector128.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16), absBytes);
            Assert.Equal(Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 8), absShorts);
        }

        [Fact]
        public unsafe void AverageRoundedTest()
        {
            var bytes1 = Vector128.Create((byte)1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31);
            var bytes2 = Vector128.Create((byte)3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33);

            var avgBytes = PackedSimd.AverageRounded(bytes1, bytes2);

            // Average is rounded up: (a + b + 1) >> 1
            Assert.Equal(Vector128.Create((byte)2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32), avgBytes);
        }

        [Fact]
        public unsafe void MinMaxSignedUnsignedTest()
        {
            var signedBytes = Vector128.Create((sbyte)-1, 2, -3, 4, -5, 6, -7, 8, -9, 10, -11, 12, -13, 14, -15, 16);
            var unsignedBytes = Vector128.Create((byte)255, 2, 253, 4, 251, 6, 249, 8, 247, 10, 245, 12, 243, 14, 241, 16);

            var signedMinV128 = Vector128.Min(signedBytes, signedBytes.WithElement(0, (sbyte)0));
            var unsignedMinV128 = Vector128.Min(unsignedBytes, unsignedBytes.WithElement(0, (byte)0));
            var signedMin = PackedSimd.Min(signedBytes, signedBytes.WithElement(0, (sbyte)0));
            var unsignedMin = PackedSimd.Min(unsignedBytes, unsignedBytes.WithElement(0, (byte)0));

            Assert.Equal((sbyte)-1, signedMinV128.GetElement(0));
            Assert.Equal((byte)0, unsignedMinV128.GetElement(0));

            Assert.Equal(signedMinV128, signedMin);
            Assert.Equal(unsignedMinV128, unsignedMin);
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
            Assert.Equal(Vector128.Create((byte)2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2), PackedSimd.Splat((byte)2));
            Assert.Equal(Vector128.Create((sbyte)-2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2), PackedSimd.Splat((sbyte)-2));
            Assert.Equal(Vector128.Create((short)-2, -2, -2, -2, -2, -2, -2, -2), PackedSimd.Splat((short)-2));
            Assert.Equal(Vector128.Create((ushort)2, 2, 2, 2, 2, 2, 2, 2), PackedSimd.Splat((ushort)2));
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

            var minResultV128 = Vector128.Min(v1, v2);
            var maxResultV128 = Vector128.Max(v1, v2);

            Assert.True(float.IsNaN(minResultV128.GetElement(1)));
            Assert.True(float.IsNaN(maxResultV128.GetElement(2)));
            Assert.Equal(float.NegativeInfinity, minResultV128.GetElement(0));
            Assert.Equal(float.PositiveInfinity, maxResultV128.GetElement(3));

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
            Assert.Equal(Vector128.Create([(nint)(-4), (-4), (-4), (-4)]), subResult);
            Assert.Equal(Vector128.Create([(nint)5, 12, 21, 32]), mulResult);
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
            Assert.Equal(Vector128.Create([(nint)64, (-64), 128, (-128)]), leftShift);
            Assert.Equal(Vector128.Create([(nint)4, (-4), 8, (-8)]), rightShiftArith);
            Assert.Equal(Vector128.Create([(nint)4, 1073741820, 8, 1073741816]), rightShiftLogical);
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

        [Fact]
        public unsafe void ConvertNarrowingSaturateSignedTest()
        {
            var v1 = Vector128.Create(32767, 32768, -32768, -32769);
            var v2 = Vector128.Create(100, 200, -100, -200);

            var result = PackedSimd.ConvertNarrowingSaturateSigned(v1, v2);

            Assert.Equal(Vector128.Create((short)32767, 32767, -32768, -32768, 100, 200, -100, -200), result);
        }

        [Fact]
        public unsafe void ConvertNarrowingSaturateUnsignedShortToByte()
        {
            // Test shorts to bytes - valid values and values that need saturation
            var lower = Vector128.Create((short)255, 256, 127, -1, 300, 0, 200, 100);
            var upper = Vector128.Create((short)50, 150, -50, -150, 75, 175, 225, 0);

            var result = PackedSimd.ConvertNarrowingSaturateUnsigned(lower, upper);

            // Values should saturate between 0 and 255
            // - Values >= 256 saturate to 255
            // - Negative values saturate to 0
            // - Values between 0-255 remain unchanged
            Assert.Equal(Vector128.Create(
                (byte)255, 255, 127, 0, 255, 0, 200, 100,
                50, 150, 0, 0, 75, 175, 225, 0
            ), result);

            // Edge cases - test with maximum short values and extreme negative values
            var lowerEdge = Vector128.Create((short)32767, 256, 255, 0, -1, -32768, 1, 254);
            var upperEdge = Vector128.Create((short)1, 2, 3, 4, 32767, 16384, 8192, 4096);

            var resultEdge = PackedSimd.ConvertNarrowingSaturateUnsigned(lowerEdge, upperEdge);

            // All values above 255 should saturate to 255
            // All negative values should saturate to 0
            Assert.Equal(Vector128.Create(
                (byte)255, 255, 255, 0, 0, 0, 1, 254,
                1, 2, 3, 4, 255, 255, 255, 255
            ), resultEdge);
        }

        [Fact]
        public unsafe void ConvertNarrowingSaturateUnsignedIntToUShort()
        {
            // Existing test renamed for clarity (was ConvertNarrowingSaturateUnsignedTest)
            var v1 = Vector128.Create(65535, 65536, -1, -100);
            var v2 = Vector128.Create(100, 200, 300, 400);

            var result = PackedSimd.ConvertNarrowingSaturateUnsigned(v1, v2);

            Assert.Equal(Vector128.Create((ushort)65535, 65535, 0, 0, 100, 200, 300, 400), result);

            // Edge cases - test with maximum int values and extreme negative values
            var lowerEdge = Vector128.Create(int.MaxValue, 65536, 65535, 0);
            var upperEdge = Vector128.Create(-1, int.MinValue, 32768, 32767);

            var resultEdge = PackedSimd.ConvertNarrowingSaturateUnsigned(lowerEdge, upperEdge);

            // Values > 65535 should saturate to 65535
            // Negative values should saturate to 0
            Assert.Equal(Vector128.Create(
                (ushort)65535, 65535, 65535, 0,
                0, 0, 32768, 32767
            ), resultEdge);
        }

        [Fact]
        public unsafe void BitmaskTest()
        {
            var v1 = Vector128.Create((byte)0b00000001, 0b00000010, 0b00000100, 0b00001000,
                                            0b00010000, 0b00100000, 0b01000000, 0b10000000,
                                            0b00000001, 0b00000010, 0b00000100, 0b00001000,
                                            0b00010000, 0b10100000, 0b01000000, 0b10000000);
            var v2 = Vector128.Create((ushort)0b1100001001100001, 0b0000000000000010, 0b0000000000000100, 0b0000000000001000,
                                    0b0000000000010000, 0b0000000000100000, 0b0000000001000000, 0b0000000010000000);

            var v3 = Vector128.Create(0b10000000000000000000000000000001, 0b00000000000111111000000000000010,
                                      0b00000000000000000000000000000100, 0b10000000000000000000000000001000);

            var bitmask_b = PackedSimd.Bitmask(v1);
            var bitmask_s = PackedSimd.Bitmask(v2);
            var bitmask_i = PackedSimd.Bitmask(v3);

            var v128emsb_b = Vector128.ExtractMostSignificantBits(v1);
            var v128emsb_s = Vector128.ExtractMostSignificantBits(v2);
            var v128emsb_i = Vector128.ExtractMostSignificantBits(v3);

            Assert.Equal(0b1010000010000000, bitmask_b);
            Assert.Equal(0b1, bitmask_s);
            Assert.Equal(0b1001, bitmask_i);

            Assert.Equal(v128emsb_b, (uint)bitmask_b);
            Assert.Equal(v128emsb_s, (uint)bitmask_s);
            Assert.Equal(v128emsb_i, (uint)bitmask_i);
        }
    }
}
