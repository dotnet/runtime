// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Wasm;
using Xunit;

public sealed class PackedSimdTests
{
    [Fact]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(PackedSimd))]
    public static unsafe void PackedSimdIsSupported()
    {
        MethodInfo? methodInfo = typeof(PackedSimd).GetProperty(nameof(PackedSimd.IsSupported))?.GetGetMethod();
        Assert.NotNull(methodInfo);
        Assert.Equal(PackedSimd.IsSupported, methodInfo.Invoke(null, null));
        Assert.Equal(PackedSimd.IsSupported, Vector128.IsHardwareAccelerated);
        Assert.True(PackedSimd.IsSupported);
    }

    [Fact]
    public static unsafe void BasicArithmeticTest()
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
    public static unsafe void BitwiseOperationsTest()
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
    public static unsafe void ShiftOperationsTest()
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
    public static unsafe void ComparisonOperationsTest()
    {
        var v1 = Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f);
        var v2 = Vector128.Create(4.0f, 3.0f, 2.0f, 1.0f);

        var minResult = PackedSimd.Min(v1, v2);
        var maxResult = PackedSimd.Max(v1, v2);

        Assert.Equal(Vector128.Create(1.0f, 2.0f, 2.0f, 1.0f), minResult);
        Assert.Equal(Vector128.Create(4.0f, 3.0f, 3.0f, 4.0f), maxResult);
    }

    [Fact]
    public static unsafe void FloatingPointOperationsTest()
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
    public static unsafe void NotTests()
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
    public static unsafe void BitwiseSelectTest()
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
    public static unsafe void LoadStoreTest()
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
    public static unsafe void ExtractInsertScalarTest()
    {
        var v = Vector128.Create(1, 2, 3, 4);

        int extracted = PackedSimd.ExtractScalar(v, 2);
        var modified = PackedSimd.ReplaceScalar(v, 2, 10);

        Assert.Equal(3, extracted);
        Assert.Equal(Vector128.Create(1, 2, 10, 4), modified);
    }

    [Fact]
    public static unsafe void Vector128GetWithElementTest()
    {
        var vi = Vector128.Create(10, 20, 30, 40);

        // GetElement/WithElement with a constant index, and ToScalar (GetElement(0)).
        Assert.Equal(10, vi.GetElement(0));
        Assert.Equal(30, vi.GetElement(2));
        Assert.Equal(10, vi.ToScalar());
        Assert.Equal(Vector128.Create(10, 20, 99, 40), vi.WithElement(2, 99));

        // Small elements must sign/zero-extend based on the element type.
        var vsb = Vector128.Create((sbyte)-1, -2, -3, -4, -5, -6, -7, -8,
                                   -9, -10, -11, -12, -13, -14, -15, -16);
        Assert.Equal((sbyte)-3, vsb.GetElement(2));

        var vb = Vector128.Create((byte)255, 1, 2, 3, 4, 5, 6, 7,
                                  8, 9, 10, 11, 12, 13, 14, 15);
        Assert.Equal((byte)255, vb.GetElement(0));

        // 64-bit and floating-point lanes.
        var vl = Vector128.Create(1L, 2L);
        Assert.Equal(2L, vl.GetElement(1));
        Assert.Equal(Vector128.Create(1L, 42L), vl.WithElement(1, 42L));

        var vf = Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f);
        Assert.Equal(3.0f, vf.GetElement(2));
        Assert.Equal(Vector128.Create(1.0f, 2.0f, 7.0f, 4.0f), vf.WithElement(2, 7.0f));

        var vd = Vector128.Create(1.0, 2.0);
        Assert.Equal(2.0, vd.GetElement(1));
        Assert.Equal(9.0, vd.WithElement(0, 9.0).ToScalar());

        // A non-constant index exercises the jump-table fallback in codegen.
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal((i + 1) * 10, vi.GetElement(i));
            Assert.Equal(7, vi.WithElement(i, 7).GetElement(i));
        }
    }

    [Fact]
    public static unsafe void Vector128ShiftTest()
    {
        // Left shift (<<) with a constant count across element widths.
        Assert.Equal(Vector128.Create(4, 8, 12, 16), Vector128.Create(1, 2, 3, 4) << 2);
        Assert.Equal(Vector128.Create(2L, 4L), Vector128.Create(1L, 2L) << 1);
        Assert.Equal(Vector128.Create((short)8, 16, 24, 32, 40, 48, 56, 64),
                     Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 8) << 3);

        // Arithmetic right shift (>>) preserves sign for signed types.
        Assert.Equal(Vector128.Create(-2, -1, 1, 2), Vector128.Create(-8, -4, 4, 8) >> 2);
        Assert.Equal(Vector128.Create((sbyte)-1, -1, 0, 1, -2, 2, -4, 4, -1, -1, 0, 1, -2, 2, -4, 4),
                     Vector128.Create((sbyte)-2, -1, 1, 2, -4, 4, -8, 8, -2, -1, 1, 2, -4, 4, -8, 8) >> 1);

        // Unsigned/logical right shift (>>>) zero-fills.
        Assert.Equal(Vector128.Create(0x3FFFFFFFu, 1u, 2u, 3u),
                     Vector128.Create(0xFFFFFFFFu, 4u, 8u, 12u) >>> 2);
        Assert.Equal(Vector128.Create(unchecked((int)0x3FFFFFFF), 1, 2, 3),
                     Vector128.Create(unchecked((int)0xFFFFFFFF), 4, 8, 12) >>> 2);

        // Non-constant counts exercise the scalar-amount path.
        var v = Vector128.Create(1, 2, 3, 4);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(Vector128.Create(1 << i, 2 << i, 3 << i, 4 << i), v << i);
            Assert.Equal(Vector128.Create(16 >> i, 32 >> i, 48 >> i, 64 >> i),
                         Vector128.Create(16, 32, 48, 64) >> i);
            Assert.Equal(Vector128.Create(16u >>> i, 32u >>> i, 48u >>> i, 64u >>> i),
                         Vector128.Create(16u, 32u, 48u, 64u) >>> i);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T Opaque<T>(T value) => value;

    [Fact]
    public static unsafe void Vector128CreateScalarTest()
    {
        // Non-constant operands force the CreateScalar/CreateScalarUnsafe lowering rather
        // than constant folding to a vector constant.

        // CreateScalar zero-fills the upper elements.
        Assert.Equal(Vector128.Create(42, 0, 0, 0), Vector128.CreateScalar(Opaque(42)));
        Assert.Equal(Vector128.Create(7L, 0L), Vector128.CreateScalar(Opaque(7L)));
        Assert.Equal(Vector128.Create((short)9, 0, 0, 0, 0, 0, 0, 0),
                     Vector128.CreateScalar(Opaque((short)9)));
        Assert.Equal(Vector128.Create((byte)200, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                     Vector128.CreateScalar(Opaque((byte)200)));
        Assert.Equal(Vector128.Create(3.5f, 0.0f, 0.0f, 0.0f), Vector128.CreateScalar(Opaque(3.5f)));
        Assert.Equal(Vector128.Create(6.25, 0.0), Vector128.CreateScalar(Opaque(6.25)));

        // CreateScalarUnsafe leaves the upper elements undefined, so only lane 0 is guaranteed.
        Assert.Equal(42, Vector128.CreateScalarUnsafe(Opaque(42)).ToScalar());
        Assert.Equal(7L, Vector128.CreateScalarUnsafe(Opaque(7L)).ToScalar());
        Assert.Equal((short)9, Vector128.CreateScalarUnsafe(Opaque((short)9)).ToScalar());
        Assert.Equal((byte)200, Vector128.CreateScalarUnsafe(Opaque((byte)200)).ToScalar());
        Assert.Equal(3.5f, Vector128.CreateScalarUnsafe(Opaque(3.5f)).ToScalar());
        Assert.Equal(6.25, Vector128.CreateScalarUnsafe(Opaque(6.25)).ToScalar());
    }

    [Fact]
    public static unsafe void Vector2And3ConversionTest()
    {
        // Non-constant operands force the reinterpret nodes rather than constant folding.

        Vector128<float> v = Opaque(Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f));

        // Narrowing keeps the lower lanes and drops the rest.
        Assert.Equal(new Vector2(1.0f, 2.0f), v.AsVector2());
        Assert.Equal(new Vector3(1.0f, 2.0f, 3.0f), v.AsVector3());

        // AsVector128 widens and zero-fills the upper lanes.
        Assert.Equal(Vector128.Create(1.0f, 2.0f, 0.0f, 0.0f), Opaque(new Vector2(1.0f, 2.0f)).AsVector128());
        Assert.Equal(Vector128.Create(1.0f, 2.0f, 3.0f, 0.0f), Opaque(new Vector3(1.0f, 2.0f, 3.0f)).AsVector128());

        // AsVector128Unsafe leaves the upper lanes undefined, so only the lower lanes are guaranteed.
        Vector128<float> u2 = Opaque(new Vector2(1.0f, 2.0f)).AsVector128Unsafe();
        Assert.Equal(1.0f, u2.GetElement(0));
        Assert.Equal(2.0f, u2.GetElement(1));

        Vector128<float> u3 = Opaque(new Vector3(1.0f, 2.0f, 3.0f)).AsVector128Unsafe();
        Assert.Equal(1.0f, u3.GetElement(0));
        Assert.Equal(2.0f, u3.GetElement(1));
        Assert.Equal(3.0f, u3.GetElement(2));
    }

    [Fact]
    public static unsafe void ConstantShuffleTest()
    {
        // Opaque data with constant indices exercises the Swizzle-based constant-shuffle path.

        Vector128<int> vi = Opaque(Vector128.Create(1, 2, 3, 4));
        Assert.Equal(Vector128.Create(4, 3, 2, 1), Vector128.Shuffle(vi, Vector128.Create(3, 2, 1, 0)));
        // Out-of-range indices zero the corresponding element.
        Assert.Equal(Vector128.Create(1, 2, 3, 0), Vector128.Shuffle(vi, Vector128.Create(0, 1, 2, 99)));

        Vector128<float> vf = Opaque(Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f));
        Assert.Equal(Vector128.Create(4.0f, 3.0f, 2.0f, 1.0f), Vector128.Shuffle(vf, Vector128.Create(3, 2, 1, 0)));

        Vector128<long> vl = Opaque(Vector128.Create(10L, 20L));
        Assert.Equal(Vector128.Create(20L, 10L), Vector128.Shuffle(vl, Vector128.Create(1L, 0L)));

        Vector128<short> vs = Opaque(Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 8));
        Assert.Equal(Vector128.Create((short)8, 7, 6, 5, 4, 3, 2, 1),
                     Vector128.Shuffle(vs, Vector128.Create((short)7, 6, 5, 4, 3, 2, 1, 0)));

        Vector128<byte> vb = Opaque(Vector128.Create((byte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15));
        Assert.Equal(Vector128.Create((byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0),
                     Vector128.Shuffle(vb, Vector128.Create((byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)));
        // Out-of-range byte indices zero their element.
        Assert.Equal(Vector128.Create((byte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 0),
                     Vector128.Shuffle(vb, Vector128.Create((byte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 200)));
    }

    [Fact]
    public static unsafe void VariableShuffleTest()
    {
        // Opaque indices force the variable-index path (byte-index expansion + i8x16.swizzle).

        Vector128<int> vi  = Opaque(Vector128.Create(1, 2, 3, 4));
        Vector128<int> ii  = Opaque(Vector128.Create(3, 2, 1, 0));
        Assert.Equal(Vector128.Create(4, 3, 2, 1), Vector128.Shuffle(vi, ii));
        // Out-of-range indices zero the corresponding element.
        Assert.Equal(Vector128.Create(1, 2, 3, 0), Vector128.Shuffle(vi, Opaque(Vector128.Create(0, 1, 2, 99))));

        Vector128<float> vf = Opaque(Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f));
        Assert.Equal(Vector128.Create(4.0f, 3.0f, 2.0f, 1.0f),
                     Vector128.Shuffle(vf, Opaque(Vector128.Create(3, 2, 1, 0))));

        Vector128<long> vl = Opaque(Vector128.Create(10L, 20L));
        Assert.Equal(Vector128.Create(20L, 10L), Vector128.Shuffle(vl, Opaque(Vector128.Create(1L, 0L))));
        // Out-of-range long index zeroes its element.
        Assert.Equal(Vector128.Create(10L, 0L), Vector128.Shuffle(vl, Opaque(Vector128.Create(0L, 5L))));

        Vector128<short> vs = Opaque(Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 8));
        Assert.Equal(Vector128.Create((short)8, 7, 6, 5, 4, 3, 2, 1),
                     Vector128.Shuffle(vs, Opaque(Vector128.Create((short)7, 6, 5, 4, 3, 2, 1, 0))));
        // Out-of-range short index zeroes its element.
        Assert.Equal(Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 0),
                     Vector128.Shuffle(vs, Opaque(Vector128.Create((short)0, 1, 2, 3, 4, 5, 6, 99))));

        Vector128<byte> vb = Opaque(Vector128.Create((byte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15));
        Assert.Equal(Vector128.Create((byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0),
                     Vector128.Shuffle(vb, Opaque(Vector128.Create((byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0))));
        // Out-of-range byte index zeroes its element.
        Assert.Equal(Vector128.Create((byte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 0),
                     Vector128.Shuffle(vb, Opaque(Vector128.Create((byte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 200))));
    }

    [Fact]
    public static unsafe void ReverseTest()
    {
        // Reverse lowers to a constant Shuffle; Opaque data forces the shuffle rather than folding.

        Vector128<int> vi = Opaque(Vector128.Create(1, 2, 3, 4));
        Assert.Equal(Vector128.Create(4, 3, 2, 1), Vector128.Reverse(vi));

        Vector128<float> vf = Opaque(Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f));
        Assert.Equal(Vector128.Create(4.0f, 3.0f, 2.0f, 1.0f), Vector128.Reverse(vf));

        Vector128<long> vl = Opaque(Vector128.Create(10L, 20L));
        Assert.Equal(Vector128.Create(20L, 10L), Vector128.Reverse(vl));

        Vector128<double> vd = Opaque(Vector128.Create(1.5, 2.5));
        Assert.Equal(Vector128.Create(2.5, 1.5), Vector128.Reverse(vd));

        Vector128<short> vs = Opaque(Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 8));
        Assert.Equal(Vector128.Create((short)8, 7, 6, 5, 4, 3, 2, 1), Vector128.Reverse(vs));

        Vector128<byte> vb = Opaque(Vector128.Create((byte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15));
        Assert.Equal(Vector128.Create((byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0), Vector128.Reverse(vb));
    }

    [Fact]
    public static unsafe void SumTest()
    {
        // Opaque data forces the shuffle + add reduction rather than constant folding.

        Assert.Equal(10, Vector128.Sum(Opaque(Vector128.Create(1, 2, 3, 4))));
        Assert.Equal(10.0f, Vector128.Sum(Opaque(Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f))));
        Assert.Equal(30L, Vector128.Sum(Opaque(Vector128.Create(10L, 20L))));
        Assert.Equal(4.0, Vector128.Sum(Opaque(Vector128.Create(1.5, 2.5))));
        Assert.Equal((short)36, Vector128.Sum(Opaque(Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 8))));
        Assert.Equal((byte)120,
                     Vector128.Sum(Opaque(Vector128.Create((byte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15))));
    }

    [Fact]
    public static unsafe void ZipTest()
    {
        // Opaque operands force the shuffle + OR interleave rather than constant folding.

        Vector128<int> li = Opaque(Vector128.Create(1, 2, 3, 4));
        Vector128<int> ri = Opaque(Vector128.Create(5, 6, 7, 8));
        Assert.Equal(Vector128.Create(1, 5, 2, 6), Vector128.ZipLower(li, ri));
        Assert.Equal(Vector128.Create(3, 7, 4, 8), Vector128.ZipUpper(li, ri));

        Vector128<float> lf = Opaque(Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f));
        Vector128<float> rf = Opaque(Vector128.Create(5.0f, 6.0f, 7.0f, 8.0f));
        Assert.Equal(Vector128.Create(1.0f, 5.0f, 2.0f, 6.0f), Vector128.ZipLower(lf, rf));
        Assert.Equal(Vector128.Create(3.0f, 7.0f, 4.0f, 8.0f), Vector128.ZipUpper(lf, rf));

        Vector128<long> ll = Opaque(Vector128.Create(10L, 20L));
        Vector128<long> rl = Opaque(Vector128.Create(30L, 40L));
        Assert.Equal(Vector128.Create(10L, 30L), Vector128.ZipLower(ll, rl));
        Assert.Equal(Vector128.Create(20L, 40L), Vector128.ZipUpper(ll, rl));

        Vector128<short> ls = Opaque(Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 8));
        Vector128<short> rs = Opaque(Vector128.Create((short)9, 10, 11, 12, 13, 14, 15, 16));
        Assert.Equal(Vector128.Create((short)1, 9, 2, 10, 3, 11, 4, 12), Vector128.ZipLower(ls, rs));
        Assert.Equal(Vector128.Create((short)5, 13, 6, 14, 7, 15, 8, 16), Vector128.ZipUpper(ls, rs));

        Vector128<byte> lb = Opaque(Vector128.Create((byte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15));
        Vector128<byte> rb = Opaque(Vector128.Create((byte)16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31));
        Assert.Equal(Vector128.Create((byte)0, 16, 1, 17, 2, 18, 3, 19, 4, 20, 5, 21, 6, 22, 7, 23),
                     Vector128.ZipLower(lb, rb));
        Assert.Equal(Vector128.Create((byte)8, 24, 9, 25, 10, 26, 11, 27, 12, 28, 13, 29, 14, 30, 15, 31),
                     Vector128.ZipUpper(lb, rb));
    }

    [Fact]
    public static unsafe void UnzipTest()
    {
        // Opaque operands force the shuffle + OR deinterleave rather than constant folding.

        Vector128<int> li = Opaque(Vector128.Create(1, 2, 3, 4));
        Vector128<int> ri = Opaque(Vector128.Create(5, 6, 7, 8));
        Assert.Equal(Vector128.Create(1, 3, 5, 7), Vector128.UnzipEven(li, ri));
        Assert.Equal(Vector128.Create(2, 4, 6, 8), Vector128.UnzipOdd(li, ri));

        Vector128<float> lf = Opaque(Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f));
        Vector128<float> rf = Opaque(Vector128.Create(5.0f, 6.0f, 7.0f, 8.0f));
        Assert.Equal(Vector128.Create(1.0f, 3.0f, 5.0f, 7.0f), Vector128.UnzipEven(lf, rf));
        Assert.Equal(Vector128.Create(2.0f, 4.0f, 6.0f, 8.0f), Vector128.UnzipOdd(lf, rf));

        Vector128<long> ll = Opaque(Vector128.Create(10L, 20L));
        Vector128<long> rl = Opaque(Vector128.Create(30L, 40L));
        Assert.Equal(Vector128.Create(10L, 30L), Vector128.UnzipEven(ll, rl));
        Assert.Equal(Vector128.Create(20L, 40L), Vector128.UnzipOdd(ll, rl));

        Vector128<short> ls = Opaque(Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 8));
        Vector128<short> rs = Opaque(Vector128.Create((short)9, 10, 11, 12, 13, 14, 15, 16));
        Assert.Equal(Vector128.Create((short)1, 3, 5, 7, 9, 11, 13, 15), Vector128.UnzipEven(ls, rs));
        Assert.Equal(Vector128.Create((short)2, 4, 6, 8, 10, 12, 14, 16), Vector128.UnzipOdd(ls, rs));

        Vector128<byte> lb = Opaque(Vector128.Create((byte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15));
        Vector128<byte> rb = Opaque(Vector128.Create((byte)16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31));
        Assert.Equal(Vector128.Create((byte)0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30),
                     Vector128.UnzipEven(lb, rb));
        Assert.Equal(Vector128.Create((byte)1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31),
                     Vector128.UnzipOdd(lb, rb));
    }

    [Fact]
    public static unsafe void ConcatTest()
    {
        // Opaque operands force the shuffle + OR concat rather than constant folding.

        Vector128<int> li = Opaque(Vector128.Create(1, 2, 3, 4));
        Vector128<int> ri = Opaque(Vector128.Create(5, 6, 7, 8));
        Assert.Equal(Vector128.Create(1, 2, 5, 6), Vector128.ConcatLowerLower(li, ri));
        Assert.Equal(Vector128.Create(3, 4, 5, 6), Vector128.ConcatUpperLower(li, ri));
        Assert.Equal(Vector128.Create(1, 2, 7, 8), Vector128.ConcatLowerUpper(li, ri));
        Assert.Equal(Vector128.Create(3, 4, 7, 8), Vector128.ConcatUpperUpper(li, ri));

        Vector128<float> lf = Opaque(Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f));
        Vector128<float> rf = Opaque(Vector128.Create(5.0f, 6.0f, 7.0f, 8.0f));
        Assert.Equal(Vector128.Create(1.0f, 2.0f, 5.0f, 6.0f), Vector128.ConcatLowerLower(lf, rf));
        Assert.Equal(Vector128.Create(3.0f, 4.0f, 7.0f, 8.0f), Vector128.ConcatUpperUpper(lf, rf));

        Vector128<long> ll = Opaque(Vector128.Create(10L, 20L));
        Vector128<long> rl = Opaque(Vector128.Create(30L, 40L));
        Assert.Equal(Vector128.Create(10L, 30L), Vector128.ConcatLowerLower(ll, rl));
        Assert.Equal(Vector128.Create(20L, 40L), Vector128.ConcatUpperUpper(ll, rl));
        Assert.Equal(Vector128.Create(20L, 30L), Vector128.ConcatUpperLower(ll, rl));

        Vector128<short> ls = Opaque(Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 8));
        Vector128<short> rs = Opaque(Vector128.Create((short)9, 10, 11, 12, 13, 14, 15, 16));
        Assert.Equal(Vector128.Create((short)1, 2, 3, 4, 9, 10, 11, 12), Vector128.ConcatLowerLower(ls, rs));
        Assert.Equal(Vector128.Create((short)5, 6, 7, 8, 13, 14, 15, 16), Vector128.ConcatUpperUpper(ls, rs));

        Vector128<byte> lb = Opaque(Vector128.Create((byte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15));
        Vector128<byte> rb = Opaque(Vector128.Create((byte)16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31));
        Assert.Equal(Vector128.Create((byte)0, 1, 2, 3, 4, 5, 6, 7, 16, 17, 18, 19, 20, 21, 22, 23),
                     Vector128.ConcatLowerLower(lb, rb));
        Assert.Equal(Vector128.Create((byte)8, 9, 10, 11, 12, 13, 14, 15, 24, 25, 26, 27, 28, 29, 30, 31),
                     Vector128.ConcatUpperUpper(lb, rb));
    }

    [Fact]
    public static unsafe void CreateAlternatingSequenceTest()
    {
        // Opaque operands force the broadcast + zip path rather than a constant vector.

        int ei = Opaque(3);
        int oi = Opaque(7);
        Assert.Equal(Vector128.Create(3, 7, 3, 7), Vector128.CreateAlternatingSequence(ei, oi));

        float ef = Opaque(1.5f);
        float of = Opaque(-2.5f);
        Assert.Equal(Vector128.Create(1.5f, -2.5f, 1.5f, -2.5f), Vector128.CreateAlternatingSequence(ef, of));

        long el = Opaque(10L);
        long ol = Opaque(-20L);
        Assert.Equal(Vector128.Create(10L, -20L), Vector128.CreateAlternatingSequence(el, ol));

        double ed = Opaque(4.25);
        double od = Opaque(8.5);
        Assert.Equal(Vector128.Create(4.25, 8.5), Vector128.CreateAlternatingSequence(ed, od));

        short es = Opaque((short)5);
        short os = Opaque((short)-6);
        Assert.Equal(Vector128.Create((short)5, -6, 5, -6, 5, -6, 5, -6), Vector128.CreateAlternatingSequence(es, os));

        byte eb = Opaque((byte)1);
        byte ob = Opaque((byte)2);
        Assert.Equal(Vector128.Create((byte)1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2),
                     Vector128.CreateAlternatingSequence(eb, ob));
    }

    [Fact]
    public static unsafe void DotTest()
    {
        // Opaque operands force the Sum(left * right) reduction rather than constant folding.

        Vector128<int> ai = Opaque(Vector128.Create(1, 2, 3, 4));
        Vector128<int> bi = Opaque(Vector128.Create(5, 6, 7, 8));
        Assert.Equal(1 * 5 + 2 * 6 + 3 * 7 + 4 * 8, Vector128.Dot(ai, bi));

        Vector128<float> af = Opaque(Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f));
        Vector128<float> bf = Opaque(Vector128.Create(5.0f, 6.0f, 7.0f, 8.0f));
        Assert.Equal(1.0f * 5 + 2.0f * 6 + 3.0f * 7 + 4.0f * 8, Vector128.Dot(af, bf));

        Vector128<long> al = Opaque(Vector128.Create(2L, 3L));
        Vector128<long> bl = Opaque(Vector128.Create(4L, 5L));
        Assert.Equal(2L * 4 + 3L * 5, Vector128.Dot(al, bl));

        Vector128<double> ad = Opaque(Vector128.Create(1.5, 2.5));
        Vector128<double> bd = Opaque(Vector128.Create(3.5, 4.5));
        Assert.Equal(1.5 * 3.5 + 2.5 * 4.5, Vector128.Dot(ad, bd));

        Vector128<short> ash = Opaque(Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 8));
        Vector128<short> bsh = Opaque(Vector128.Create((short)1, 1, 1, 1, 1, 1, 1, 1));
        Assert.Equal((short)(1 + 2 + 3 + 4 + 5 + 6 + 7 + 8), Vector128.Dot(ash, bsh));

        Vector128<byte> ab = Opaque(Vector128.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8));
        Vector128<byte> bb = Opaque(Vector128.Create((byte)1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1));
        Assert.Equal((byte)((1 + 2 + 3 + 4 + 5 + 6 + 7 + 8) * 2), Vector128.Dot(ab, bb));
    }

    [Fact]
    public static unsafe void WidenTest()
    {
        // Opaque operands force the widening intrinsics rather than constant folding. The float
        // WidenUpper is the newly-enabled case (shuffle + promote); the integer forms exercise the
        // sign/zero-extend widening instructions.

        Vector128<float> f = Opaque(Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f));
        Assert.Equal(Vector128.Create(1.0, 2.0), Vector128.WidenLower(f));
        Assert.Equal(Vector128.Create(3.0, 4.0), Vector128.WidenUpper(f));

        Vector128<int> i = Opaque(Vector128.Create(-1, 2, -3, 4));
        Assert.Equal(Vector128.Create(-1L, 2L), Vector128.WidenLower(i));
        Assert.Equal(Vector128.Create(-3L, 4L), Vector128.WidenUpper(i));

        Vector128<short> s = Opaque(Vector128.Create((short)-1, 2, -3, 4, -5, 6, -7, 8));
        Assert.Equal(Vector128.Create(-1, 2, -3, 4), Vector128.WidenLower(s));
        Assert.Equal(Vector128.Create(-5, 6, -7, 8), Vector128.WidenUpper(s));

        Vector128<byte> b = Opaque(Vector128.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16));
        Assert.Equal(Vector128.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8), Vector128.WidenLower(b));
        Assert.Equal(Vector128.Create((ushort)9, 10, 11, 12, 13, 14, 15, 16), Vector128.WidenUpper(b));
    }

    [Fact]
    public static unsafe void NarrowTest()
    {
        // Opaque operands force the narrowing builder (byte-granularity shuffle for integers,
        // demote + shuffle for double->float) rather than constant folding.

        Vector128<short> sh1 = Opaque(Vector128.Create((short)0x0100, 0x0302, 0x0504, 0x0706, 0x0908, 0x0B0A, 0x0D0C, 0x0F0E));
        Vector128<short> sh2 = Opaque(Vector128.Create(unchecked((short)0x1110), 0x1312, 0x1514, 0x1716, 0x1918, 0x1B1A, 0x1D1C, 0x1F1E));
        Assert.Equal(Vector128.Create((sbyte)0x00, 0x02, 0x04, 0x06, 0x08, 0x0A, 0x0C, 0x0E, 0x10, 0x12, 0x14, 0x16, 0x18, 0x1A, 0x1C, 0x1E), Vector128.Narrow(sh1, sh2));

        Vector128<int> i1 = Opaque(Vector128.Create(0x00030002, 0x00050004, 0x00070006, 0x00090008));
        Vector128<int> i2 = Opaque(Vector128.Create(0x000B000A, 0x000D000C, 0x000F000E, 0x00110010));
        Assert.Equal(Vector128.Create((short)2, 4, 6, 8, 10, 12, 14, 16), Vector128.Narrow(i1, i2));

        Vector128<long> l1 = Opaque(Vector128.Create(0x0000000200000001L, 0x0000000400000003L));
        Vector128<long> l2 = Opaque(Vector128.Create(0x0000000600000005L, 0x0000000800000007L));
        Assert.Equal(Vector128.Create(1, 3, 5, 7), Vector128.Narrow(l1, l2));

        Vector128<double> d1 = Opaque(Vector128.Create(1.5, 2.5));
        Vector128<double> d2 = Opaque(Vector128.Create(3.5, 4.5));
        Assert.Equal(Vector128.Create(1.5f, 2.5f, 3.5f, 4.5f), Vector128.Narrow(d1, d2));
    }

    [Fact]
    public static unsafe void NarrowWithSaturationTest()
    {
        // Opaque operands force the saturating-narrow builder (clamp to the narrow range then narrow).

        Vector128<short> sh1 = Opaque(Vector128.Create((short)-200, 200, -100, 100, 0, 50, -50, 127));
        Vector128<short> sh2 = Opaque(Vector128.Create((short)128, 300, -300, 1, -1, 2, -2, 3));
        Assert.Equal(Vector128.Create((sbyte)-128, 127, -100, 100, 0, 50, -50, 127, 127, 127, -128, 1, -1, 2, -2, 3), Vector128.NarrowWithSaturation(sh1, sh2));

        // Unsigned source values above 0x7FFF must clamp as unsigned magnitude (not be treated as negative).
        Vector128<ushort> ush1 = Opaque(Vector128.Create((ushort)0, 255, 256, 40000, 0x8000, 0xFFFF, 100, 200));
        Vector128<ushort> ush2 = Opaque(Vector128.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8));
        Assert.Equal(Vector128.Create((byte)0, 255, 255, 255, 255, 255, 100, 200, 1, 2, 3, 4, 5, 6, 7, 8), Vector128.NarrowWithSaturation(ush1, ush2));

        Vector128<int> i1 = Opaque(Vector128.Create(-40000, 40000, 100, -100));
        Vector128<int> i2 = Opaque(Vector128.Create(32767, -32768, 32768, -32769));
        Assert.Equal(Vector128.Create((short)-32768, 32767, 100, -100, 32767, -32768, 32767, -32768), Vector128.NarrowWithSaturation(i1, i2));

        Vector128<long> l1 = Opaque(Vector128.Create(long.MinValue, long.MaxValue));
        Vector128<long> l2 = Opaque(Vector128.Create(5L, -5L));
        Assert.Equal(Vector128.Create(int.MinValue, int.MaxValue, 5, -5), Vector128.NarrowWithSaturation(l1, l2));

        Vector128<ulong> ul1 = Opaque(Vector128.Create(0UL, 0xFFFFFFFFFFFFFFFFUL));
        Vector128<ulong> ul2 = Opaque(Vector128.Create(0x1_0000_0000UL, 7UL));
        Assert.Equal(Vector128.Create(0u, uint.MaxValue, uint.MaxValue, 7u), Vector128.NarrowWithSaturation(ul1, ul2));

        Vector128<double> d1 = Opaque(Vector128.Create(1.5, 2.5));
        Vector128<double> d2 = Opaque(Vector128.Create(3.5, 4.5));
        Assert.Equal(Vector128.Create(1.5f, 2.5f, 3.5f, 4.5f), Vector128.NarrowWithSaturation(d1, d2));
    }

    [Fact]
    public static unsafe void MinMaxScalarTest()
    {
        // Scalar Math.Min/Max and the Number/Magnitude variants now lower through the SIMD builders on
        // WASM by wrapping the operands in a single-element vector and extracting the result.

        Assert.Equal(5.0, Math.Max(Opaque(3.0), Opaque(5.0)));
        Assert.Equal(3.0, Math.Min(Opaque(3.0), Opaque(5.0)));
        Assert.Equal(5.0f, Math.Max(Opaque(3.0f), Opaque(5.0f)));
        Assert.Equal(3.0f, Math.Min(Opaque(3.0f), Opaque(5.0f)));

        // Max/Min propagate NaN.
        Assert.True(double.IsNaN(Math.Max(Opaque(double.NaN), Opaque(5.0))));
        Assert.True(double.IsNaN(Math.Min(Opaque(5.0), Opaque(double.NaN))));

        // The Number variants ignore NaN.
        Assert.Equal(5.0, double.MaxNumber(Opaque(double.NaN), Opaque(5.0)));
        Assert.Equal(5.0, double.MinNumber(Opaque(double.NaN), Opaque(5.0)));
        Assert.Equal(5.0, double.MaxNumber(Opaque(3.0), Opaque(5.0)));
        Assert.Equal(3.0, double.MinNumber(Opaque(3.0), Opaque(5.0)));

        // Magnitude variants compare by absolute value.
        Assert.Equal(-5.0, double.MaxMagnitude(Opaque(-5.0), Opaque(3.0)));
        Assert.Equal(3.0, double.MinMagnitude(Opaque(-5.0), Opaque(3.0)));

        // -0.0 is less than +0.0.
        Assert.False(double.IsNegative(Math.Max(Opaque(-0.0), Opaque(0.0))));
        Assert.True(double.IsNegative(Math.Min(Opaque(-0.0), Opaque(0.0))));
    }

    [Fact]
    public static unsafe void PromotedSimdFieldAccessTest()
    {
        // Individually reading and writing Vector2/3/4 fields promotes the local and lowers the field
        // accesses through GetElement/WithElement rather than memory-based field loads and stores.

        Vector4 v4 = Opaque(new Vector4(1.0f, 2.0f, 3.0f, 4.0f));
        v4.X += 10.0f;
        v4.W = v4.Y + v4.Z;
        Assert.Equal(11.0f, v4.X);
        Assert.Equal(2.0f, v4.Y);
        Assert.Equal(3.0f, v4.Z);
        Assert.Equal(5.0f, v4.W);

        Vector3 v3 = Opaque(new Vector3(1.0f, 2.0f, 3.0f));
        v3.Y = v3.X + v3.Z;
        Assert.Equal(1.0f, v3.X);
        Assert.Equal(4.0f, v3.Y);
        Assert.Equal(3.0f, v3.Z);

        Vector2 v2 = Opaque(new Vector2(1.0f, 2.0f));
        v2.X = v2.Y * 2.0f;
        Assert.Equal(4.0f, v2.X);
        Assert.Equal(2.0f, v2.Y);

        // Plane embeds a Vector3 Normal in a 16-byte value, exercising the SIMD12 field get/set path.
        Plane plane = Opaque(new Plane(1.0f, 2.0f, 3.0f, 4.0f));
        plane.Normal = new Vector3(plane.Normal.X + 10.0f, plane.Normal.Y, plane.Normal.Z);
        Assert.Equal(new Vector3(11.0f, 2.0f, 3.0f), plane.Normal);
        Assert.Equal(4.0f, plane.D);
    }

    [Fact]
    public static unsafe void ExtractMostSignificantBitsConstantFoldTest()
    {
        // A constant input lets value numbering fold ExtractMostSignificantBits to an integer constant;
        // the Opaque overloads keep the runtime path covered so both agree.

        Assert.Equal(0b1010u, Vector128.Create(0, -1, 0, -1).ExtractMostSignificantBits());
        Assert.Equal(0b1010u, Opaque(Vector128.Create(0, -1, 0, -1)).ExtractMostSignificantBits());

        Assert.Equal(0b0101u, Vector128.Create(-1, 0, -1, 0).ExtractMostSignificantBits());

        var bytes = Vector128.Create((byte)0x80, 0, 0x80, 0, 0x80, 0, 0x80, 0, 0x80, 0, 0x80, 0, 0x80, 0, 0x80, 0);
        Assert.Equal(0b0101010101010101u, bytes.ExtractMostSignificantBits());
        Assert.Equal(0b0101010101010101u, Opaque(bytes).ExtractMostSignificantBits());

        Assert.Equal(0b10u, Vector128.Create(0.0, -1.0).ExtractMostSignificantBits());
        Assert.Equal(0b01u, Vector128.Create(-1.0, 0.0).ExtractMostSignificantBits());
    }

    [Fact]
    public static unsafe void ConditionalSelectConstantFoldTest()
    {
        // A constant mask lets value numbering / gtFoldExprHWIntrinsic fold ConditionalSelect to a
        // constant vector as (trueValue & mask) | (falseValue & ~mask); the Opaque overloads keep the
        // runtime BitwiseSelect path covered so both agree.

        var trueValue = Vector128.Create(1, 2, 3, 4);
        var falseValue = Vector128.Create(5, 6, 7, 8);

        // All-bits-set mask selects trueValue
        Assert.Equal(trueValue, Vector128.ConditionalSelect(Vector128<int>.AllBitsSet, trueValue, falseValue));

        // Zero mask selects falseValue
        Assert.Equal(falseValue, Vector128.ConditionalSelect(Vector128<int>.Zero, trueValue, falseValue));

        // Per-lane mix
        var mask = Vector128.Create(-1, 0, -1, 0);
        var expected = Vector128.Create(1, 6, 3, 8);
        Assert.Equal(expected, Vector128.ConditionalSelect(mask, trueValue, falseValue));
        Assert.Equal(expected, Vector128.ConditionalSelect(Opaque(mask), Opaque(trueValue), Opaque(falseValue)));

        // Sub-lane granularity with bytes
        var byteMask = Vector128.Create((byte)0xF0, 0x0F, 0xFF, 0x00, 0xF0, 0x0F, 0xFF, 0x00, 0xF0, 0x0F, 0xFF, 0x00, 0xF0, 0x0F, 0xFF, 0x00);
        var byteTrue = Vector128.Create((byte)0xAA);
        var byteFalse = Vector128.Create((byte)0x55);
        var byteExpected = Vector128.Create((byte)0xA5, 0x5A, 0xAA, 0x55, 0xA5, 0x5A, 0xAA, 0x55, 0xA5, 0x5A, 0xAA, 0x55, 0xA5, 0x5A, 0xAA, 0x55);
        Assert.Equal(byteExpected, Vector128.ConditionalSelect(byteMask, byteTrue, byteFalse));
        Assert.Equal(byteExpected, Vector128.ConditionalSelect(Opaque(byteMask), Opaque(byteTrue), Opaque(byteFalse)));
    }

    [Fact]
    public static unsafe void SaturatingArithmeticTest()
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
    public static unsafe void SaturatingArithmeticSignedTest()
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
    public static unsafe void SaturatingArithmeticForIntegersTest()
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
    public static unsafe void SaturatingArithmeticForUnsignedIntegersTest()
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
    public static unsafe void SaturatingArithmeticEdgeCasesTest()
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
    public static unsafe void WideningOperationsTest()
    {
        var v = Vector128.Create((short)1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000);

        var lowerWidened = PackedSimd.SignExtendWideningLower(v);
        var upperWidened = PackedSimd.SignExtendWideningUpper(v);

        Assert.Equal(Vector128.Create(1000, 2000, 3000, 4000), lowerWidened);
        Assert.Equal(Vector128.Create(5000, 6000, 7000, 8000), upperWidened);
    }

    [Fact]
    public static unsafe void SwizzleTest()
    {
        var v = Vector128.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        var indices = Vector128.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12);

        var swizzled = PackedSimd.Swizzle(v, indices);
        Assert.Equal(Vector128.Create((byte)4, 3, 2, 1, 8, 7, 6, 5, 12, 11, 10, 9, 16, 15, 14, 13), swizzled);
    }

    [Fact]
    public static unsafe void LoadScalarAndSplatTest()
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
    public static unsafe void LoadStoreNullCheckTest()
    {
        Assert.Throws<NullReferenceException>(() => LoadScalarAndSplatVector128(null));
        Assert.Throws<NullReferenceException>(() => LoadScalarVector128(null));
        Assert.Throws<NullReferenceException>(() => LoadWideningVector128(null));
        Assert.Throws<NullReferenceException>(() => LoadScalarAndInsert(null, 2));
        Assert.Throws<NullReferenceException>(() => StoreSelectedScalar(null, 2));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe Vector128<int> LoadScalarAndSplatVector128(int* address)
    {
        return PackedSimd.LoadScalarAndSplatVector128(address);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe Vector128<int> LoadScalarVector128(int* address)
    {
        return PackedSimd.LoadScalarVector128(address);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe Vector128<short> LoadWideningVector128(sbyte* address)
    {
        return PackedSimd.LoadWideningVector128(address);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe Vector128<int> LoadScalarAndInsert(int* address, byte index)
    {
        Vector128<int> vector = Vector128.Create(1, 2, 3, 4);
        return PackedSimd.LoadScalarAndInsert(address, vector, index);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void StoreSelectedScalar(int* address, byte index)
    {
        Vector128<int> vector = Vector128.Create(1, 2, 3, 4);
        PackedSimd.StoreSelectedScalar(address, vector, index);
    }

    [Fact]
    public static unsafe void LoadWideningTest()
    {
        byte[] bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        fixed (byte* ptr = bytes)
        {
            var widened = PackedSimd.LoadWideningVector128(ptr);
            Assert.Equal(Vector128.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8), widened);
        }
    }

    [Fact]
    public static unsafe void StoreSelectedScalarTest()
    {
        var v = Vector128.Create(1, 2, 3, 4);
        int value = 0;
        int* ptr = &value;

        PackedSimd.StoreSelectedScalar(ptr, v, 2);
        Assert.Equal(3, value);
    }

    [Fact]
    public static unsafe void LoadScalarAndInsertTest()
    {
        var v = Vector128.Create(1, 2, 3, 4);
        int newValue = 42;
        int* ptr = &newValue;

        var result = PackedSimd.LoadScalarAndInsert(ptr, v, 2);
        Assert.Equal(Vector128.Create(1, 2, 42, 4), result);
    }

    [Fact]
    public static unsafe void ConversionTest()
    {
        var intVector = Vector128.Create(1, 2, 3, 4);
        var floatVector = Vector128.Create(1.5f, 2.5f, 3.5f, 4.5f);

        var intToFloat = PackedSimd.ConvertToSingle(intVector);
        var floatToDouble = PackedSimd.ConvertToDoubleLower(floatVector);

        Assert.Equal(Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f), intToFloat);
        Assert.Equal(Vector128.Create(1.5, 2.5), floatToDouble);
    }

    [Fact]
    public static unsafe void AddPairwiseWideningTest()
    {
        var bytes = Vector128.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);

        var widened = PackedSimd.AddPairwiseWidening(bytes);
        Assert.Equal(Vector128.Create((ushort)3, 7, 11, 15, 19, 23, 27, 31), widened);
    }

    [Fact]
    public static unsafe void MultiplyWideningTest()
    {
        var shorts = Vector128.Create((short)10, 20, 30, 40, 50, 60, 70, 80);
        var multiplier = Vector128.Create((short)2, 2, 2, 2, 2, 2, 2, 2);

        var lowerResult = PackedSimd.MultiplyWideningLower(shorts, multiplier);
        var upperResult = PackedSimd.MultiplyWideningUpper(shorts, multiplier);

        Assert.Equal(Vector128.Create(20, 40, 60, 80), lowerResult);
        Assert.Equal(Vector128.Create(100, 120, 140, 160), upperResult);
    }

    [Fact]
    public static unsafe void DotProductTest()
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
    public static unsafe void FloatingPointNegationTest()
    {
        var v = Vector128.Create(1.0f, -2.0f, 3.0f, -4.0f);
        var d = Vector128.Create(1.0, -2.0);

        var negatedFloat = PackedSimd.Negate(v);
        var negatedDouble = PackedSimd.Negate(d);

        Assert.Equal(Vector128.Create(-1.0f, 2.0f, -3.0f, 4.0f), negatedFloat);
        Assert.Equal(Vector128.Create(-1.0, 2.0), negatedDouble);
    }

    [Fact]
    public static unsafe void FloatingPointAbsTest()
    {
        var v = Vector128.Create(-1.0f, 2.0f, -3.0f, 4.0f);
        var d = Vector128.Create(-1.0, 2.0);

        var absFloat = PackedSimd.Abs(v);
        var absDouble = PackedSimd.Abs(d);

        Assert.Equal(Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f), absFloat);
        Assert.Equal(Vector128.Create(1.0, 2.0), absDouble);
    }

    [Fact]
    public static unsafe void FloatingPointDivisionTest()
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
    public static unsafe void IntegerAbsTest()
    {
        var bytes = Vector128.Create((sbyte)-1, 2, -3, 4, -5, 6, -7, 8, -9, 10, -11, 12, -13, 14, -15, 16);
        var shorts = Vector128.Create((short)-1, 2, -3, 4, 5, 6, -7, 8);

        var absBytes = PackedSimd.Abs(bytes);
        var absShorts = PackedSimd.Abs(shorts);
        Assert.Equal(Vector128.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16), absBytes);
        Assert.Equal(Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 8), absShorts);
    }

    [Fact]
    public static unsafe void AverageRoundedTest()
    {
        var bytes1 = Vector128.Create((byte)1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31);
        var bytes2 = Vector128.Create((byte)3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33);

        var avgBytes = PackedSimd.AverageRounded(bytes1, bytes2);

        // Average is rounded up: (a + b + 1) >> 1
        Assert.Equal(Vector128.Create((byte)2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32), avgBytes);
    }

    [Fact]
    public static unsafe void MinMaxSignedUnsignedTest()
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
    public static unsafe void SplatTypes()
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
    public static unsafe void LoadScalarAndSplatInfinityTest()
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
    public static unsafe void FloatingPointTruncateTest()
    {
        var v1 = Vector128.Create(1.7f, -2.3f, 3.5f, -4.8f);
        var d1 = Vector128.Create(1.7, -2.3);

        var truncFloat = PackedSimd.Truncate(v1);
        var truncDouble = PackedSimd.Truncate(d1);

        Assert.Equal(Vector128.Create(1.0f, -2.0f, 3.0f, -4.0f), truncFloat);
        Assert.Equal(Vector128.Create(1.0, -2.0), truncDouble);
    }

    [Fact]
    public static unsafe void ComparisonWithNaNTest()
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
    public static unsafe void NativeIntegerArithmeticTest()
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
    public static unsafe void NativeUnsignedIntegerArithmeticTest()
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
    public static unsafe void NativeIntegerLoadStoreTest()
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
    public static unsafe void NativeUnsignedIntegerLoadStoreTest()
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
    public static void NativeIntegerShiftTest()
    {
        var v = Vector128.Create([(nint)16, (nint)(-16), (nint)32, (nint)(-32)]);

        var leftShift = PackedSimd.ShiftLeft(v, 2);
        var rightShiftArith = PackedSimd.ShiftRightArithmetic(v, 2);
        var rightShiftLogical = PackedSimd.ShiftRightLogical(v, 2);
        Assert.Equal(Vector128.Create([(nint)64, (-64), 128, (-128)]), leftShift);
        Assert.Equal(Vector128.Create([(nint)4, (-4), 8, (-8)]), rightShiftArith);
        Assert.Equal(
            Vector128.Create([
                (nint)4,
                unchecked((nint)(unchecked((nuint)(nint)(-16)) >> 2)),
                (nint)8,
                unchecked((nint)(unchecked((nuint)(nint)(-32)) >> 2))
            ]),
            rightShiftLogical);
    }

    [Fact]
    public static void NativeUnsignedIntegerShiftTest()
    {
        var v = Vector128.Create([(nuint)16, unchecked((nuint)(-16)), (nuint)32, unchecked((nuint)(-32))]);

        var leftShift = PackedSimd.ShiftLeft(v, 2);
        var rightShiftLogical = PackedSimd.ShiftRightLogical(v, 2);
        Assert.Equal(Vector128.Create([(nuint)64, unchecked((nuint)(-64)), (nuint)128, unchecked((nuint)(-128))]), leftShift);
        Assert.Equal(
            Vector128.Create([
                (nuint)16 >> 2,
                unchecked((nuint)(-16)) >> 2,
                (nuint)32 >> 2,
                unchecked((nuint)(-32)) >> 2
            ]),
            rightShiftLogical);
    }

    [Fact]
    public static unsafe void ConvertNarrowingSaturateSignedTest()
    {
        var v1 = Vector128.Create(32767, 32768, -32768, -32769);
        var v2 = Vector128.Create(100, 200, -100, -200);

        var result = PackedSimd.ConvertNarrowingSaturateSigned(v1, v2);

        Assert.Equal(Vector128.Create((short)32767, 32767, -32768, -32768, 100, 200, -100, -200), result);
    }

    [Fact]
    public static unsafe void ConvertNarrowingSaturateUnsignedShortToByte()
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
    public static unsafe void ConvertNarrowingSaturateUnsignedIntToUShort()
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
    public static unsafe void BitmaskTest()
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
