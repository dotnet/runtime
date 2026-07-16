// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;

namespace System.Tests
{
    /// <summary>
    /// Streams operator test vectors out of the <c>readtest.in</c> file that ships with the Intel(R)
    /// Decimal Floating-Point Math Library so the ported <see cref="Numerics.Decimal32" />,
    /// <see cref="Numerics.Decimal64" />, and <see cref="Numerics.Decimal128" /> operators can be
    /// cross-validated bit-for-bit against the reference implementation.
    /// </summary>
    /// <remarks>
    /// The file is very large (tens of thousands of vectors spanning hundreds of operations) and is not
    /// redistributed with the runtime, so the theories that consume this data are gated on the
    /// <see cref="IsAvailable" /> condition and skip unless an interested developer copies <c>readtest.in</c>
    /// next to the test assembly (that is, into <see cref="AppContext.BaseDirectory" />). This keeps the
    /// exhaustive validation available for local or outer-loop runs without redistributing the file or
    /// paying its cost on every test pass. Gating on a file the developer deliberately drops into the
    /// output directory (rather than an environment variable naming an arbitrary path) avoids reading a
    /// file from a location an unrelated process could influence and is no worse than shipping it.
    ///
    /// Each line is whitespace separated as <c>op rnd a b result flags</c> for binary operations
    /// (arithmetic, comparison, <c>copySign</c>, and the min/max family) or <c>op rnd a result flags</c>
    /// for unary operations (<c>abs</c>, <c>negate</c>) and predicates (<c>isNaN</c>, <c>isInf</c>, and
    /// friends). Only round-to-nearest-even (<c>rnd == 0</c>) rows whose operands are bracketed hex bit
    /// patterns are consumed; rows using decimal-string or named operands (for example <c>QNaN</c> or
    /// <c>Infinity</c>), other rounding modes, and the trailing IEEE exception-flags column are ignored.
    /// Bid128 operands appear both as <c>[hi,lo]</c> (arithmetic) and <c>[32hex]</c> (comparison); both
    /// encodings are handled.
    /// </remarks>
    public static class DecimalIeee754IntelTestData
    {
        private const string ReadTestFileName = "readtest.in";

        private static readonly string? s_readTestPath = ResolveReadTestPath();

        private static readonly HashSet<string> s_bid32Arithmetic = new() { "bid32_add", "bid32_sub", "bid32_mul", "bid32_div" };
        private static readonly HashSet<string> s_bid64Arithmetic = new() { "bid64_add", "bid64_sub", "bid64_mul", "bid64_div" };
        private static readonly HashSet<string> s_bid128Arithmetic = new() { "bid128_add", "bid128_sub", "bid128_mul", "bid128_div" };

        private static readonly HashSet<string> s_bid32Comparison = new() { "bid32_quiet_equal", "bid32_quiet_not_equal", "bid32_quiet_less", "bid32_quiet_greater", "bid32_quiet_less_equal", "bid32_quiet_greater_equal" };
        private static readonly HashSet<string> s_bid64Comparison = new() { "bid64_quiet_equal", "bid64_quiet_not_equal", "bid64_quiet_less", "bid64_quiet_greater", "bid64_quiet_less_equal", "bid64_quiet_greater_equal" };
        private static readonly HashSet<string> s_bid128Comparison = new() { "bid128_quiet_equal", "bid128_quiet_not_equal", "bid128_quiet_less", "bid128_quiet_greater", "bid128_quiet_less_equal", "bid128_quiet_greater_equal" };

        private static readonly HashSet<string> s_bid32Unary = new() { "bid32_abs", "bid32_negate" };
        private static readonly HashSet<string> s_bid64Unary = new() { "bid64_abs", "bid64_negate" };
        private static readonly HashSet<string> s_bid128Unary = new() { "bid128_abs", "bid128_negate" };

        // Only copySign is cross-validated here. Intel's minnum/maxnum/minnum_mag/maxnum_mag implement the
        // IEEE 754-2008 minNum/maxNum operations, where-as .NET's MinNumber/MaxNumber (and the magnitude
        // variants) implement the IEEE 754-2019 minimumNumber/maximumNumber operations. These differ on
        // signed-zero ordering, signaling-NaN quieting, and which cohort member is returned when the two
        // operands are numerically equal, so the Intel vectors are not a valid oracle for that family.
        private static readonly HashSet<string> s_bid32BinaryValue = new() { "bid32_copySign" };
        private static readonly HashSet<string> s_bid64BinaryValue = new() { "bid64_copySign" };
        private static readonly HashSet<string> s_bid128BinaryValue = new() { "bid128_copySign" };

        private static readonly HashSet<string> s_bid32Predicate = new() { "bid32_isNaN", "bid32_isInf", "bid32_isFinite", "bid32_isSigned", "bid32_isNormal", "bid32_isSubnormal" };
        private static readonly HashSet<string> s_bid64Predicate = new() { "bid64_isNaN", "bid64_isInf", "bid64_isFinite", "bid64_isSigned", "bid64_isNormal", "bid64_isSubnormal" };
        private static readonly HashSet<string> s_bid128Predicate = new() { "bid128_isNaN", "bid128_isInf", "bid128_isFinite", "bid128_isSigned", "bid128_isNormal", "bid128_isSubnormal" };

        // Integer -> decimal (the .NET implicit/explicit constructors use the IEEE convertFromInt semantics: exact
        // when the value fits, otherwise rounded to the format precision, with the preferred exponent of zero).
        private static readonly HashSet<string> s_bid32FromInteger = new() { "bid32_from_int32", "bid32_from_int64", "bid32_from_uint32", "bid32_from_uint64" };
        private static readonly HashSet<string> s_bid64FromInteger = new() { "bid64_from_int32", "bid64_from_int64", "bid64_from_uint32", "bid64_from_uint64" };
        private static readonly HashSet<string> s_bid128FromInteger = new() { "bid128_from_int32", "bid128_from_int64", "bid128_from_uint32", "bid128_from_uint64" };

        // Decimal -> integer. Only the round-toward-zero (`_int`) family is consumed because the .NET explicit
        // operators truncate toward zero. Intel reports out-of-range/NaN operands by returning a sentinel and
        // raising the invalid flag, where-as .NET saturates (unchecked) or throws (checked); such rows are skipped
        // here (invalid-flagged) and the saturation/overflow behavior is covered by the oracle-derived vectors.
        private static readonly HashSet<string> s_bid32ToInteger = new() { "bid32_to_int8_int", "bid32_to_int16_int", "bid32_to_int32_int", "bid32_to_int64_int", "bid32_to_uint8_int", "bid32_to_uint16_int", "bid32_to_uint32_int", "bid32_to_uint64_int" };
        private static readonly HashSet<string> s_bid64ToInteger = new() { "bid64_to_int8_int", "bid64_to_int16_int", "bid64_to_int32_int", "bid64_to_int64_int", "bid64_to_uint8_int", "bid64_to_uint16_int", "bid64_to_uint32_int", "bid64_to_uint64_int" };
        private static readonly HashSet<string> s_bid128ToInteger = new() { "bid128_to_int8_int", "bid128_to_int16_int", "bid128_to_int32_int", "bid128_to_int64_int", "bid128_to_uint8_int", "bid128_to_uint16_int", "bid128_to_uint32_int", "bid128_to_uint64_int" };

        // Decimal -> binary float and binary float -> decimal (correctly rounded). Only binary32/binary64 map to a
        // .NET type (float/double); binary80/binary128 are ignored. NaN operands are skipped so the payload
        // convention differences between the two libraries do not produce spurious failures.
        private static readonly HashSet<string> s_bid32ToBinary = new() { "bid32_to_binary32", "bid32_to_binary64" };
        private static readonly HashSet<string> s_bid64ToBinary = new() { "bid64_to_binary32", "bid64_to_binary64" };
        private static readonly HashSet<string> s_bid128ToBinary = new() { "bid128_to_binary32", "bid128_to_binary64" };

        private static readonly HashSet<string> s_bid32FromBinary = new() { "binary32_to_bid32", "binary64_to_bid32" };
        private static readonly HashSet<string> s_bid64FromBinary = new() { "binary32_to_bid64", "binary64_to_bid64" };
        private static readonly HashSet<string> s_bid128FromBinary = new() { "binary32_to_bid128", "binary64_to_bid128" };

        // Cross-format decimal conversions, grouped by the target format (widening is exact, narrowing rounds). NaN
        // operands are skipped for the same payload-convention reason as the binary float conversions.
        private static readonly HashSet<string> s_bid32Cross = new() { "bid64_to_bid32", "bid128_to_bid32" };
        private static readonly HashSet<string> s_bid64Cross = new() { "bid32_to_bid64", "bid128_to_bid64" };
        private static readonly HashSet<string> s_bid128Cross = new() { "bid32_to_bid128", "bid64_to_bid128" };

        // Truncating remainder (fmod, the C# `%` operator), not the round-to-nearest IEEE 754 remainder.
        private static readonly HashSet<string> s_bid32Modulus = new() { "bid32_fmod" };
        private static readonly HashSet<string> s_bid64Modulus = new() { "bid64_fmod" };
        private static readonly HashSet<string> s_bid128Modulus = new() { "bid128_fmod" };

        // Round to an integral value under each rounding mode, mapping onto the .NET Round/Ceiling/Floor/Truncate
        // surface. `round_integral_exact` takes the mode from the rounding-context column, so only its
        // round-to-nearest-even (rnd == 0) rows are consumed here; the mode-named variants ignore that column.
        private static readonly HashSet<string> s_bid32RoundIntegral = new() { "bid32_round_integral_exact", "bid32_round_integral_nearest_even", "bid32_round_integral_nearest_away", "bid32_round_integral_negative", "bid32_round_integral_positive", "bid32_round_integral_zero" };
        private static readonly HashSet<string> s_bid64RoundIntegral = new() { "bid64_round_integral_exact", "bid64_round_integral_nearest_even", "bid64_round_integral_nearest_away", "bid64_round_integral_negative", "bid64_round_integral_positive", "bid64_round_integral_zero" };
        private static readonly HashSet<string> s_bid128RoundIntegral = new() { "bid128_round_integral_exact", "bid128_round_integral_nearest_even", "bid128_round_integral_nearest_away", "bid128_round_integral_negative", "bid128_round_integral_positive", "bid128_round_integral_zero" };

        // ScaleB (the `int` scaling-exponent variant). NaN operands are skipped for the same payload-convention
        // reason as RoundIntegral; invalid-flagged rows (overflow/underflow beyond the format range) are also
        // skipped because Intel reports them via a sentinel plus the invalid flag rather than the saturated result.
        private static readonly HashSet<string> s_bid32ScaleB = new() { "bid32_scalbn" };
        private static readonly HashSet<string> s_bid64ScaleB = new() { "bid64_scalbn" };
        private static readonly HashSet<string> s_bid128ScaleB = new() { "bid128_scalbn" };

        // BitIncrement/BitDecrement (IEEE 754 nextUp/nextDown). NaN operands are skipped for the same
        // payload-convention reason as RoundIntegral.
        private static readonly HashSet<string> s_bid32BitIncrement = new() { "bid32_nextup" };
        private static readonly HashSet<string> s_bid64BitIncrement = new() { "bid64_nextup" };
        private static readonly HashSet<string> s_bid128BitIncrement = new() { "bid128_nextup" };
        private static readonly HashSet<string> s_bid32BitDecrement = new() { "bid32_nextdown" };
        private static readonly HashSet<string> s_bid64BitDecrement = new() { "bid64_nextdown" };
        private static readonly HashSet<string> s_bid128BitDecrement = new() { "bid128_nextdown" };

        // ILogB. Only finite, non-zero operands are consumed (invalid-flagged rows cover zero/infinity/NaN, where
        // Intel's C99 ilogb sentinels diverge from the .NET int.MinValue/int.MaxValue contract).
        private static readonly HashSet<string> s_bid32ILogB = new() { "bid32_ilogb" };
        private static readonly HashSet<string> s_bid64ILogB = new() { "bid64_ilogb" };
        private static readonly HashSet<string> s_bid128ILogB = new() { "bid128_ilogb" };

        // FusedMultiplyAdd. Every reference row is exercised, including NaN payload propagation and the invalid
        // operations (0*Inf, Inf + opposite Inf) that quiet to the canonical NaN; all results match bit-exact.
        private static readonly HashSet<string> s_bid32Fma = new() { "bid32_fma" };
        private static readonly HashSet<string> s_bid64Fma = new() { "bid64_fma" };
        private static readonly HashSet<string> s_bid128Fma = new() { "bid128_fma" };

        /// <summary>
        /// Gets a value indicating whether the Intel <c>readtest.in</c> reference vectors are available,
        /// gating the theories that consume them.
        /// </summary>
        public static bool IsAvailable => s_readTestPath is not null;

        public static IEnumerable<object[]> Decimal32Arithmetic()
        {
            foreach (string[] fields in EnumerateRows(s_bid32Arithmetic))
            {
                if (TryParseBid32(fields[2], out uint left) && TryParseBid32(fields[3], out uint right) && TryParseBid32(fields[4], out uint expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), left, right, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal64Arithmetic()
        {
            foreach (string[] fields in EnumerateRows(s_bid64Arithmetic))
            {
                if (TryParseBid64(fields[2], out ulong left) && TryParseBid64(fields[3], out ulong right) && TryParseBid64(fields[4], out ulong expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), left, right, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal128Arithmetic()
        {
            foreach (string[] fields in EnumerateRows(s_bid128Arithmetic))
            {
                if (TryParseBid128(fields[2], out UInt128 left) && TryParseBid128(fields[3], out UInt128 right) && TryParseBid128(fields[4], out UInt128 expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), left, right, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal32Comparison()
        {
            foreach (string[] fields in EnumerateRows(s_bid32Comparison))
            {
                if (TryParseBid32(fields[2], out uint left) && TryParseBid32(fields[3], out uint right) && TryParseComparisonResult(fields[4], out bool expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), left, right, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal64Comparison()
        {
            foreach (string[] fields in EnumerateRows(s_bid64Comparison))
            {
                if (TryParseBid64(fields[2], out ulong left) && TryParseBid64(fields[3], out ulong right) && TryParseComparisonResult(fields[4], out bool expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), left, right, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal128Comparison()
        {
            foreach (string[] fields in EnumerateRows(s_bid128Comparison))
            {
                if (TryParseBid128(fields[2], out UInt128 left) && TryParseBid128(fields[3], out UInt128 right) && TryParseComparisonResult(fields[4], out bool expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), left, right, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal32Unary()
        {
            foreach (string[] fields in EnumerateRows(s_bid32Unary))
            {
                if (TryParseBid32(fields[2], out uint value) && TryParseBid32(fields[3], out uint expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal64Unary()
        {
            foreach (string[] fields in EnumerateRows(s_bid64Unary))
            {
                if (TryParseBid64(fields[2], out ulong value) && TryParseBid64(fields[3], out ulong expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal128Unary()
        {
            foreach (string[] fields in EnumerateRows(s_bid128Unary))
            {
                if (TryParseBid128(fields[2], out UInt128 value) && TryParseBid128(fields[3], out UInt128 expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal32BinaryValue()
        {
            foreach (string[] fields in EnumerateRows(s_bid32BinaryValue))
            {
                if (TryParseBid32(fields[2], out uint left) && TryParseBid32(fields[3], out uint right) && TryParseBid32(fields[4], out uint expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), left, right, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal64BinaryValue()
        {
            foreach (string[] fields in EnumerateRows(s_bid64BinaryValue))
            {
                if (TryParseBid64(fields[2], out ulong left) && TryParseBid64(fields[3], out ulong right) && TryParseBid64(fields[4], out ulong expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), left, right, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal128BinaryValue()
        {
            foreach (string[] fields in EnumerateRows(s_bid128BinaryValue))
            {
                if (TryParseBid128(fields[2], out UInt128 left) && TryParseBid128(fields[3], out UInt128 right) && TryParseBid128(fields[4], out UInt128 expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), left, right, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal32Predicate()
        {
            foreach (string[] fields in EnumerateRows(s_bid32Predicate))
            {
                if (TryParseBid32(fields[2], out uint value) && TryParseComparisonResult(fields[3], out bool expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal64Predicate()
        {
            foreach (string[] fields in EnumerateRows(s_bid64Predicate))
            {
                if (TryParseBid64(fields[2], out ulong value) && TryParseComparisonResult(fields[3], out bool expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal128Predicate()
        {
            foreach (string[] fields in EnumerateRows(s_bid128Predicate))
            {
                if (TryParseBid128(fields[2], out UInt128 value) && TryParseComparisonResult(fields[3], out bool expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal32FromInteger()
        {
            foreach (string[] fields in EnumerateRows(s_bid32FromInteger))
            {
                if (TryParseBid32(fields[3], out uint expected))
                {
                    yield return new object[] { IntegerSourceType(fields[0]), fields[2], expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal64FromInteger()
        {
            foreach (string[] fields in EnumerateRows(s_bid64FromInteger))
            {
                if (TryParseBid64(fields[3], out ulong expected))
                {
                    yield return new object[] { IntegerSourceType(fields[0]), fields[2], expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal128FromInteger()
        {
            foreach (string[] fields in EnumerateRows(s_bid128FromInteger))
            {
                if (TryParseBid128(fields[3], out UInt128 expected))
                {
                    yield return new object[] { IntegerSourceType(fields[0]), fields[2], expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal32ToInteger()
        {
            foreach (string[] fields in EnumerateRows(s_bid32ToInteger))
            {
                if (TryParseBid32(fields[2], out uint value) && !IsInvalidFlagged(fields[4]))
                {
                    yield return new object[] { IntegerTargetType(fields[0]), value, fields[3] };
                }
            }
        }

        public static IEnumerable<object[]> Decimal64ToInteger()
        {
            foreach (string[] fields in EnumerateRows(s_bid64ToInteger))
            {
                if (TryParseBid64(fields[2], out ulong value) && !IsInvalidFlagged(fields[4]))
                {
                    yield return new object[] { IntegerTargetType(fields[0]), value, fields[3] };
                }
            }
        }

        public static IEnumerable<object[]> Decimal128ToInteger()
        {
            foreach (string[] fields in EnumerateRows(s_bid128ToInteger))
            {
                if (TryParseBid128(fields[2], out UInt128 value) && !IsInvalidFlagged(fields[4]))
                {
                    yield return new object[] { IntegerTargetType(fields[0]), value, fields[3] };
                }
            }
        }

        public static IEnumerable<object[]> Decimal32ToBinary()
        {
            foreach (string[] fields in EnumerateRows(s_bid32ToBinary))
            {
                if (TryParseBid32(fields[2], out uint value) && !IsBid32NaN(value) && TryParseHexBits(fields[3], out ulong expected))
                {
                    yield return new object[] { BinaryTargetType(fields[0]), value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal64ToBinary()
        {
            foreach (string[] fields in EnumerateRows(s_bid64ToBinary))
            {
                if (TryParseBid64(fields[2], out ulong value) && !IsBid64NaN(value) && TryParseHexBits(fields[3], out ulong expected))
                {
                    yield return new object[] { BinaryTargetType(fields[0]), value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal128ToBinary()
        {
            foreach (string[] fields in EnumerateRows(s_bid128ToBinary))
            {
                if (TryParseBid128(fields[2], out UInt128 value) && !IsBid128NaN(value) && TryParseHexBits(fields[3], out ulong expected))
                {
                    yield return new object[] { BinaryTargetType(fields[0]), value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal32FromBinary()
        {
            foreach (string[] fields in EnumerateRows(s_bid32FromBinary))
            {
                if (TryParseHexBits(fields[2], out ulong value) && !IsBinaryNaN(BinarySourceType(fields[0]), value) && TryParseBid32(fields[3], out uint expected))
                {
                    yield return new object[] { BinarySourceType(fields[0]), value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal64FromBinary()
        {
            foreach (string[] fields in EnumerateRows(s_bid64FromBinary))
            {
                if (TryParseHexBits(fields[2], out ulong value) && !IsBinaryNaN(BinarySourceType(fields[0]), value) && TryParseBid64(fields[3], out ulong expected))
                {
                    yield return new object[] { BinarySourceType(fields[0]), value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal128FromBinary()
        {
            foreach (string[] fields in EnumerateRows(s_bid128FromBinary))
            {
                if (TryParseHexBits(fields[2], out ulong value) && !IsBinaryNaN(BinarySourceType(fields[0]), value) && TryParseBid128(fields[3], out UInt128 expected))
                {
                    yield return new object[] { BinarySourceType(fields[0]), value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal32Cross()
        {
            foreach (string[] fields in EnumerateRows(s_bid32Cross))
            {
                if (TryParseDecimalSource(fields[0], fields[2], out UInt128 value) && TryParseBid32(fields[3], out uint expected))
                {
                    yield return new object[] { DecimalSourceType(fields[0]), value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal64Cross()
        {
            foreach (string[] fields in EnumerateRows(s_bid64Cross))
            {
                if (TryParseDecimalSource(fields[0], fields[2], out UInt128 value) && TryParseBid64(fields[3], out ulong expected))
                {
                    yield return new object[] { DecimalSourceType(fields[0]), value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal128Cross()
        {
            foreach (string[] fields in EnumerateRows(s_bid128Cross))
            {
                if (TryParseDecimalSource(fields[0], fields[2], out UInt128 value) && TryParseBid128(fields[3], out UInt128 expected))
                {
                    yield return new object[] { DecimalSourceType(fields[0]), value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal32Modulus()
        {
            foreach (string[] fields in EnumerateRows(s_bid32Modulus))
            {
                if (TryParseBid32(fields[2], out uint left) && TryParseBid32(fields[3], out uint right) && TryParseBid32(fields[4], out uint expected))
                {
                    yield return new object[] { left, right, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal64Modulus()
        {
            foreach (string[] fields in EnumerateRows(s_bid64Modulus))
            {
                if (TryParseBid64(fields[2], out ulong left) && TryParseBid64(fields[3], out ulong right) && TryParseBid64(fields[4], out ulong expected))
                {
                    yield return new object[] { left, right, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal128Modulus()
        {
            foreach (string[] fields in EnumerateRows(s_bid128Modulus))
            {
                if (TryParseBid128(fields[2], out UInt128 left) && TryParseBid128(fields[3], out UInt128 right) && TryParseBid128(fields[4], out UInt128 expected))
                {
                    yield return new object[] { left, right, expected };
                }
            }
        }

        // NaN operands are skipped: rounding leaves the value's payload untouched, but Intel canonicalizes and quiets
        // NaNs so its result column would not match the raw operand bits. The mode is taken from the operation name.
        public static IEnumerable<object[]> Decimal32RoundIntegral()
        {
            foreach (string[] fields in EnumerateRows(s_bid32RoundIntegral))
            {
                if (TryParseBid32(fields[2], out uint value) && !IsBid32NaN(value) && TryParseBid32(fields[3], out uint expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal64RoundIntegral()
        {
            foreach (string[] fields in EnumerateRows(s_bid64RoundIntegral))
            {
                if (TryParseBid64(fields[2], out ulong value) && !IsBid64NaN(value) && TryParseBid64(fields[3], out ulong expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal128RoundIntegral()
        {
            foreach (string[] fields in EnumerateRows(s_bid128RoundIntegral))
            {
                if (TryParseBid128(fields[2], out UInt128 value) && !IsBid128NaN(value) && TryParseBid128(fields[3], out UInt128 expected))
                {
                    yield return new object[] { OperationSuffix(fields[0]), value, expected };
                }
            }
        }

        // NaN operands are skipped for the same payload-quieting reason as RoundIntegral.
        public static IEnumerable<object[]> Decimal32BitIncrement()
        {
            foreach (string[] fields in EnumerateRows(s_bid32BitIncrement))
            {
                if (TryParseBid32(fields[2], out uint value) && !IsBid32NaN(value) && TryParseBid32(fields[3], out uint expected))
                {
                    yield return new object[] { value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal64BitIncrement()
        {
            foreach (string[] fields in EnumerateRows(s_bid64BitIncrement))
            {
                if (TryParseBid64(fields[2], out ulong value) && !IsBid64NaN(value) && TryParseBid64(fields[3], out ulong expected))
                {
                    yield return new object[] { value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal128BitIncrement()
        {
            foreach (string[] fields in EnumerateRows(s_bid128BitIncrement))
            {
                if (TryParseBid128(fields[2], out UInt128 value) && !IsBid128NaN(value) && TryParseBid128(fields[3], out UInt128 expected))
                {
                    yield return new object[] { value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal32BitDecrement()
        {
            foreach (string[] fields in EnumerateRows(s_bid32BitDecrement))
            {
                if (TryParseBid32(fields[2], out uint value) && !IsBid32NaN(value) && TryParseBid32(fields[3], out uint expected))
                {
                    yield return new object[] { value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal64BitDecrement()
        {
            foreach (string[] fields in EnumerateRows(s_bid64BitDecrement))
            {
                if (TryParseBid64(fields[2], out ulong value) && !IsBid64NaN(value) && TryParseBid64(fields[3], out ulong expected))
                {
                    yield return new object[] { value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal128BitDecrement()
        {
            foreach (string[] fields in EnumerateRows(s_bid128BitDecrement))
            {
                if (TryParseBid128(fields[2], out UInt128 value) && !IsBid128NaN(value) && TryParseBid128(fields[3], out UInt128 expected))
                {
                    yield return new object[] { value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal32ScaleB()
        {
            foreach (string[] fields in EnumerateRows(s_bid32ScaleB))
            {
                if ((fields.Length >= 6) && TryParseBid32(fields[2], out uint value) && !IsBid32NaN(value) && TryParseScaleAmount(fields[3], out int n) && TryParseBid32(fields[4], out uint expected) && !IsInvalidFlagged(fields[5]))
                {
                    yield return new object[] { value, n, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal64ScaleB()
        {
            foreach (string[] fields in EnumerateRows(s_bid64ScaleB))
            {
                if ((fields.Length >= 6) && TryParseBid64(fields[2], out ulong value) && !IsBid64NaN(value) && TryParseScaleAmount(fields[3], out int n) && TryParseBid64(fields[4], out ulong expected) && !IsInvalidFlagged(fields[5]))
                {
                    yield return new object[] { value, n, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal128ScaleB()
        {
            foreach (string[] fields in EnumerateRows(s_bid128ScaleB))
            {
                if ((fields.Length >= 6) && TryParseBid128(fields[2], out UInt128 value) && !IsBid128NaN(value) && TryParseScaleAmount(fields[3], out int n) && TryParseBid128(fields[4], out UInt128 expected) && !IsInvalidFlagged(fields[5]))
                {
                    yield return new object[] { value, n, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal32ILogB()
        {
            foreach (string[] fields in EnumerateRows(s_bid32ILogB))
            {
                if (TryParseBid32(fields[2], out uint value) && !IsInvalidFlagged(fields[4]) && int.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int expected))
                {
                    yield return new object[] { value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal64ILogB()
        {
            foreach (string[] fields in EnumerateRows(s_bid64ILogB))
            {
                if (TryParseBid64(fields[2], out ulong value) && !IsInvalidFlagged(fields[4]) && int.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int expected))
                {
                    yield return new object[] { value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal128ILogB()
        {
            foreach (string[] fields in EnumerateRows(s_bid128ILogB))
            {
                if (TryParseBid128(fields[2], out UInt128 value) && !IsInvalidFlagged(fields[4]) && int.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int expected))
                {
                    yield return new object[] { value, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal32FusedMultiplyAdd()
        {
            foreach (string[] fields in EnumerateRows(s_bid32Fma))
            {
                if ((fields.Length >= 6) && TryParseBid32(fields[2], out uint x) && TryParseBid32(fields[3], out uint y) && TryParseBid32(fields[4], out uint z) && TryParseBid32(fields[5], out uint expected))
                {
                    yield return new object[] { x, y, z, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal64FusedMultiplyAdd()
        {
            foreach (string[] fields in EnumerateRows(s_bid64Fma))
            {
                if ((fields.Length >= 6) && TryParseBid64(fields[2], out ulong x) && TryParseBid64(fields[3], out ulong y) && TryParseBid64(fields[4], out ulong z) && TryParseBid64(fields[5], out ulong expected))
                {
                    yield return new object[] { x, y, z, expected };
                }
            }
        }

        public static IEnumerable<object[]> Decimal128FusedMultiplyAdd()
        {
            foreach (string[] fields in EnumerateRows(s_bid128Fma))
            {
                if ((fields.Length >= 6) && TryParseBid128(fields[2], out UInt128 x) && TryParseBid128(fields[3], out UInt128 y) && TryParseBid128(fields[4], out UInt128 z) && TryParseBid128(fields[5], out UInt128 expected))
                {
                    yield return new object[] { x, y, z, expected };
                }
            }
        }

        // For `bidNN_from_<type>` the integer source type is the trailing token; for `bidNN_to_<type>_int` it is the
        // third underscore-separated token; for the binary and cross families it is the leading or third token.
        private static string IntegerSourceType(string operation) => operation.Substring(operation.LastIndexOf('_') + 1);

        private static string IntegerTargetType(string operation) => NthToken(operation, 2);

        private static string BinaryTargetType(string operation) => NthToken(operation, 2);

        private static string BinarySourceType(string operation) => NthToken(operation, 0);

        private static string DecimalSourceType(string operation) => NthToken(operation, 0);

        // Extracts the zero-based, underscore-delimited token from a fixed-format operation name without the
        // array and per-token substring allocations that `string.Split('_')` would incur on every vector row.
        private static string NthToken(string operation, int index)
        {
            ReadOnlySpan<char> remaining = operation;

            for (int i = 0; i < index; i++)
            {
                remaining = remaining.Slice(remaining.IndexOf('_') + 1);
            }

            int end = remaining.IndexOf('_');
            return (end < 0 ? remaining : remaining.Slice(0, end)).ToString();
        }

        private static bool TryParseDecimalSource(string operation, string token, out UInt128 value)
        {
            switch (DecimalSourceType(operation))
            {
                case "bid32":
                    if (TryParseBid32(token, out uint u32) && !IsBid32NaN(u32))
                    {
                        value = u32;
                        return true;
                    }
                    break;

                case "bid64":
                    if (TryParseBid64(token, out ulong u64) && !IsBid64NaN(u64))
                    {
                        value = u64;
                        return true;
                    }
                    break;

                case "bid128":
                    if (TryParseBid128(token, out UInt128 u128) && !IsBid128NaN(u128))
                    {
                        value = u128;
                        return true;
                    }
                    break;
            }

            value = default;
            return false;
        }

        private static bool IsInvalidFlagged(string token)
        {
            ReadOnlySpan<char> span = token;

            if (span.StartsWith("0x") || span.StartsWith("0X"))
            {
                span = span.Slice(2);
            }

            // A row whose flags cannot be parsed is treated as invalid so it is skipped rather than trusted.
            if (!uint.TryParse(span, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint flags))
            {
                return true;
            }

            // Bit 0 (0x01) is the IEEE invalid-operation flag Intel raises for NaN/infinity/out-of-range operands.
            return (flags & 0x01) != 0;
        }

        private static bool TryParseHexBits(string token, out ulong value)
        {
            value = 0;

            if ((token.Length < 2) || (token[0] != '[') || (token[^1] != ']'))
            {
                return false;
            }

            return ulong.TryParse(token.AsSpan(1, token.Length - 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
        }

        private static bool IsBid32NaN(uint value) => (value & 0x7C000000u) == 0x7C000000u;

        private static bool IsBid64NaN(ulong value) => (value & 0x7C00000000000000ul) == 0x7C00000000000000ul;

        private static bool IsBid128NaN(UInt128 value) => (value & new UInt128(0x7C00000000000000ul, 0x0)) == new UInt128(0x7C00000000000000ul, 0x0);

        private static bool IsBinaryNaN(string binaryType, ulong bits) => binaryType switch
        {
            "binary32" => ((bits & 0x7F800000ul) == 0x7F800000ul) && ((bits & 0x7FFFFFul) != 0),
            "binary64" => ((bits & 0x7FF0000000000000ul) == 0x7FF0000000000000ul) && ((bits & 0xFFFFFFFFFFFFFul) != 0),
            _ => false,
        };

        private static IEnumerable<string[]> EnumerateRows(HashSet<string> operations)
        {
            string? path = s_readTestPath;

            if (path is null)
            {
                yield break;
            }

            foreach (string line in File.ReadLines(path))
            {
                string[] fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

                // A usable row is `op 0 a b result flags`; anything shorter, in a different rounding mode,
                // or naming an operation we do not care about is skipped.
                if ((fields.Length >= 5) && (fields[1] == "0") && operations.Contains(fields[0]))
                {
                    yield return fields;
                }
            }
        }

        private static string OperationSuffix(string operation) => operation.Substring(operation.IndexOf('_') + 1);

        // The scaling amount is a plain decimal integer; Intel occasionally writes it float-style (for example
        // `1.0`), which is accepted only when the fractional part is entirely zero.
        private static bool TryParseScaleAmount(string token, out int value)
        {
            int dot = token.IndexOf('.');

            if (dot >= 0)
            {
                for (int i = dot + 1; i < token.Length; i++)
                {
                    if (token[i] != '0')
                    {
                        value = 0;
                        return false;
                    }
                }

                token = token.Substring(0, dot);
            }

            return int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseComparisonResult(string token, out bool value)
        {
            switch (token)
            {
                case "0":
                    value = false;
                    return true;

                case "1":
                    value = true;
                    return true;

                default:
                    value = false;
                    return false;
            }
        }

        private static bool TryParseBid32(string token, out uint value)
        {
            value = 0;

            if ((token.Length < 2) || (token[0] != '[') || (token[^1] != ']'))
            {
                return false;
            }

            // Operands are not always zero-padded to the full width, so parse the inner hex directly and
            // let uint.TryParse reject anything wider than the type.
            return uint.TryParse(token.AsSpan(1, token.Length - 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseBid64(string token, out ulong value)
        {
            value = 0;

            if ((token.Length < 2) || (token[0] != '[') || (token[^1] != ']'))
            {
                return false;
            }

            return ulong.TryParse(token.AsSpan(1, token.Length - 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseBid128(string token, out UInt128 value)
        {
            value = default;

            if ((token.Length < 2) || (token[0] != '[') || (token[^1] != ']'))
            {
                return false;
            }

            ReadOnlySpan<char> inner = token.AsSpan(1, token.Length - 2);
            int comma = inner.IndexOf(',');

            if (comma >= 0)
            {
                // Bid128 arithmetic operands are encoded as `[hi,lo]`.
                if (!ulong.TryParse(inner.Slice(0, comma), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ulong upper) ||
                    !ulong.TryParse(inner.Slice(comma + 1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ulong lower))
                {
                    return false;
                }

                value = new UInt128(upper, lower);
                return true;
            }

            // Bid128 comparison operands and every bid128 result are encoded as a single `[hex]`.
            return UInt128.TryParse(inner, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
        }

        // Intel's readtest.in encodes unsigned integer operands and results as a signed decimal string that is
        // only congruent to the intended unsigned value modulo 2^width (for example uint32 4294967295 is written
        // as "-1", and uint64 9223372036854775807 as "-9223372036854775809", whose magnitude even exceeds the
        // signed range). Parse through BigInteger and reduce modulo 2^width to recover the unsigned value.
        internal static byte ParseUInt8(string s) => unchecked((byte)(uint)(BigInteger.Parse(s, CultureInfo.InvariantCulture) & 0xFF));

        internal static ushort ParseUInt16(string s) => unchecked((ushort)(uint)(BigInteger.Parse(s, CultureInfo.InvariantCulture) & 0xFFFF));

        internal static uint ParseUInt32(string s) => unchecked((uint)(BigInteger.Parse(s, CultureInfo.InvariantCulture) & 0xFFFFFFFF));

        internal static ulong ParseUInt64(string s) => unchecked((ulong)(BigInteger.Parse(s, CultureInfo.InvariantCulture) & ulong.MaxValue));

        private static string? ResolveReadTestPath()
        {
            string path = Path.Combine(AppContext.BaseDirectory, ReadTestFileName);
            return File.Exists(path) ? path : null;
        }
    }
}
