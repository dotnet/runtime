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
    private static void TestGroup3(ReadOnlySpan<char> span)
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
    private static void TestGroup4(ReadOnlySpan<char> span)
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
    private static void TestGroup5(ReadOnlySpan<char> span)
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
    private static void TestGroup6(ReadOnlySpan<char> span)
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
    private static void TestGroup7(ReadOnlySpan<char> span)
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

    public static IEnumerable<char[]> Permutations(char[] source)
    {
        return Enumerable.Range(0, 1 << (source.Length)).Select(index => source.Where((v, i) => (index & (1 << i)) != 0).ToArray());
    }

    public static int Main(string[] args)
    {
        var allPermutations = Permutations(new char[]
        {
            'a', 'A', 'z', 'Z', 'x', '\r', '\n',
            '-', 'ж', 'Ы', 'c', 'd', 'e', 'v',
            (char)0x9000, (char)0x0090, (char)0x9090,
            (char)0x2000, (char)0x0020, (char)0x2020
        });

        foreach (var item in allPermutations)
        {
            ReadOnlySpan<char> span = item.AsSpan();
            TestGroup1(span);
            TestGroup2(span);
            TestGroup3(span);
            TestGroup4(span);
            TestGroup5(span);
            TestGroup6(span);
            TestGroup7(span);
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
