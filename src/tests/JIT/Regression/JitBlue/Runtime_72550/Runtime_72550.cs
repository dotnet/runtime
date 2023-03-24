// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class Runtime_72550
{
    private static int retCode = 100;

    [Fact]
    public static int TestEntryPoint()
    {
        IsTrue(Test1.StartsWith1(""));
        IsTrue(Test1.StartsWith2(""));
        IsTrue(Test1.StartsWith3(""));
        IsTrue(Test1.StartsWith4(""));
        IsTrue(Test1.StartsWith5(""));

        IsTrue(!Test1.StartsWith1("swar"));
        IsTrue(!Test1.StartsWith2("swar"));
        IsTrue(!Test1.StartsWith3("swar"));
        IsTrue(!Test1.StartsWith4("swar"));
        IsTrue(!Test1.StartsWith5("swar"));

        IsTrue(!Test1.StartsWith1("simd"));
        IsTrue(!Test1.StartsWith2("simd"));
        IsTrue(!Test1.StartsWith3("simd"));
        IsTrue(!Test1.StartsWith4("simd"));
        IsTrue(!Test1.StartsWith5("simd"));

        IsTrue(!Test1.StartsWith1("swar\0"));
        IsTrue(!Test1.StartsWith2("swar\0"));
        IsTrue(!Test1.StartsWith3("swar\0"));
        IsTrue(!Test1.StartsWith4("swar\0"));
        IsTrue(!Test1.StartsWith5("swar\0"));

        IsTrue(!Test1.StartsWith1(" swar"));
        IsTrue(!Test1.StartsWith2(" swar"));
        IsTrue(!Test1.StartsWith3(" swar"));
        IsTrue(!Test1.StartsWith4(" swar"));
        IsTrue(!Test1.StartsWith5(" swar"));

        IsTrue(!Test1.StartsWith1("swarswar"));
        IsTrue(!Test1.StartsWith2("swarswar"));
        IsTrue(!Test1.StartsWith3("swarswar"));
        IsTrue(!Test1.StartsWith4("swarswar"));
        IsTrue(!Test1.StartsWith5("swarswar"));

        IsTrue(!Test1.StartsWith1("simd"));
        IsTrue(!Test1.StartsWith2("simd"));
        IsTrue(!Test1.StartsWith3("simd"));
        IsTrue(!Test1.StartsWith4("simd"));
        IsTrue(!Test1.StartsWith5("simd"));

        IsTrue(!Test1.StartsWith1("simx"));
        IsTrue(!Test1.StartsWith2("simx"));
        IsTrue(!Test1.StartsWith3("simx"));
        IsTrue(!Test1.StartsWith4("simx"));
        IsTrue(!Test1.StartsWith5("simx"));

        IsTrue(!Test1.StartsWith1("simdsimdsimd"));
        IsTrue(!Test1.StartsWith2("simdsimdsimd"));
        IsTrue(!Test1.StartsWith3("simdsimdsimd"));
        IsTrue(!Test1.StartsWith4("simdsimdsimd"));
        IsTrue(!Test1.StartsWith5("simdsimdsimd"));

        IsTrue(!Test1.StartsWith1("simdsimdsimdsimdsimdsimd"));
        IsTrue(!Test1.StartsWith2("simdsimdsimdsimdsimdsimd"));
        IsTrue(!Test1.StartsWith3("simdsimdsimdsimdsimdsimd"));
        IsTrue(!Test1.StartsWith4("simdsimdsimdsimdsimdsimd"));
        IsTrue(!Test1.StartsWith5("simdsimdsimdsimdsimdsimd"));


        IsTrue(Test2.StartsWith1(""));
        IsTrue(Test2.StartsWith2(""));
        IsTrue(Test2.StartsWith3(""));
        IsTrue(Test2.StartsWith4(""));
        IsTrue(Test2.StartsWith5(""));

        IsTrue(Test2.StartsWith1("swar"));
        IsTrue(Test2.StartsWith2("swar"));
        IsTrue(Test2.StartsWith3("swar"));
        IsTrue(Test2.StartsWith4("swar"));
        IsTrue(Test2.StartsWith5("swar"));

        IsTrue(Test2.StartsWith1("swa"));
        IsTrue(Test2.StartsWith2("swa"));
        IsTrue(Test2.StartsWith3("swa"));
        IsTrue(Test2.StartsWith4("swa"));
        IsTrue(Test2.StartsWith5("swa"));

        IsTrue(Test2.StartsWith1("s"));
        IsTrue(Test2.StartsWith2("s"));
        IsTrue(Test2.StartsWith3("s"));
        IsTrue(Test2.StartsWith4("s"));
        IsTrue(Test2.StartsWith5("s"));

        IsTrue(!Test2.StartsWith1("s\0"));
        IsTrue(!Test2.StartsWith2("s\0"));
        IsTrue(!Test2.StartsWith3("s\0"));
        IsTrue(!Test2.StartsWith4("s\0"));
        IsTrue(!Test2.StartsWith5("s\0"));

        IsTrue(!Test2.StartsWith1("simd"));
        IsTrue(!Test2.StartsWith2("simd"));
        IsTrue(!Test2.StartsWith3("simd"));
        IsTrue(!Test2.StartsWith4("simd"));
        IsTrue(!Test2.StartsWith5("simd"));

        IsTrue(!Test2.StartsWith1("swar\0"));
        IsTrue(!Test2.StartsWith2("swar\0"));
        IsTrue(!Test2.StartsWith3("swar\0"));
        IsTrue(!Test2.StartsWith4("swar\0"));
        IsTrue(!Test2.StartsWith5("swar\0"));

        IsTrue(!Test2.StartsWith1(" swar"));
        IsTrue(!Test2.StartsWith2(" swar"));
        IsTrue(!Test2.StartsWith3(" swar"));
        IsTrue(!Test2.StartsWith4(" swar"));
        IsTrue(!Test2.StartsWith5(" swar"));

        IsTrue(!Test2.StartsWith1("swarswar"));
        IsTrue(!Test2.StartsWith2("swarswar"));
        IsTrue(!Test2.StartsWith3("swarswar"));
        IsTrue(!Test2.StartsWith4("swarswar"));
        IsTrue(!Test2.StartsWith5("swarswar"));

        IsTrue(!Test2.StartsWith1("simd"));
        IsTrue(!Test2.StartsWith2("simd"));
        IsTrue(!Test2.StartsWith3("simd"));
        IsTrue(!Test2.StartsWith4("simd"));
        IsTrue(!Test2.StartsWith5("simd"));

        IsTrue(!Test2.StartsWith1("simx"));
        IsTrue(!Test2.StartsWith2("simx"));
        IsTrue(!Test2.StartsWith3("simx"));
        IsTrue(!Test2.StartsWith4("simx"));
        IsTrue(!Test2.StartsWith5("simx"));

        IsTrue(!Test2.StartsWith1("simdsimdsimd"));
        IsTrue(!Test2.StartsWith2("simdsimdsimd"));
        IsTrue(!Test2.StartsWith3("simdsimdsimd"));
        IsTrue(!Test2.StartsWith4("simdsimdsimd"));
        IsTrue(!Test2.StartsWith5("simdsimdsimd"));

        IsTrue(!Test2.StartsWith1("simdsimdsimdsimdsimdsimd"));
        IsTrue(!Test2.StartsWith2("simdsimdsimdsimdsimdsimd"));
        IsTrue(!Test2.StartsWith3("simdsimdsimdsimdsimdsimd"));
        IsTrue(!Test2.StartsWith4("simdsimdsimdsimdsimdsimd"));
        IsTrue(!Test2.StartsWith5("simdsimdsimdsimdsimdsimd"));


        IsTrue(Test3.StartsWith1(""));
        IsTrue(Test3.StartsWith2(""));
        IsTrue(Test3.StartsWith3(""));
        IsTrue(Test3.StartsWith4(""));
        IsTrue(Test3.StartsWith5(""));

        IsTrue(!Test3.StartsWith1("swar"));
        IsTrue(!Test3.StartsWith2("swar"));
        IsTrue(!Test3.StartsWith3("swar"));
        IsTrue(!Test3.StartsWith4("swar"));
        IsTrue(!Test3.StartsWith5("swar"));

        IsTrue(Test3.StartsWith1("simd"));
        IsTrue(Test3.StartsWith2("simd"));
        IsTrue(Test3.StartsWith3("simd"));
        IsTrue(Test3.StartsWith4("simd"));
        IsTrue(Test3.StartsWith5("simd"));
        
        IsTrue(!Test3.StartsWith1("swar\0"));
        IsTrue(!Test3.StartsWith2("swar\0"));
        IsTrue(!Test3.StartsWith3("swar\0"));
        IsTrue(!Test3.StartsWith4("swar\0"));
        IsTrue(!Test3.StartsWith5("swar\0"));

        IsTrue(!Test3.StartsWith1(" swar"));
        IsTrue(!Test3.StartsWith2(" swar"));
        IsTrue(!Test3.StartsWith3(" swar"));
        IsTrue(!Test3.StartsWith4(" swar"));
        IsTrue(!Test3.StartsWith5(" swar"));

        IsTrue(!Test3.StartsWith1("swarswar"));
        IsTrue(!Test3.StartsWith2("swarswar"));
        IsTrue(!Test3.StartsWith3("swarswar"));
        IsTrue(!Test3.StartsWith4("swarswar"));
        IsTrue(!Test3.StartsWith5("swarswar"));

        IsTrue(Test3.StartsWith1("simd"));
        IsTrue(Test3.StartsWith2("simd"));
        IsTrue(Test3.StartsWith3("simd"));
        IsTrue(Test3.StartsWith4("simd"));
        IsTrue(Test3.StartsWith5("simd"));

        IsTrue(!Test3.StartsWith1("simx"));
        IsTrue(!Test3.StartsWith2("simx"));
        IsTrue(!Test3.StartsWith3("simx"));
        IsTrue(!Test3.StartsWith4("simx"));
        IsTrue(!Test3.StartsWith5("simx"));

        IsTrue(Test3.StartsWith1("simdsimdsimd"));
        IsTrue(Test3.StartsWith2("simdsimdsimd"));
        IsTrue(Test3.StartsWith3("simdsimdsimd"));
        IsTrue(Test3.StartsWith4("simdsimdsimd"));
        IsTrue(Test3.StartsWith5("simdsimdsimd"));

        IsTrue(!Test3.StartsWith1("simdsimdsimdsimdsimdsimd"));
        IsTrue(!Test3.StartsWith2("simdsimdsimdsimdsimdsimd"));
        IsTrue(!Test3.StartsWith3("simdsimdsimdsimdsimdsimd"));
        IsTrue(!Test3.StartsWith4("simdsimdsimdsimdsimdsimd"));
        IsTrue(!Test3.StartsWith5("simdsimdsimdsimdsimdsimd"));

        return retCode;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void IsTrue(bool condition, [CallerLineNumber] int line = 0)
    {
        if (!condition)
        {
            Console.WriteLine($"Expected: true, line: {line}");
            retCode++;
        }
    }

    class Test1
    {
        private const string Str = "";
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool StartsWith1(string s) => Str.StartsWith(s, StringComparison.OrdinalIgnoreCase);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool StartsWith2(string s) => Str.StartsWith(s, StringComparison.Ordinal);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool StartsWith3(string s) => Str.AsSpan().StartsWith(s);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool StartsWith4(string s) => Str.AsSpan().StartsWith(s, StringComparison.Ordinal);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool StartsWith5(string s) => Str.AsSpan().StartsWith(s, StringComparison.OrdinalIgnoreCase);
    }

    class Test2
    {
        private const string Str = "swar";
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool StartsWith1(string s) => Str.StartsWith(s, StringComparison.OrdinalIgnoreCase);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool StartsWith2(string s) => Str.StartsWith(s, StringComparison.Ordinal);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool StartsWith3(string s) => Str.AsSpan().StartsWith(s);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool StartsWith4(string s) => Str.AsSpan().StartsWith(s, StringComparison.Ordinal);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool StartsWith5(string s) => Str.AsSpan().StartsWith(s, StringComparison.OrdinalIgnoreCase);
    }

    class Test3
    {
        private const string Str = "simdsimdsimd";
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool StartsWith1(string s) => Str.StartsWith(s, StringComparison.OrdinalIgnoreCase);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool StartsWith2(string s) => Str.StartsWith(s, StringComparison.Ordinal);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool StartsWith3(string s) => Str.AsSpan().StartsWith(s);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool StartsWith4(string s) => Str.AsSpan().StartsWith(s, StringComparison.Ordinal);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool StartsWith5(string s) => Str.AsSpan().StartsWith(s, StringComparison.OrdinalIgnoreCase);
    }
}
