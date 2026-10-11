// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Unit tests for the recognition of constant set membership tests that consist of several (disjoint) ranges
// and/or single values, e.g. "c is >= '0' and <= '9' or >= 'A' and <= 'Z'". The JIT converts these into
// a range check followed by a bit test. All tests compare the result with a simple reference implementation
// over a large part of the value domain.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace optRangeSetRecognition;

public class CRangeSetRecognitionTest
{
    private static readonly int[] s_primes = [2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61];

    private static bool IsPrimeRef(long value) => Array.IndexOf(s_primes, (int)value) >= 0 && value == (int)value;

    private static long[] InterestingLongs()
    {
        var values = new List<long>();
        for (long i = -200; i <= 200; i++)
        {
            values.Add(i);
            values.Add(int.MinValue + i);
            values.Add(int.MaxValue + i);
            values.Add(uint.MaxValue + i);
            values.Add(long.MinValue + (i + 200));
            values.Add(long.MaxValue - (i + 200));
        }
        return values.ToArray();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsVowel(char c) => c is 'a' or 'e' or 'i' or 'o' or 'u' or 'A' or 'E' or 'I' or 'O' or 'U';

    [Fact]
    public static void Vowels()
    {
        for (int i = 0; i <= char.MaxValue; i++)
        {
            char c = (char)i;
            Assert.Equal("aeiouAEIOU".Contains(c), IsVowel(c));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsPrimeInt(int x) => x is 2 or 3 or 5 or 7 or 11 or 13 or 17 or 19 or 23 or 29 or 31 or 37 or 41 or 43 or 47 or 53 or 59 or 61;

    [Fact]
    public static void PrimesInt()
    {
        foreach (long l in InterestingLongs())
        {
            int x = (int)l;
            Assert.Equal(IsPrimeRef(x), IsPrimeInt(x));
        }
        for (int x = -1000; x <= 1000; x++)
        {
            Assert.Equal(IsPrimeRef(x), IsPrimeInt(x));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsPrimeLong(long x) => x is 2 or 3 or 5 or 7 or 11 or 13 or 17 or 19 or 23 or 29 or 31 or 37 or 41 or 43 or 47 or 53 or 59 or 61;

    [Fact]
    public static void PrimesLong()
    {
        foreach (long x in InterestingLongs())
        {
            Assert.Equal(IsPrimeRef(x), IsPrimeLong(x));
        }
        for (long x = -1000; x <= 1000; x++)
        {
            Assert.Equal(IsPrimeRef(x), IsPrimeLong(x));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsAlnumUpper(char c) => c is >= '0' and <= '9' or >= 'A' and <= 'Z';

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsHexDigit(char c) => c is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f';

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsAsciiWhiteSpace(char c) => c is ' ' or >= '\t' and <= '\r';

    // Different ways to write "digit or [A-Z]"
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool DigitOrUpper_IsAsciiHelpers(char c) => char.IsAsciiDigit(c) || char.IsAsciiLetterUpper(c);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool DigitOrUpper_IsAsciiDigitAndCompare(char c) => char.IsAsciiDigit(c) || (c >= 'A' && c <= 'Z');

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool DigitOrUpper_Compares(char c) => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z');

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool DigitOrUpper_Subtractions(char c) => (uint)(c - '0') <= 9 || (uint)(c - 'A') <= 'Z' - 'A';

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool DigitOrUpper_SwitchExpression(char c) => c switch
    {
        >= '0' and <= '9' => true,
        >= 'A' and <= 'Z' => true,
        _ => false,
    };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool DigitOrUpper_Negated(char c) => !(c is >= '0' and <= '9' or >= 'A' and <= 'Z');

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DigitOrUpper_Branches(char c)
    {
        if (c >= '0' && c <= '9')
        {
            return 1;
        }

        if (c >= 'A' && c <= 'Z')
        {
            return 2;
        }

        return 0;
    }

    [Fact]
    public static void DigitOrUpperVariants()
    {
        for (int i = 0; i <= char.MaxValue; i++)
        {
            char c = (char)i;
            bool digit = c >= '0' && c <= '9';
            bool upper = c >= 'A' && c <= 'Z';
            Assert.Equal(digit || upper, DigitOrUpper_IsAsciiHelpers(c));
            Assert.Equal(digit || upper, DigitOrUpper_IsAsciiDigitAndCompare(c));
            Assert.Equal(digit || upper, DigitOrUpper_Compares(c));
            Assert.Equal(digit || upper, DigitOrUpper_Subtractions(c));
            Assert.Equal(digit || upper, DigitOrUpper_SwitchExpression(c));
            Assert.Equal(!(digit || upper), DigitOrUpper_Negated(c));
            Assert.Equal(digit ? 1 : upper ? 2 : 0, DigitOrUpper_Branches(c));
        }
    }
    // Sets that span more than one bit table: the values that do not fit in the first bit table are a single range.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsIdentChar(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_';

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsIdentCharCompares(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsNotIdentChar(char c) => !(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_');

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int IdentCharBranches(char c)
    {
        if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_')
        {
            return 1;
        }

        return 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsBase64Char(char c) => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '+' or '/';

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool WideInt(int c) => c is 3 or 5 or >= 10 and <= 12 or >= 70 and <= 120;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool WideNegativeInt(int c) => c is -100 or -90 or >= -80 and <= -70 or >= 0 and <= 40;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool WideLong(long c) => c is 3 or 5 or >= 10 and <= 12 or >= 70 and <= 120;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool WideUnsigned(uint c) => c is 3 or 5 or >= 10 and <= 12 or >= 70 and <= 120;

    // The values that do not fit in the first bit table are not a single range, this is not converted
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsAsciiPunctuation(char c) => c is >= '!' and <= '/' or >= ':' and <= '@' or >= '[' and <= '`' or >= '{' and <= '~';

    [Fact]
    public static void WideSets()
    {
        for (int i = 0; i <= char.MaxValue; i++)
        {
            char c = (char)i;
            bool ident = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
            Assert.Equal(ident, IsIdentChar(c));
            Assert.Equal(ident, IsIdentCharCompares(c));
            Assert.Equal(!ident, IsNotIdentChar(c));
            Assert.Equal(ident ? 1 : 2, IdentCharBranches(c));
            Assert.Equal((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '+' || c == '/', IsBase64Char(c));
            Assert.Equal((c >= '!' && c <= '/') || (c >= ':' && c <= '@') || (c >= '[' && c <= '`') || (c >= '{' && c <= '~'), IsAsciiPunctuation(c));
        }

        foreach (long l in InterestingLongs())
        {
            CheckWide(l);
        }
        for (long x = -300; x <= 300; x++)
        {
            CheckWide(x);
        }

        static void CheckWide(long x)
        {
            bool wide = x == 3 || x == 5 || (x >= 10 && x <= 12) || (x >= 70 && x <= 120);
            Assert.Equal(wide, WideLong(x));
            if ((x >= int.MinValue) && (x <= int.MaxValue))
            {
                Assert.Equal(wide, WideInt((int)x));
                Assert.Equal((x == -100) || (x == -90) || (x >= -80 && x <= -70) || (x >= 0 && x <= 40), WideNegativeInt((int)x));
            }
            if ((x >= 0) && (x <= uint.MaxValue))
            {
                Assert.Equal(wide, WideUnsigned((uint)x));
            }
        }
    }
    [Fact]
    public static void CharRanges()
    {
        for (int i = 0; i <= char.MaxValue; i++)
        {
            char c = (char)i;
            Assert.Equal((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z'), IsAlnumUpper(c));
            Assert.Equal((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'), IsHexDigit(c));
            Assert.Equal(c == ' ' || (c >= 9 && c <= 13), IsAsciiWhiteSpace(c));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TwoRanges(int c) => c is >= 10 and <= 20 or >= 40 and <= 50;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool NotTwoRanges(int c) => !(c is >= 10 and <= 20 or >= 40 and <= 50);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool NegativeRanges(int c) => c is >= -20 and <= -10 or >= 5 and <= 9;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool MixedRangesAndValues(int c) => c is 3 or >= 10 and <= 20 or 33 or >= 40 and <= 50 or 60;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool UnsignedRanges(uint c) => c is >= 10u and <= 20u or >= 40u and <= 50u;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool UnsignedWrapAround(uint c) => c is >= 0xFFFFFFF0u or <= 3u or (>= 20u and <= 25u);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool EdgeOfDomain(int c) => c is >= int.MinValue and <= int.MinValue + 5 or >= 10 and <= 20;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LongRanges(long c) => c is >= 10 and <= 20 or >= 40 and <= 50;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LongRangesFarFromZero(long c) => c is >= 0x100000000L and <= 0x100000005L or >= 0x100000010L and <= 0x100000020L;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ByteRanges(byte c) => c is >= 10 and <= 20 or >= 40 and <= 50 or >= 100 and <= 110;

    [Fact]
    public static void IntRanges()
    {
        foreach (long l in InterestingLongs())
        {
            CheckInt((int)l);
        }
        for (int x = -1000; x <= 1000; x++)
        {
            CheckInt(x);
        }

        static void CheckInt(int x)
        {
            bool two = (x >= 10 && x <= 20) || (x >= 40 && x <= 50);
            Assert.Equal(two, TwoRanges(x));
            Assert.Equal(!two, NotTwoRanges(x));
            Assert.Equal((x >= -20 && x <= -10) || (x >= 5 && x <= 9), NegativeRanges(x));
            Assert.Equal(x == 3 || x == 33 || x == 60 || two, MixedRangesAndValues(x));
            Assert.Equal((x >= int.MinValue && x <= int.MinValue + 5) || (x >= 10 && x <= 20), EdgeOfDomain(x));

            uint u = (uint)x;
            Assert.Equal((u >= 10 && u <= 20) || (u >= 40 && u <= 50), UnsignedRanges(u));
            Assert.Equal(u >= 0xFFFFFFF0u || u <= 3u || (u >= 20 && u <= 25), UnsignedWrapAround(u));

            if ((x >= 0) && (x <= 255))
            {
                byte b = (byte)x;
                Assert.Equal((b >= 10 && b <= 20) || (b >= 40 && b <= 50) || (b >= 100 && b <= 110), ByteRanges(b));
            }
        }
    }

    [Fact]
    public static void LongRangeTests()
    {
        foreach (long x in InterestingLongs())
        {
            CheckLong(x);
        }
        for (long x = 0x100000000L - 100; x <= 0x100000000L + 100; x++)
        {
            CheckLong(x);
        }

        static void CheckLong(long x)
        {
            Assert.Equal((x >= 10 && x <= 20) || (x >= 40 && x <= 50), LongRanges(x));
            Assert.Equal((x >= 0x100000000L && x <= 0x100000005L) || (x >= 0x100000010L && x <= 0x100000020L), LongRangesFarFromZero(x));
        }
    }

    // The result is used in control flow instead of being materialized as a value.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Branchy(int c)
    {
        if (c < 5)
        {
            return 7;
        }

        if (c >= 10 && c <= 20 || c >= 40 && c <= 50)
        {
            return 1;
        }

        return 2;
    }

    [Fact]
    public static void BranchyControlFlow()
    {
        for (int x = -100; x <= 100; x++)
        {
            int expected = x < 5 ? 7 : ((x >= 10 && x <= 20) || (x >= 40 && x <= 50)) ? 1 : 2;
            Assert.Equal(expected, Branchy(x));
        }
    }

    // The tested value has a side effect that has to be preserved exactly once.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SideEffect(int[] values, ref int index) => values[index++] is >= 10 and <= 20 or >= 40 and <= 50;

    [Fact]
    public static void SideEffectIsPreserved()
    {
        for (int x = -100; x <= 100; x++)
        {
            int[] values = [x];
            int index = 0;
            Assert.Equal((x >= 10 && x <= 20) || (x >= 40 && x <= 50), SideEffect(values, ref index));
            Assert.Equal(1, index);
        }
    }
}
