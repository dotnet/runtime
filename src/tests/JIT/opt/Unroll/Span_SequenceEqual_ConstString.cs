// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public static class Program
{
    private static int s_ReturnCode = 100;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup1(ReadOnlySpan<char> span)
    {
        AssertEqual(span.SequenceEqual("".ToVar()), span.SequenceEqual(""));
        AssertEqual(span.SequenceEqual("a".ToVar()), span.SequenceEqual("a"));
        AssertEqual(span.SequenceEqual("z".ToVar()), span.SequenceEqual("z"));
        AssertEqual(span.SequenceEqual("A".ToVar()), span.SequenceEqual("A"));
        AssertEqual(span.SequenceEqual("Z".ToVar()), span.SequenceEqual("Z"));
        AssertEqual(span.SequenceEqual("x".ToVar()), span.SequenceEqual("x"));
        AssertEqual(span.SequenceEqual("X".ToVar()), span.SequenceEqual("X"));
        AssertEqual(span.SequenceEqual("\r".ToVar()), span.SequenceEqual("\r"));
        AssertEqual(span.SequenceEqual("-".ToVar()), span.SequenceEqual("-"));
        AssertEqual(span.SequenceEqual("\0".ToVar()), span.SequenceEqual("\0"));
        AssertEqual(span.SequenceEqual("ж".ToVar()), span.SequenceEqual("ж"));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup2(ReadOnlySpan<char> span)
    {
        AssertEqual(span.Equals("".ToVar(), StringComparison.CurrentCultureIgnoreCase), span.Equals("", StringComparison.CurrentCultureIgnoreCase));
        AssertEqual(span.Equals("a".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("a", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("z".ToVar(), StringComparison.Ordinal), span.Equals("z", StringComparison.Ordinal));
        AssertEqual(span.Equals("A".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("A", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("Z".ToVar(), StringComparison.InvariantCulture), span.Equals("Z", StringComparison.InvariantCulture));
        AssertEqual(span.Equals("x".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("x", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("X".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("X", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("\r".ToVar(), StringComparison.InvariantCultureIgnoreCase), span.Equals("\r", StringComparison.InvariantCultureIgnoreCase));
        AssertEqual(span.Equals("-".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("-", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("\0".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("\0", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("ж".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("ж", StringComparison.OrdinalIgnoreCase));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup3(ReadOnlySpan<char> span)
    {
        AssertEqual(span.SequenceEqual("aa".ToVar()), span.SequenceEqual("aa"));
        AssertEqual(span.SequenceEqual("zz".ToVar()), span.SequenceEqual("zz"));
        AssertEqual(span.SequenceEqual("AA".ToVar()), span.SequenceEqual("AA"));
        AssertEqual(span.SequenceEqual("ZZ".ToVar()), span.SequenceEqual("ZZ"));
        AssertEqual(span.SequenceEqual("xx".ToVar()), span.SequenceEqual("xx"));
        AssertEqual(span.SequenceEqual("XX".ToVar()), span.SequenceEqual("XX"));
        AssertEqual(span.SequenceEqual("\r\r".ToVar()), span.SequenceEqual("\r\r"));
        AssertEqual(span.SequenceEqual("--".ToVar()), span.SequenceEqual("--"));
        AssertEqual(span.SequenceEqual("\0\0".ToVar()), span.SequenceEqual("\0\0"));
        AssertEqual(span.SequenceEqual("жж".ToVar()), span.SequenceEqual("жж"));
        AssertEqual(span.SequenceEqual("va".ToVar()), span.SequenceEqual("va"));
        AssertEqual(span.SequenceEqual("vz".ToVar()), span.SequenceEqual("vz"));
        AssertEqual(span.SequenceEqual("vA".ToVar()), span.SequenceEqual("vA"));
        AssertEqual(span.SequenceEqual("vZ".ToVar()), span.SequenceEqual("vZ"));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup4(ReadOnlySpan<char> span)
    {
        AssertEqual(span.Equals("aa".ToVar(), StringComparison.CurrentCultureIgnoreCase), span.Equals("aa", StringComparison.CurrentCultureIgnoreCase));
        AssertEqual(span.Equals("zz".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("zz", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("AA".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("AA", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("ZZ".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("ZZ", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("xx".ToVar(), StringComparison.InvariantCultureIgnoreCase), span.Equals("xx", StringComparison.InvariantCultureIgnoreCase));
        AssertEqual(span.Equals("XX".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("XX", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("\r\r".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("\r\r", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("--".ToVar(), StringComparison.Ordinal), span.Equals("--", StringComparison.Ordinal));
        AssertEqual(span.Equals("\0\0".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("\0\0", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("жж".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("жж", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("va".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("va", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("vz".ToVar(), StringComparison.InvariantCulture), span.Equals("vz", StringComparison.InvariantCulture));
        AssertEqual(span.Equals("vA".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("vA", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("vZ".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("vZ", StringComparison.OrdinalIgnoreCase));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup5(ReadOnlySpan<char> span)
    {
        AssertEqual(span.SequenceEqual("vx".ToVar()), span.SequenceEqual("vx"));
        AssertEqual(span.SequenceEqual("vX".ToVar()), span.SequenceEqual("vX"));
        AssertEqual(span.SequenceEqual("v\r".ToVar()), span.SequenceEqual("v\r"));
        AssertEqual(span.SequenceEqual("v-".ToVar()), span.SequenceEqual("v-"));
        AssertEqual(span.SequenceEqual("v\0".ToVar()), span.SequenceEqual("v\0"));
        AssertEqual(span.SequenceEqual("vж".ToVar()), span.SequenceEqual("vж"));
        AssertEqual(span.SequenceEqual("aJ".ToVar()), span.SequenceEqual("aJ"));
        AssertEqual(span.SequenceEqual("zJ".ToVar()), span.SequenceEqual("zJ"));
        AssertEqual(span.SequenceEqual("AJ".ToVar()), span.SequenceEqual("AJ"));
        AssertEqual(span.SequenceEqual("ZJ".ToVar()), span.SequenceEqual("ZJ"));
        AssertEqual(span.SequenceEqual("xJ".ToVar()), span.SequenceEqual("xJ"));
        AssertEqual(span.SequenceEqual("XJ".ToVar()), span.SequenceEqual("XJ"));
        AssertEqual(span.SequenceEqual("\rJ".ToVar()), span.SequenceEqual("\rJ"));
        AssertEqual(span.SequenceEqual("-J".ToVar()), span.SequenceEqual("-J"));
        AssertEqual(span.SequenceEqual("\0J".ToVar()), span.SequenceEqual("\0J"));
        AssertEqual(span.SequenceEqual("жJ".ToVar()), span.SequenceEqual("жJ"));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup6(ReadOnlySpan<char> span)
    {
        AssertEqual(span.Equals("vx".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("vx", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("vX".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("vX", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("v\r".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("v\r", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("v-".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("v-", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("v\0".ToVar(), StringComparison.Ordinal), span.Equals("v\0", StringComparison.Ordinal));
        AssertEqual(span.Equals("vж".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("vж", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("aJ".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("aJ", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("zJ".ToVar(), StringComparison.InvariantCulture), span.Equals("zJ", StringComparison.InvariantCulture));
        AssertEqual(span.Equals("AJ".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("AJ", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("ZJ".ToVar(), StringComparison.CurrentCultureIgnoreCase), span.Equals("ZJ", StringComparison.CurrentCultureIgnoreCase));
        AssertEqual(span.Equals("xJ".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("xJ", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("XJ".ToVar(), StringComparison.InvariantCultureIgnoreCase), span.Equals("XJ", StringComparison.InvariantCultureIgnoreCase));
        AssertEqual(span.Equals("\rJ".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("\rJ", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("-J".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("-J", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("\0J".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("\0J", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("жJ".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("жJ", StringComparison.OrdinalIgnoreCase));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup7(ReadOnlySpan<char> span)
    {
        AssertEqual(span.SequenceEqual("aaa".ToVar()), span.SequenceEqual("aaa"));
        AssertEqual(span.SequenceEqual("zzz".ToVar()), span.SequenceEqual("zzz"));
        AssertEqual(span.SequenceEqual("AAA".ToVar()), span.SequenceEqual("AAA"));
        AssertEqual(span.SequenceEqual("ZZZ".ToVar()), span.SequenceEqual("ZZZ"));
        AssertEqual(span.SequenceEqual("xxx".ToVar()), span.SequenceEqual("xxx"));
        AssertEqual(span.SequenceEqual("XXX".ToVar()), span.SequenceEqual("XXX"));
        AssertEqual(span.SequenceEqual("\r\r\r".ToVar()), span.SequenceEqual("\r\r\r"));
        AssertEqual(span.SequenceEqual("---".ToVar()), span.SequenceEqual("---"));
        AssertEqual(span.SequenceEqual("\0\0\0".ToVar()), span.SequenceEqual("\0\0\0"));
        AssertEqual(span.SequenceEqual("жжж".ToVar()), span.SequenceEqual("жжж"));
        AssertEqual(span.SequenceEqual("ava".ToVar()), span.SequenceEqual("ava"));
        AssertEqual(span.SequenceEqual("zvz".ToVar()), span.SequenceEqual("zvz"));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup8(ReadOnlySpan<char> span)
    {
        AssertEqual(span.Equals("aaa".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("aaa", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("zzz".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("zzz", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("AAA".ToVar(), StringComparison.InvariantCultureIgnoreCase), span.Equals("AAA", StringComparison.InvariantCultureIgnoreCase));
        AssertEqual(span.Equals("ZZZ".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("ZZZ", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("xxx".ToVar(), StringComparison.InvariantCulture), span.Equals("xxx", StringComparison.InvariantCulture));
        AssertEqual(span.Equals("XXX".ToVar(), StringComparison.CurrentCultureIgnoreCase), span.Equals("XXX", StringComparison.CurrentCultureIgnoreCase));
        AssertEqual(span.Equals("\r\r\r".ToVar(), StringComparison.Ordinal), span.Equals("\r\r\r", StringComparison.Ordinal));
        AssertEqual(span.Equals("---".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("---", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("\0\0\0".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("\0\0\0", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("жжж".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("жжж", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("ava".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("ava", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("zvz".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("zvz", StringComparison.OrdinalIgnoreCase));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup9(ReadOnlySpan<char> span)
    {
        AssertEqual(span.SequenceEqual("AvA".ToVar()), span.SequenceEqual("AvA"));
        AssertEqual(span.SequenceEqual("ZvZ".ToVar()), span.SequenceEqual("ZvZ"));
        AssertEqual(span.SequenceEqual("xvx".ToVar()), span.SequenceEqual("xvx"));
        AssertEqual(span.SequenceEqual("XvX".ToVar()), span.SequenceEqual("XvX"));
        AssertEqual(span.SequenceEqual("\rv\r".ToVar()), span.SequenceEqual("\rv\r"));
        AssertEqual(span.SequenceEqual("-v-".ToVar()), span.SequenceEqual("-v-"));
        AssertEqual(span.SequenceEqual("\0v\0".ToVar()), span.SequenceEqual("\0v\0"));
        AssertEqual(span.SequenceEqual("жvж".ToVar()), span.SequenceEqual("жvж"));
        AssertEqual(span.SequenceEqual("aaж".ToVar()), span.SequenceEqual("aaж"));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup10(ReadOnlySpan<char> span)
    {
        AssertEqual(span.Equals("AvA".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("AvA", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("ZvZ".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("ZvZ", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("xvx".ToVar(), StringComparison.CurrentCultureIgnoreCase), span.Equals("xvx", StringComparison.CurrentCultureIgnoreCase));
        AssertEqual(span.Equals("XvX".ToVar(), StringComparison.InvariantCultureIgnoreCase), span.Equals("XvX", StringComparison.InvariantCultureIgnoreCase));
        AssertEqual(span.Equals("\rv\r".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("\rv\r", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("-v-".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("-v-", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("\0v\0".ToVar(), StringComparison.InvariantCulture), span.Equals("\0v\0", StringComparison.InvariantCulture));
        AssertEqual(span.Equals("жvж".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("жvж", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("aaж".ToVar(), StringComparison.Ordinal), span.Equals("aaж", StringComparison.Ordinal));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup11(ReadOnlySpan<char> span)
    {
        AssertEqual(span.SequenceEqual("aaaa".ToVar()), span.SequenceEqual("aaaa"));
        AssertEqual(span.SequenceEqual("zzzz".ToVar()), span.SequenceEqual("zzzz"));
        AssertEqual(span.SequenceEqual("AAAA".ToVar()), span.SequenceEqual("AAAA"));
        AssertEqual(span.SequenceEqual("ZZZZ".ToVar()), span.SequenceEqual("ZZZZ"));
        AssertEqual(span.SequenceEqual("xxxx".ToVar()), span.SequenceEqual("xxxx"));
        AssertEqual(span.SequenceEqual("XXXX".ToVar()), span.SequenceEqual("XXXX"));
        AssertEqual(span.SequenceEqual("\r\r\r\r".ToVar()), span.SequenceEqual("\r\r\r\r"));
        AssertEqual(span.SequenceEqual("----".ToVar()), span.SequenceEqual("----"));
        AssertEqual(span.SequenceEqual("\0\0\0\0".ToVar()), span.SequenceEqual("\0\0\0\0"));
        AssertEqual(span.SequenceEqual("жжжж".ToVar()), span.SequenceEqual("жжжж"));
        AssertEqual(span.SequenceEqual("aaaa".ToVar()), span.SequenceEqual("aaaa"));
        AssertEqual(span.SequenceEqual("zzzz".ToVar()), span.SequenceEqual("zzzz"));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup12(ReadOnlySpan<char> span)
    {
        AssertEqual(span.Equals("aaaa".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("aaaa", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("zzzz".ToVar(), StringComparison.CurrentCultureIgnoreCase), span.Equals("zzzz", StringComparison.CurrentCultureIgnoreCase));
        AssertEqual(span.Equals("AAAA".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("AAAA", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("ZZZZ".ToVar(), StringComparison.Ordinal), span.Equals("ZZZZ", StringComparison.Ordinal));
        AssertEqual(span.Equals("xxxx".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("xxxx", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("XXXX".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("XXXX", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("\r\r\r\r".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("\r\r\r\r", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("----".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("----", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("\0\0\0\0".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("\0\0\0\0", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("жжжж".ToVar(), StringComparison.InvariantCulture), span.Equals("жжжж", StringComparison.InvariantCulture));
        AssertEqual(span.Equals("aaaa".ToVar(), StringComparison.InvariantCultureIgnoreCase), span.Equals("aaaa", StringComparison.InvariantCultureIgnoreCase));
        AssertEqual(span.Equals("zzzz".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("zzzz", StringComparison.OrdinalIgnoreCase));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup13(ReadOnlySpan<char> span)
    {
        AssertEqual(span.SequenceEqual("AAdd".ToVar()), span.SequenceEqual("AAdd"));
        AssertEqual(span.SequenceEqual("ZZdd".ToVar()), span.SequenceEqual("ZZdd"));
        AssertEqual(span.SequenceEqual("xxdd".ToVar()), span.SequenceEqual("xxdd"));
        AssertEqual(span.SequenceEqual("XXdd".ToVar()), span.SequenceEqual("XXdd"));
        AssertEqual(span.SequenceEqual("dd\r\r".ToVar()), span.SequenceEqual("dd\r\r"));
        AssertEqual(span.SequenceEqual("--xx".ToVar()), span.SequenceEqual("--xx"));
        AssertEqual(span.SequenceEqual("\0\0bb".ToVar()), span.SequenceEqual("\0\0bb"));
        AssertEqual(span.SequenceEqual("aaaж".ToVar()), span.SequenceEqual("aaaж"));
        AssertEqual(span.SequenceEqual("abcd".ToVar()), span.SequenceEqual("abcd"));
        AssertEqual(span.SequenceEqual("zZzв".ToVar()), span.SequenceEqual("zZzв"));
        AssertEqual(span.SequenceEqual("ABCD".ToVar()), span.SequenceEqual("ABCD"));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup14(ReadOnlySpan<char> span)
    {
        AssertEqual(span.Equals("AAdd".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("AAdd", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("ZZdd".ToVar(), StringComparison.Ordinal), span.Equals("ZZdd", StringComparison.Ordinal));
        AssertEqual(span.Equals("xxdd".ToVar(), StringComparison.InvariantCulture), span.Equals("xxdd", StringComparison.InvariantCulture));
        AssertEqual(span.Equals("XXdd".ToVar(), StringComparison.CurrentCultureIgnoreCase), span.Equals("XXdd", StringComparison.CurrentCultureIgnoreCase));
        AssertEqual(span.Equals("dd\r\r".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("dd\r\r", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("--xx".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("--xx", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("\0\0bb".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("\0\0bb", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("aaaж".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("aaaж", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("abcd".ToVar(), StringComparison.InvariantCultureIgnoreCase), span.Equals("abcd", StringComparison.InvariantCultureIgnoreCase));
        AssertEqual(span.Equals("zZzв".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("zZzв", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.Equals("ABCD".ToVar(), StringComparison.OrdinalIgnoreCase), span.Equals("ABCD", StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<char[]> Powerset(char[] source)
    {
        return Enumerable
            .Range(0, 1 << (source.Length))
            .Select(index => source.Where((c, i) => (index & (1 << i)) != 0).ToArray());
    }

    public static int Main(string[] args)
    {
        var powerset = Powerset(new char[]
            {
                'a', 'A', 'z', 'Z', '\r', '-', 
                'ж', 'Ы', 'c', 'd', 'v', (char)0x20,
                (char)0x9500, (char)0x0095, (char)0x9595
            });

        foreach (var item in powerset)
        {
            ReadOnlySpan<char> span = item.AsSpan();
            TestGroup1(span);
            TestGroup2(span);
            TestGroup3(span);
            TestGroup4(span);
            TestGroup5(span);
            TestGroup6(span);
            TestGroup7(span);
            TestGroup8(span);
            TestGroup9(span);
            TestGroup10(span);
            TestGroup11(span);
            TestGroup12(span);
            TestGroup13(span);
            TestGroup14(span);
        }
        return s_ReturnCode;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string ToVar(this string str) => str;

    private static void AssertEqual(bool expected, bool actual, [CallerLineNumber] int line = 0)
    {
        if (expected != actual)
        {
            Console.WriteLine($"ERROR: {expected} != {actual} L{line}");
            s_ReturnCode++;
        }
    }
}
