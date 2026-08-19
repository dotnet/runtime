// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Wasm;
using Xunit;

namespace System.Runtime.Intrinsics.Wasm.Tests
{
    [PlatformSpecific(TestPlatforms.Browser)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/123011", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsCoreCLR))]
    public sealed class RelaxedSimdTests
    {
        [Fact]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(RelaxedSimd))]
        public unsafe void RelaxedSimdIsSupportedReflects()
        {
            MethodInfo methodInfo = typeof(RelaxedSimd).GetMethod("get_IsSupported");
            Assert.NotNull(methodInfo);
            Assert.Equal(RelaxedSimd.IsSupported, methodInfo.Invoke(null, null));
        }

        [ConditionalFact(typeof(RelaxedSimd), nameof(RelaxedSimd.IsSupported))]
        public unsafe void DotProductByteSByteMatchesScalar()
        {
            // Per the finished spec, `a` is signed and `b` is unsigned-7-bit. When every lane
            // of `b` is in [0, 127] every implementation must match a straightforward
            // pairwise (sbyte, byte) -> int16 multiply-add.
            var s = Vector128.Create((sbyte)-1, 2, -3, 4, -5, 6, -7, 8, -9, 10, -11, 12, -13, 14, -15, 16);
            var u = Vector128.Create((byte)2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3);

            Vector128<short> actual = RelaxedSimd.DotProduct(s, u);

            for (int i = 0; i < 8; i++)
            {
                short expected = (short)(s[2 * i] * u[2 * i] + s[2 * i + 1] * u[2 * i + 1]);
                Assert.Equal(expected, actual[i]);
            }
        }

        [ConditionalFact(typeof(RelaxedSimd), nameof(RelaxedSimd.IsSupported))]
        public unsafe void DotProductAddByteSByteMatchesScalar()
        {
            var s = Vector128.Create((sbyte)-1, 2, -3, 4, -5, 6, -7, 8, -9, 10, -11, 12, -13, 14, -15, 16);
            var u = Vector128.Create((byte)2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3);
            var acc = Vector128.Create(100, 200, 300, 400);

            Vector128<int> actual = RelaxedSimd.DotProductAdd(s, u, acc);

            for (int i = 0; i < 4; i++)
            {
                int expected = acc[i];
                for (int j = 0; j < 4; j++)
                    expected += s[4 * i + j] * u[4 * i + j];
                Assert.Equal(expected, actual[i]);
            }
        }

        [ConditionalFact(typeof(RelaxedSimd), nameof(RelaxedSimd.IsSupported))]
        public unsafe void MultiplyAddFloatMatchesScalarApproximately()
        {
            // Relaxed FMA may or may not round the intermediate product; verify the result is
            // within a small relative tolerance of the unfused result. float.Epsilon is a
            // subnormal (~1.4e-45) and is not a meaningful ULP scale for this comparison, so we
            // use a plain relative epsilon calibrated for single precision.
            var a = Vector128.Create(1.5f, 2.25f, -3.125f, 4.0f);
            var b = Vector128.Create(2.0f, -1.5f, 0.5f, 6.25f);
            var c = Vector128.Create(0.5f, 1.0f, -0.25f, -2.0f);

            Vector128<float> actual = RelaxedSimd.MultiplyAddEstimate(a, b, c);
            Vector128<float> unfused = (a * b) + c;

            const float RelativeTolerance = 1e-5f;
            for (int i = 0; i < 4; i++)
            {
                float tolerance = Math.Max(Math.Abs(unfused[i]), 1.0f) * RelativeTolerance;
                Assert.True(Math.Abs(actual[i] - unfused[i]) <= tolerance,
                    $"lane {i}: relaxed FMA {actual[i]} differs from unfused {unfused[i]} by more than {tolerance}");
            }
        }

        [ConditionalFact(typeof(RelaxedSimd), nameof(RelaxedSimd.IsSupported))]
        public unsafe void LaneSelectAllOnesAllZerosBehavesLikeConditionalSelect()
        {
            // For mask lanes that are all-ones or all-zeros the relaxed lane select must match
            // the deterministic semantics.
            var left = Vector128.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            var right = Vector128.Create((byte)17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            var mask = Vector128.Create((byte)0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00,
                                                   0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00);

            Vector128<byte> actual = RelaxedSimd.LaneSelect(left, right, mask);
            Vector128<byte> expected = Vector128.ConditionalSelect(mask, left, right);

            Assert.Equal(expected, actual);
        }

        [ConditionalFact(typeof(RelaxedSimd), nameof(RelaxedSimd.IsSupported))]
        public unsafe void SwizzleInRangeMatchesVector128Shuffle()
        {
            // For index lanes in [0, 16) the relaxed swizzle must agree with Vector128.Shuffle.
            var v = Vector128.Create((byte)10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160);
            var idx = Vector128.Create((byte)15, 0, 14, 1, 13, 2, 12, 3, 11, 4, 10, 5, 9, 6, 8, 7);

            Vector128<byte> actual = RelaxedSimd.Swizzle(v, idx);
            Vector128<byte> expected = Vector128.Shuffle(v, idx);

            Assert.Equal(expected, actual);
        }
    }
}
