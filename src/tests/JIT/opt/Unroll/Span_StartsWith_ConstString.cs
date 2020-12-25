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
    private static void TestGroup3(ReadOnlySpan<char> span)
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
    private static void TestGroup4(ReadOnlySpan<char> span)
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
    private static void TestGroup5(ReadOnlySpan<char> span)
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
    private static void TestGroup6(ReadOnlySpan<char> span)
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
    private static void TestGroup7(ReadOnlySpan<char> span)
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
