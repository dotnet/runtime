// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;

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

        private static string? ResolveReadTestPath()
        {
            string path = Path.Combine(AppContext.BaseDirectory, ReadTestFileName);
            return File.Exists(path) ? path : null;
        }
    }
}
