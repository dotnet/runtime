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
        AssertEqual(span.StartsWith("".ToVar()), span.StartsWith(""));
        AssertEqual(span.StartsWith("a".ToVar()), span.StartsWith("a"));
        AssertEqual(span.StartsWith("z".ToVar()), span.StartsWith("z"));
        AssertEqual(span.StartsWith("A".ToVar()), span.StartsWith("A"));
        AssertEqual(span.StartsWith("Z".ToVar()), span.StartsWith("Z"));
        AssertEqual(span.StartsWith("x".ToVar()), span.StartsWith("x"));
        AssertEqual(span.StartsWith("X".ToVar()), span.StartsWith("X"));
        AssertEqual(span.StartsWith("\r".ToVar()), span.StartsWith("\r"));
        AssertEqual(span.StartsWith("-".ToVar()), span.StartsWith("-"));
        AssertEqual(span.StartsWith("\0".ToVar()), span.StartsWith("\0"));
        AssertEqual(span.StartsWith("ж".ToVar()), span.StartsWith("ж"));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup2(ReadOnlySpan<char> span)
    {
        AssertEqual(span.StartsWith("".ToVar(), StringComparison.CurrentCultureIgnoreCase), span.StartsWith("", StringComparison.CurrentCultureIgnoreCase));
        AssertEqual(span.StartsWith("a".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("a", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("z".ToVar(), StringComparison.Ordinal), span.StartsWith("z", StringComparison.Ordinal));
        AssertEqual(span.StartsWith("A".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("A", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("Z".ToVar(), StringComparison.InvariantCulture), span.StartsWith("Z", StringComparison.InvariantCulture));
        AssertEqual(span.StartsWith("x".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("x", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("X".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("X", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("\r".ToVar(), StringComparison.InvariantCultureIgnoreCase), span.StartsWith("\r", StringComparison.InvariantCultureIgnoreCase));
        AssertEqual(span.StartsWith("-".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("-", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("\0".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("\0", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("ж".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("ж", StringComparison.OrdinalIgnoreCase));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup3(ReadOnlySpan<char> span)
    {
        AssertEqual(span.StartsWith("aa".ToVar()), span.StartsWith("aa"));
        AssertEqual(span.StartsWith("zz".ToVar()), span.StartsWith("zz"));
        AssertEqual(span.StartsWith("AA".ToVar()), span.StartsWith("AA"));
        AssertEqual(span.StartsWith("ZZ".ToVar()), span.StartsWith("ZZ"));
        AssertEqual(span.StartsWith("xx".ToVar()), span.StartsWith("xx"));
        AssertEqual(span.StartsWith("XX".ToVar()), span.StartsWith("XX"));
        AssertEqual(span.StartsWith("\r\r".ToVar()), span.StartsWith("\r\r"));
        AssertEqual(span.StartsWith("--".ToVar()), span.StartsWith("--"));
        AssertEqual(span.StartsWith("\0\0".ToVar()), span.StartsWith("\0\0"));
        AssertEqual(span.StartsWith("жж".ToVar()), span.StartsWith("жж"));
        AssertEqual(span.StartsWith("va".ToVar()), span.StartsWith("va"));
        AssertEqual(span.StartsWith("vz".ToVar()), span.StartsWith("vz"));
        AssertEqual(span.StartsWith("vA".ToVar()), span.StartsWith("vA"));
        AssertEqual(span.StartsWith("vZ".ToVar()), span.StartsWith("vZ"));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup4(ReadOnlySpan<char> span)
    {
        AssertEqual(span.StartsWith("aa".ToVar(), StringComparison.CurrentCultureIgnoreCase), span.StartsWith("aa", StringComparison.CurrentCultureIgnoreCase));
        AssertEqual(span.StartsWith("zz".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("zz", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("AA".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("AA", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("ZZ".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("ZZ", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("xx".ToVar(), StringComparison.InvariantCultureIgnoreCase), span.StartsWith("xx", StringComparison.InvariantCultureIgnoreCase));
        AssertEqual(span.StartsWith("XX".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("XX", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("\r\r".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("\r\r", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("--".ToVar(), StringComparison.Ordinal), span.StartsWith("--", StringComparison.Ordinal));
        AssertEqual(span.StartsWith("\0\0".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("\0\0", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("жж".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("жж", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("va".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("va", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("vz".ToVar(), StringComparison.InvariantCulture), span.StartsWith("vz", StringComparison.InvariantCulture));
        AssertEqual(span.StartsWith("vA".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("vA", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("vZ".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("vZ", StringComparison.OrdinalIgnoreCase));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup5(ReadOnlySpan<char> span)
    {
        AssertEqual(span.StartsWith("vx".ToVar()), span.StartsWith("vx"));
        AssertEqual(span.StartsWith("vX".ToVar()), span.StartsWith("vX"));
        AssertEqual(span.StartsWith("v\r".ToVar()), span.StartsWith("v\r"));
        AssertEqual(span.StartsWith("v-".ToVar()), span.StartsWith("v-"));
        AssertEqual(span.StartsWith("v\0".ToVar()), span.StartsWith("v\0"));
        AssertEqual(span.StartsWith("vж".ToVar()), span.StartsWith("vж"));
        AssertEqual(span.StartsWith("aJ".ToVar()), span.StartsWith("aJ"));
        AssertEqual(span.StartsWith("zJ".ToVar()), span.StartsWith("zJ"));
        AssertEqual(span.StartsWith("AJ".ToVar()), span.StartsWith("AJ"));
        AssertEqual(span.StartsWith("ZJ".ToVar()), span.StartsWith("ZJ"));
        AssertEqual(span.StartsWith("xJ".ToVar()), span.StartsWith("xJ"));
        AssertEqual(span.StartsWith("XJ".ToVar()), span.StartsWith("XJ"));
        AssertEqual(span.StartsWith("\rJ".ToVar()), span.StartsWith("\rJ"));
        AssertEqual(span.StartsWith("-J".ToVar()), span.StartsWith("-J"));
        AssertEqual(span.StartsWith("\0J".ToVar()), span.StartsWith("\0J"));
        AssertEqual(span.StartsWith("жJ".ToVar()), span.StartsWith("жJ"));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup6(ReadOnlySpan<char> span)
    {
        AssertEqual(span.StartsWith("vx".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("vx", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("vX".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("vX", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("v\r".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("v\r", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("v-".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("v-", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("v\0".ToVar(), StringComparison.Ordinal), span.StartsWith("v\0", StringComparison.Ordinal));
        AssertEqual(span.StartsWith("vж".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("vж", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("aJ".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("aJ", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("zJ".ToVar(), StringComparison.InvariantCulture), span.StartsWith("zJ", StringComparison.InvariantCulture));
        AssertEqual(span.StartsWith("AJ".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("AJ", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("ZJ".ToVar(), StringComparison.CurrentCultureIgnoreCase), span.StartsWith("ZJ", StringComparison.CurrentCultureIgnoreCase));
        AssertEqual(span.StartsWith("xJ".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("xJ", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("XJ".ToVar(), StringComparison.InvariantCultureIgnoreCase), span.StartsWith("XJ", StringComparison.InvariantCultureIgnoreCase));
        AssertEqual(span.StartsWith("\rJ".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("\rJ", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("-J".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("-J", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("\0J".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("\0J", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("жJ".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("жJ", StringComparison.OrdinalIgnoreCase));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup7(ReadOnlySpan<char> span)
    {
        AssertEqual(span.StartsWith("aaa".ToVar()), span.StartsWith("aaa"));
        AssertEqual(span.StartsWith("zzz".ToVar()), span.StartsWith("zzz"));
        AssertEqual(span.StartsWith("AAA".ToVar()), span.StartsWith("AAA"));
        AssertEqual(span.StartsWith("ZZZ".ToVar()), span.StartsWith("ZZZ"));
        AssertEqual(span.StartsWith("xxx".ToVar()), span.StartsWith("xxx"));
        AssertEqual(span.StartsWith("XXX".ToVar()), span.StartsWith("XXX"));
        AssertEqual(span.StartsWith("\r\r\r".ToVar()), span.StartsWith("\r\r\r"));
        AssertEqual(span.StartsWith("---".ToVar()), span.StartsWith("---"));
        AssertEqual(span.StartsWith("\0\0\0".ToVar()), span.StartsWith("\0\0\0"));
        AssertEqual(span.StartsWith("жжж".ToVar()), span.StartsWith("жжж"));
        AssertEqual(span.StartsWith("ava".ToVar()), span.StartsWith("ava"));
        AssertEqual(span.StartsWith("zvz".ToVar()), span.StartsWith("zvz"));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup8(ReadOnlySpan<char> span)
    {
        AssertEqual(span.StartsWith("aaa".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("aaa", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("zzz".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("zzz", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("AAA".ToVar(), StringComparison.InvariantCultureIgnoreCase), span.StartsWith("AAA", StringComparison.InvariantCultureIgnoreCase));
        AssertEqual(span.StartsWith("ZZZ".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("ZZZ", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("xxx".ToVar(), StringComparison.InvariantCulture), span.StartsWith("xxx", StringComparison.InvariantCulture));
        AssertEqual(span.StartsWith("XXX".ToVar(), StringComparison.CurrentCultureIgnoreCase), span.StartsWith("XXX", StringComparison.CurrentCultureIgnoreCase));
        AssertEqual(span.StartsWith("\r\r\r".ToVar(), StringComparison.Ordinal), span.StartsWith("\r\r\r", StringComparison.Ordinal));
        AssertEqual(span.StartsWith("---".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("---", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("\0\0\0".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("\0\0\0", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("жжж".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("жжж", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("ava".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("ava", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("zvz".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("zvz", StringComparison.OrdinalIgnoreCase));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup9(ReadOnlySpan<char> span)
    {
        AssertEqual(span.StartsWith("AvA".ToVar()), span.StartsWith("AvA"));
        AssertEqual(span.StartsWith("ZvZ".ToVar()), span.StartsWith("ZvZ"));
        AssertEqual(span.StartsWith("xvx".ToVar()), span.StartsWith("xvx"));
        AssertEqual(span.StartsWith("XvX".ToVar()), span.StartsWith("XvX"));
        AssertEqual(span.StartsWith("\rv\r".ToVar()), span.StartsWith("\rv\r"));
        AssertEqual(span.StartsWith("-v-".ToVar()), span.StartsWith("-v-"));
        AssertEqual(span.StartsWith("\0v\0".ToVar()), span.StartsWith("\0v\0"));
        AssertEqual(span.StartsWith("жvж".ToVar()), span.StartsWith("жvж"));
        AssertEqual(span.StartsWith("aaж".ToVar()), span.StartsWith("aaж"));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup10(ReadOnlySpan<char> span)
    {
        AssertEqual(span.StartsWith("AvA".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("AvA", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("ZvZ".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("ZvZ", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("xvx".ToVar(), StringComparison.CurrentCultureIgnoreCase), span.StartsWith("xvx", StringComparison.CurrentCultureIgnoreCase));
        AssertEqual(span.StartsWith("XvX".ToVar(), StringComparison.InvariantCultureIgnoreCase), span.StartsWith("XvX", StringComparison.InvariantCultureIgnoreCase));
        AssertEqual(span.StartsWith("\rv\r".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("\rv\r", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("-v-".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("-v-", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("\0v\0".ToVar(), StringComparison.InvariantCulture), span.StartsWith("\0v\0", StringComparison.InvariantCulture));
        AssertEqual(span.StartsWith("жvж".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("жvж", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("aaж".ToVar(), StringComparison.Ordinal), span.StartsWith("aaж", StringComparison.Ordinal));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup11(ReadOnlySpan<char> span)
    {
        AssertEqual(span.StartsWith("aaaa".ToVar()), span.StartsWith("aaaa"));
        AssertEqual(span.StartsWith("zzzz".ToVar()), span.StartsWith("zzzz"));
        AssertEqual(span.StartsWith("AAAA".ToVar()), span.StartsWith("AAAA"));
        AssertEqual(span.StartsWith("ZZZZ".ToVar()), span.StartsWith("ZZZZ"));
        AssertEqual(span.StartsWith("xxxx".ToVar()), span.StartsWith("xxxx"));
        AssertEqual(span.StartsWith("XXXX".ToVar()), span.StartsWith("XXXX"));
        AssertEqual(span.StartsWith("\r\r\r\r".ToVar()), span.StartsWith("\r\r\r\r"));
        AssertEqual(span.StartsWith("----".ToVar()), span.StartsWith("----"));
        AssertEqual(span.StartsWith("\0\0\0\0".ToVar()), span.StartsWith("\0\0\0\0"));
        AssertEqual(span.StartsWith("жжжж".ToVar()), span.StartsWith("жжжж"));
        AssertEqual(span.StartsWith("aaaa".ToVar()), span.StartsWith("aaaa"));
        AssertEqual(span.StartsWith("zzzz".ToVar()), span.StartsWith("zzzz"));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup12(ReadOnlySpan<char> span)
    {
        AssertEqual(span.StartsWith("aaaa".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("aaaa", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("zzzz".ToVar(), StringComparison.CurrentCultureIgnoreCase), span.StartsWith("zzzz", StringComparison.CurrentCultureIgnoreCase));
        AssertEqual(span.StartsWith("AAAA".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("AAAA", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("ZZZZ".ToVar(), StringComparison.Ordinal), span.StartsWith("ZZZZ", StringComparison.Ordinal));
        AssertEqual(span.StartsWith("xxxx".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("xxxx", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("XXXX".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("XXXX", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("\r\r\r\r".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("\r\r\r\r", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("----".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("----", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("\0\0\0\0".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("\0\0\0\0", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("жжжж".ToVar(), StringComparison.InvariantCulture), span.StartsWith("жжжж", StringComparison.InvariantCulture));
        AssertEqual(span.StartsWith("aaaa".ToVar(), StringComparison.InvariantCultureIgnoreCase), span.StartsWith("aaaa", StringComparison.InvariantCultureIgnoreCase));
        AssertEqual(span.StartsWith("zzzz".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("zzzz", StringComparison.OrdinalIgnoreCase));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup13(ReadOnlySpan<char> span)
    {
        AssertEqual(span.StartsWith("AAdd".ToVar()), span.StartsWith("AAdd"));
        AssertEqual(span.StartsWith("ZZdd".ToVar()), span.StartsWith("ZZdd"));
        AssertEqual(span.StartsWith("xxdd".ToVar()), span.StartsWith("xxdd"));
        AssertEqual(span.StartsWith("XXdd".ToVar()), span.StartsWith("XXdd"));
        AssertEqual(span.StartsWith("dd\r\r".ToVar()), span.StartsWith("dd\r\r"));
        AssertEqual(span.StartsWith("--xx".ToVar()), span.StartsWith("--xx"));
        AssertEqual(span.StartsWith("\0\0bb".ToVar()), span.StartsWith("\0\0bb"));
        AssertEqual(span.StartsWith("aaaж".ToVar()), span.StartsWith("aaaж"));
        AssertEqual(span.StartsWith("abcd".ToVar()), span.StartsWith("abcd"));
        AssertEqual(span.StartsWith("zZzв".ToVar()), span.StartsWith("zZzв"));
        AssertEqual(span.StartsWith("ABCD".ToVar()), span.StartsWith("ABCD"));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void TestGroup14(ReadOnlySpan<char> span)
    {
        AssertEqual(span.StartsWith("AAdd".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("AAdd", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("ZZdd".ToVar(), StringComparison.Ordinal), span.StartsWith("ZZdd", StringComparison.Ordinal));
        AssertEqual(span.StartsWith("xxdd".ToVar(), StringComparison.InvariantCulture), span.StartsWith("xxdd", StringComparison.InvariantCulture));
        AssertEqual(span.StartsWith("XXdd".ToVar(), StringComparison.CurrentCultureIgnoreCase), span.StartsWith("XXdd", StringComparison.CurrentCultureIgnoreCase));
        AssertEqual(span.StartsWith("dd\r\r".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("dd\r\r", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("--xx".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("--xx", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("\0\0bb".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("\0\0bb", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("aaaж".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("aaaж", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("abcd".ToVar(), StringComparison.InvariantCultureIgnoreCase), span.StartsWith("abcd", StringComparison.InvariantCultureIgnoreCase));
        AssertEqual(span.StartsWith("zZzв".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("zZzв", StringComparison.OrdinalIgnoreCase));
        AssertEqual(span.StartsWith("ABCD".ToVar(), StringComparison.OrdinalIgnoreCase), span.StartsWith("ABCD", StringComparison.OrdinalIgnoreCase));
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
