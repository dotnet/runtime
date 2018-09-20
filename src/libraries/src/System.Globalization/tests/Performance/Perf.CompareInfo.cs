// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Microsoft.Xunit.Performance;
using System.Collections.Generic;

namespace System.Globalization.Tests
{
    public class Perf_CompareInfo
    {
        private static string GenerateInputString(char source, int count, char replaceChar, int replacePos)
        {
            char[] str = new char[count];
            for (int i = 0; i < count; i++)
            {
                str[i] = replaceChar;
            }
            str[replacePos] = replaceChar;

            return new string(str);
        }

        public static IEnumerable<object[]> s_compareTestData = new List<object[]>
        {
            new object[] { "", "string1", "string2", CompareOptions.None },
            new object[] { "tr-TR", "StrIng", "string", CompareOptions.IgnoreCase },
            new object[] { "en-US", "StrIng", "string", CompareOptions.OrdinalIgnoreCase },
            new object[] { "", "\u3060", "\u305F", CompareOptions.None },
            new object[] { "ja-JP", "ABCDE", "c", CompareOptions.None },
            new object[] { "es-ES", "$", "&", CompareOptions.IgnoreSymbols },
            new object[] { "", GenerateInputString('A', 10, '5', 5), GenerateInputString('A', 10, '5', 6), CompareOptions.Ordinal },
            new object[] { "", GenerateInputString('A', 100, 'X', 70), GenerateInputString('A', 100, 'X', 70), CompareOptions.OrdinalIgnoreCase },
            new object[] { "ja-JP", GenerateInputString('A', 100, 'D', 70), GenerateInputString('A', 100, 'd', 70), CompareOptions.OrdinalIgnoreCase },
            new object[] { "en-US", GenerateInputString('A', 1000, 'G', 500), GenerateInputString('A', 1000, 'G', 500), CompareOptions.None },
            new object[] { "en-US", GenerateInputString('\u3060', 1000, 'x', 500), GenerateInputString('\u3060', 1000, 'x', 10), CompareOptions.None },
            new object[] { "es-ES", GenerateInputString('\u3060', 100, '\u3059', 50), GenerateInputString('\u3060', 100, '\u3059', 50), CompareOptions.Ordinal },
            new object[] { "tr-TR", GenerateInputString('\u3060', 5000, '\u3059', 2501), GenerateInputString('\u3060', 5000, '\u3059', 2500), CompareOptions.Ordinal }
        };

        [Benchmark]
        [MemberData(nameof(s_compareTestData))]
        public void Compare(string culture, string string1, string string2, CompareOptions options)
        {
            CompareInfo compareInfo = CultureInfo.GetCultureInfo(culture).CompareInfo;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    compareInfo.Compare(string1, string2, options);
                }
        }

        public static IEnumerable<object[]> s_indexTestData = new List<object[]>
        {
            new object[] { "", "string1", "string2", CompareOptions.None },
            new object[] { "", "foobardzsdzs", "rddzs", CompareOptions.IgnoreCase },
            new object[] { "en-US", "StrIng", "string", CompareOptions.OrdinalIgnoreCase },
            new object[] { "", "\u3060", "\u305F", CompareOptions.None },
            new object[] { "ja-JP", "ABCDE", "c", CompareOptions.None },
            new object[] { "", "$", "&", CompareOptions.IgnoreSymbols },
            new object[] { "", "More Test's", "Tests", CompareOptions.IgnoreSymbols },
            new object[] { "es-ES", "TestFooBA\u0300R", "FooB\u00C0R", CompareOptions.IgnoreNonSpace },
            new object[] { "en-US", "Hello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello Worldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylong!xyz", "~", CompareOptions.Ordinal },
            new object[] { "en-US", "Hello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello Worldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylong!xyz", "w", CompareOptions.OrdinalIgnoreCase },
            new object[] { "es-ES", "Hello Worldbbbbbbbbbbbbbbcbbbbbbbbbbbbbbbbbbba!", "y", CompareOptions.Ordinal },
            new object[] { "", GenerateInputString('A', 10, '5', 5), "5", CompareOptions.Ordinal },
            new object[] { "", GenerateInputString('A', 100, 'X', 70), "x", CompareOptions.OrdinalIgnoreCase },
            new object[] { "ja-JP", GenerateInputString('A', 100, 'X', 70), "x", CompareOptions.OrdinalIgnoreCase },
            new object[] { "en-US", GenerateInputString('A', 1000, 'X', 500), "X", CompareOptions.None },
            new object[] { "en-US", GenerateInputString('\u3060', 1000, 'x', 500), "x", CompareOptions.None },
            new object[] { "es-ES", GenerateInputString('\u3060', 100, '\u3059', 50), "\u3059", CompareOptions.Ordinal }
        };

        [Benchmark]
        [MemberData(nameof(s_indexTestData))]
        public void IndexOf(string culture, string source, string value, CompareOptions options)
        {
            CompareInfo compareInfo = CultureInfo.GetCultureInfo(culture).CompareInfo;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    compareInfo.IndexOf(source, value, options);
                }
        }

        [Benchmark]
        [MemberData(nameof(s_indexTestData))]
        public void LastIndexOf(string culture, string source, string value, CompareOptions options)
        {
            CompareInfo compareInfo = CultureInfo.GetCultureInfo(culture).CompareInfo;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    compareInfo.LastIndexOf(source, value, options);
                }
        }

        public static IEnumerable<object[]> s_prefixTestData = new List<object[]>
        {
            new object[] { "", "string1", "str", CompareOptions.None },
            new object[] { "", "foobardzsdzs", "FooBarDZ", CompareOptions.IgnoreCase },
            new object[] { "en-US", "StrIng", "str", CompareOptions.OrdinalIgnoreCase },
            new object[] { "", "\u3060", "\u305F", CompareOptions.None },
            new object[] { "ja-JP", "ABCDE", "cd", CompareOptions.None },
            new object[] { "", "$", "&", CompareOptions.IgnoreSymbols },
            new object[] { "", "More's Test's", "More", CompareOptions.IgnoreSymbols },
            new object[] { "es-ES", "TestFooBA\u0300R", "FooB\u00C0R", CompareOptions.IgnoreNonSpace },
            new object[] { "es-ES", "Hello Worldbbbbbbbbbbbbbbcbbbbbbbbbbbbbbbbbbba!", "Hello World", CompareOptions.Ordinal },
            new object[] { "", GenerateInputString('A', 10, '5', 5), "AAAAA", CompareOptions.Ordinal },
            new object[] { "", GenerateInputString('A', 100, 'X', 70), new string('a', 30), CompareOptions.OrdinalIgnoreCase },
            new object[] { "ja-JP", GenerateInputString('A', 100, 'X', 70), new string('a', 70), CompareOptions.OrdinalIgnoreCase },
            new object[] { "en-US", GenerateInputString('A', 1000, 'X', 500), new string('A', 500), CompareOptions.None },
            new object[] { "en-US", GenerateInputString('\u3060', 1000, 'x', 500), new string('\u3060', 30), CompareOptions.None },
            new object[] { "es-ES", GenerateInputString('\u3060', 100, '\u3059', 50), "\u3060text", CompareOptions.Ordinal }
        };

        [Benchmark]
        [MemberData(nameof(s_prefixTestData))]
        public void IsPrefix(string culture, string source, string prefix, CompareOptions options)
        {
            CompareInfo compareInfo = CultureInfo.GetCultureInfo(culture).CompareInfo;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    compareInfo.IsPrefix(source, prefix, options);
                }
        }

        public static IEnumerable<object[]> s_suffixTestData = new List<object[]>
        {
            new object[] { "", "string1", "ing1", CompareOptions.None },
            new object[] { "", "foobardzsdzs", "DZsDzS", CompareOptions.IgnoreCase },
            new object[] { "en-US", "StrIng", "str", CompareOptions.OrdinalIgnoreCase },
            new object[] { "", "\u3060", "\u305F", CompareOptions.IgnoreSymbols },
            new object[] { "ja-JP", "ABCDE", "E", CompareOptions.None },
            new object[] { "", "$", "&", CompareOptions.IgnoreSymbols },
            new object[] { "", "More's Test's", "Test", CompareOptions.IgnoreSymbols },
            new object[] { "es-ES", "TestFooBA\u0300R", "FooB\u00C0R", CompareOptions.IgnoreNonSpace },
            new object[] { "", GenerateInputString('A', 10, '5', 5), "5AAAA", CompareOptions.Ordinal },
            new object[] { "", GenerateInputString('A', 100, 'X', 70), new string('a', 30), CompareOptions.OrdinalIgnoreCase },
            new object[] { "ja-JP", GenerateInputString('A', 100, 'X', 70), "x" + new string('a', 29), CompareOptions.OrdinalIgnoreCase },
            new object[] { "en-US", GenerateInputString('A', 1000, 'X', 100), new string('A', 900), CompareOptions.None },
            new object[] { "en-US", GenerateInputString('\u3060', 1000, 'x', 500), new string('\u3060', 30), CompareOptions.None },
            new object[] { "es-ES", GenerateInputString('\u3060', 100, '\u3059', 50), "\u3060text", CompareOptions.Ordinal }
        };

        [Benchmark]
        [MemberData(nameof(s_suffixTestData))]
        public void IsSuffix(string culture, string source, string suffix, CompareOptions options)
        {
            CompareInfo compareInfo = CultureInfo.GetCultureInfo(culture).CompareInfo;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    compareInfo.IsSuffix(source, suffix, options);
                }
        }

        [Benchmark]
        [InlineData("foo")]
        [InlineData("Exhibit \u00C0")]
        [InlineData("TestFooBA\u0300RnotsolongTELLme")]
        [InlineData("More Test's")]
        [InlineData("$")]
        [InlineData("\u3060")]
        [InlineData("Hello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello Worldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylong!xyz")]
        public void IsSortable(string text)
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    CompareInfo.IsSortable(text);
                }
        }
    }
}
