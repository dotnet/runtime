// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Tests;
using Xunit;

namespace System.Numerics.Tests
{
    public class DebuggerDisplayTests
    {
        public static TheoryData<uint[], string> DisplayData()
        {
            var data = new TheoryData<uint[], string>
            {
                { new uint[] { 0, 0, 1 }, "18446744073709551616" },
            };

            // On 64-bit, these values fit in <= 4 nuint limbs so DebuggerDisplay
            // shows the full ToString rather than scientific notation.
            if (nint.Size == 8)
            {
                data.Add(new uint[] { 0, 0, 0, 0, 1 }, "340282366920938463463374607431768211456");
                data.Add(new uint[] { 0, 0x12345678, 0, 0xCC00CC00, 0x80808080 },
                    new BigInteger(1, new nuint[] { unchecked((nuint)0x12345678_00000000), unchecked((nuint)0xCC00CC00_00000000), 0x80808080 }).ToString());
            }
            else
            {
                data.Add(new uint[] { 0, 0, 0, 0, 1 }, "3.40282367e+38");
                data.Add(new uint[] { 0, 0x12345678, 0, 0xCC00CC00, 0x80808080 }, "7.33616508e+47");
            }

            return data;
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        [MemberData(nameof(DisplayData))]
        [SkipOnPlatform(TestPlatforms.Browser, "DebuggerDisplayAttribute is stripped on wasm")]
        public void TestDebuggerDisplay(uint[] bits32, string displayString)
        {
            // Convert uint[] test data to nuint[] for the internal constructor
            nuint[] bits;
            if (nint.Size == 8)
            {
                int nuintLen = (bits32.Length + 1) / 2;
                bits = new nuint[nuintLen];
                for (int i = 0; i < bits32.Length; i += 2)
                {
                    ulong lo = bits32[i];
                    ulong hi = (i + 1 < bits32.Length) ? bits32[i + 1] : 0;
                    bits[i / 2] = (nuint)(lo | (hi << 32));
                }
                // Trim trailing zeros
                int len = bits.Length;
                while (len > 0 && bits[len - 1] == 0) len--;
                if (len < bits.Length)
                    bits = bits[..len];
            }
            else
            {
                bits = new nuint[bits32.Length];
                for (int i = 0; i < bits32.Length; i++)
                    bits[i] = bits32[i];
            }

            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                BigInteger positiveValue = new BigInteger(1, bits);
                Assert.Equal(displayString, DebuggerAttributes.ValidateDebuggerDisplayReferences(positiveValue));

                BigInteger negativeValue = new BigInteger(-1, bits);
                Assert.Equal("-" + displayString, DebuggerAttributes.ValidateDebuggerDisplayReferences(negativeValue));
            }
        }
    }
}
