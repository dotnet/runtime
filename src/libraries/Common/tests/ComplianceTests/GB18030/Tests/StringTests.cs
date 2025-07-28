// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;
#pragma warning disable xUnit2009 // Do not use boolean check to check for substrings
#pragma warning disable xUnit2010 // Do not use boolean check to check for string equality

namespace GB18030.Tests;

public class StringTests
{
    private const string Dummy = "\uFFFF";

    [Theory]
    [MemberData(nameof(TestHelper.EncodedTestData), MemberType = typeof(TestHelper))]
    public unsafe void Ctor(byte[] encoded)
    {
        fixed (sbyte* p = (sbyte[])(object)encoded)
        {
            string s = new string(p, 0, encoded.Length, TestHelper.GB18030Encoding);
            Assert.True(encoded.AsSpan().SequenceEqual(TestHelper.GB18030Encoding.GetBytes(s)));
        }
    }

    public static IEnumerable<object[]> Compare_TestData() =>
        TestHelper.s_cultureInfos.SelectMany(culture =>
        TestHelper.s_compareOptions.SelectMany(option =>
        TestHelper.s_decodedTestData.Select(testData => new object[] { culture, option, testData })));

    [Theory]
    [MemberData(nameof(Compare_TestData))]
    public void Compare(CultureInfo culture, CompareOptions option, string decoded)
    {
#pragma warning disable 0618 // suppress obsolete warning for String.Copy
        string copy = string.Copy(decoded);
#pragma warning restore 0618
        Assert.True(string.Compare(decoded, copy, culture, option) == 0);
    }

    [Theory]
    [MemberData(nameof(TestHelper.DecodedTestData), MemberType = typeof(TestHelper))]
    public void Contains(string decoded)
    {
        for (int i = 0; i < decoded.Length; i++)
            Assert.True(decoded.Contains(decoded.Substring(0, i)));

        for (int i = decoded.Length - 1; i >= 0; i--)
            Assert.True(decoded.Contains(decoded.Substring(i)));
    }

    [Theory]
    [MemberData(nameof(TestHelper.DecodedTestData), MemberType = typeof(TestHelper))]
    public void String_Equals(string decoded)
    {
#pragma warning disable 0618 // suppress obsolete warning for String.Copy
        string copy = string.Copy(decoded);
#pragma warning restore 0618
        Assert.True(decoded.Equals(decoded));
        Assert.True(decoded.Equals(copy));
        Assert.True(decoded.Equals((object)copy));
        Assert.True(string.Equals(decoded, copy));

        Assert.False(decoded.Equals(copy + Dummy));
        Assert.False(decoded.Equals(Dummy + copy));
        Assert.False(decoded.Equals(copy.Substring(0, copy.Length / 2) + Dummy + copy.Substring(copy.Length / 2)));
        Assert.False(decoded.Equals(null));
    }

    public static IEnumerable<object[]> StringComparison_TestData() =>
        TestHelper.s_allStringComparisons.SelectMany(comparison =>
        TestHelper.s_decodedTestData.Select(decoded => new object[] { comparison, decoded }));

    [Theory]
    [MemberData(nameof(StringComparison_TestData))]
    public void String_Equals_StringComparison(StringComparison comparison, string decoded)
    {
#pragma warning disable 0618 // suppress obsolete warning for String.Copy
        string copy = string.Copy(decoded);
#pragma warning restore 0618
        if ((int)comparison % 2 != 0) // Odd values are *IgnoreCase
        {
            Assert.True(decoded.ToLower().Equals(copy.ToUpper(), comparison));
            Assert.True(string.Equals(decoded.ToUpper(), copy.ToLower(), comparison));
        }
        else
        {
            Assert.True(decoded.Equals(copy, comparison));
            Assert.True(string.Equals(decoded, copy, comparison));
        }
    }

    public static IEnumerable<object[]> EndsStartsWith_TestData() =>
        TestHelper.s_cultureInfos.SelectMany(culture =>
        new bool[] { true, false }.SelectMany(ignoreCase =>
        TestHelper.s_decodedTestData.Select(testData => new object[] { culture, ignoreCase, testData })));

    [Theory]
    [MemberData(nameof(EndsStartsWith_TestData))]
    public void EndsWith(CultureInfo culture, bool ignoreCase, string decoded)
    {
        string suffix = string.Empty;
        foreach (string textElement in TestHelper.GetTextElements(decoded).Reverse())
        {
            suffix = textElement + suffix;
            if (ignoreCase)
                Assert.True(decoded.ToUpper().EndsWith(suffix.ToLower(), ignoreCase, culture));
            else
                Assert.True(decoded.EndsWith(suffix, ignoreCase, culture));
        }
    }

    public static IEnumerable<object[]> EndsStartsWith_Ordinal_TestData() =>
        new StringComparison[]
        {
            StringComparison.Ordinal,
            StringComparison.OrdinalIgnoreCase
        }
        .SelectMany(culture =>
        TestHelper.s_decodedTestData.Select(testData => new object[] { culture, testData }));

    [Theory]
    [MemberData(nameof(EndsStartsWith_Ordinal_TestData))]
    public void EndsWith_Ordinal(StringComparison comparison, string decoded)
    {
        for (int i = decoded.Length - 1; i >= 0; i--)
        {
            if (comparison == StringComparison.OrdinalIgnoreCase)
                Assert.True(decoded.ToLower().EndsWith(decoded.Substring(i).ToUpper(), comparison));
            else
                Assert.True(decoded.EndsWith(decoded.Substring(i), comparison));
        }
    }

    [Theory]
    [MemberData(nameof(EndsStartsWith_TestData))]
    public void StartsWith(CultureInfo culture, bool ignoreCase, string decoded)
    {
        string prefix = string.Empty;
        foreach (string textElement in TestHelper.GetTextElements(decoded))
        {
            prefix += textElement;
            if (ignoreCase)
                Assert.True(decoded.ToUpper().StartsWith(prefix.ToLower(), ignoreCase, culture));
            else
                Assert.True(decoded.StartsWith(prefix, ignoreCase, culture));
        }
    }

    [Theory]
    [MemberData(nameof(EndsStartsWith_Ordinal_TestData))]
    public void StartsWith_Ordinal(StringComparison comparison, string decoded)
    {
        for (int i = 0; i < decoded.Length; i++)
        {
            if (comparison == StringComparison.OrdinalIgnoreCase)
                Assert.True(decoded.ToLower().StartsWith(decoded.Substring(0, i).ToUpper(), comparison));
            else
                Assert.True(decoded.StartsWith(decoded.Substring(0, i), comparison));
        }
    }

    [Theory]
    [MemberData(nameof(StringComparison_TestData))]
    public void IndexOf_MultipleElements(StringComparison comparison, string decoded)
    {
        bool ignoreCase = (int)comparison % 2 != 0;
        if (comparison is StringComparison.Ordinal or StringComparison.OrdinalIgnoreCase)
        {
            for (int i = 0; i < decoded.Length; i++)
            {
                string left = decoded.Substring(0, i);
                string right = decoded.Substring(i);
                Assert.Equal(decoded.Length, left.Length + right.Length);

                Assert.Equal(0, decoded.IndexOf(left, comparison));
                // right substring starts at the end of left one.
                Assert.Equal(left.Length, decoded.IndexOf(right, startIndex: left.Length, comparison));

                if (ignoreCase)
                {
                    Assert.Equal(0, decoded.ToLower().IndexOf(left.ToUpper(), comparison));
                    Assert.Equal(left.Length, decoded.ToLower().IndexOf(right.ToUpper(), startIndex: left.Length, comparison));
                }
            }
        }
        else
        {
            string[] textElements = TestHelper.GetTextElements(decoded).ToArray();
            for (int i = 0; i < textElements.Length; i++)
            {
                string left = string.Concat(textElements.Take(i));
                string right = string.Concat(textElements.Skip(i));
                Assert.Equal(decoded.Length, left.Length + right.Length);

                Assert.Equal(0, decoded.IndexOf(left, comparison));
                Assert.Equal(left.Length, decoded.IndexOf(right, startIndex: left.Length, comparison));

                if (ignoreCase)
                {
                    Assert.Equal(0, decoded.ToLower().IndexOf(left.ToUpper(), comparison));
                    Assert.Equal(left.Length, decoded.ToLower().IndexOf(right.ToUpper(), startIndex: left.Length, comparison));
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(StringComparison_TestData))]
    public void IndexOf_SingleElement(StringComparison comparison, string decoded)
    {
        bool ignoreCase = (int)comparison % 2 != 0;
        if (comparison is StringComparison.Ordinal or StringComparison.OrdinalIgnoreCase)
        {
            for (int i = 0; i < decoded.Length; i++)
            {
                string current = decoded.Substring(i, 1);
                // Fast-check the expected index.
                Assert.Equal(i, decoded.IndexOf(current, startIndex: i, comparison));
                IndexOf_SingleElement_Core(current, expectedIndex: i, ignoreCase);
            }
        }
        else
        {
            string[] textElements = TestHelper.GetTextElements(decoded).ToArray();
            for (int i = 0; i < textElements.Length; i++)
            {
                string current = textElements[i];
                int expectedIndex = textElements.Take(i).Sum(e => e.Length);
                // Fast-check the expected index.
                Assert.Equal(expectedIndex, decoded.IndexOf(current, startIndex: expectedIndex, comparison));
                IndexOf_SingleElement_Core(current, expectedIndex, ignoreCase);
            }
        }

        void IndexOf_SingleElement_Core(string current, int expectedIndex, bool ignoreCase)
        {
            int startIndex = 0;
            while (true)
            {
                int result = ignoreCase ?
                    decoded.ToLower().IndexOf(current.ToUpper(), startIndex, comparison) :
                    decoded.IndexOf(current, startIndex, comparison);

                if (result == -1 || result > expectedIndex)
                    Assert.Fail($"'{current}' not found or found too late in '{decoded}'");
                else if (result < expectedIndex)
                    startIndex = result + current.Length;
                else
                {
                    Assert.Equal(expectedIndex, result);
                    break;
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(StringComparison_TestData))]
    public void LastIndexOf_MultipleElements(StringComparison comparison, string decoded)
    {
        bool ignoreCase = (int)comparison % 2 != 0;

        // Don't deal with LastIndexOf(string.Empty) nuances, test against full length outside of the loop.
        // see https://learn.microsoft.com/dotnet/core/compatibility/core-libraries/5.0/lastindexof-improved-handling-of-empty-values
        Assert.Equal(0, decoded.LastIndexOf(decoded, comparison));
        if (ignoreCase)
            Assert.Equal(0, decoded.ToLower().LastIndexOf(decoded.ToUpper(), comparison));

        if (comparison is StringComparison.Ordinal or StringComparison.OrdinalIgnoreCase)
        {

            for (int i = 1; i < decoded.Length; i++)
            {
                string left = decoded.Substring(0, i);
                string right = decoded.Substring(i);
                Assert.Equal(decoded.Length, left.Length + right.Length);

                Assert.Equal(0, decoded.LastIndexOf(left, startIndex: left.Length - 1, comparison));
                // right substring starts at the end of left one.
                Assert.Equal(left.Length, decoded.LastIndexOf(right, comparison));

                if (ignoreCase)
                {
                    Assert.Equal(0, decoded.ToLower().LastIndexOf(left.ToUpper(), startIndex: left.Length - 1, comparison));
                    Assert.Equal(left.Length, decoded.ToLower().LastIndexOf(right.ToUpper(), comparison));
                }
            }
        }
        else
        {
            string[] textElements = TestHelper.GetTextElements(decoded).ToArray();
            for (int i = 1; i < textElements.Length; i++)
            {
                string left = string.Concat(textElements.Take(i));
                string right = string.Concat(textElements.Skip(i));
                Assert.Equal(decoded.Length, left.Length + right.Length);

                Assert.Equal(0, decoded.LastIndexOf(left, startIndex: left.Length - 1, comparison));
                Assert.Equal(left.Length, decoded.LastIndexOf(right, comparison));
                
                if (ignoreCase)
                {
                    Assert.Equal(0, decoded.ToLower().LastIndexOf(left.ToUpper(), startIndex: left.Length - 1, comparison));
                    Assert.Equal(left.Length, decoded.ToLower().LastIndexOf(right.ToUpper(), comparison));
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(StringComparison_TestData))]
    public void LastIndexOf_SingleElement(StringComparison comparison, string decoded)
    {
        bool ignoreCase = (int)comparison % 2 != 0;
        if (comparison is StringComparison.Ordinal or StringComparison.OrdinalIgnoreCase)
        {
            for (int i = 0; i < decoded.Length; i++)
            {
                string current = decoded.Substring(i, 1);
                // Fast-check the expected index.
                Assert.Equal(i, decoded.LastIndexOf(current, startIndex: i, comparison));
                LastIndexOf_SingleElement_Core(current, expectedIndex: i, ignoreCase);
            }
        }
        else
        {
            string[] textElements = TestHelper.GetTextElements(decoded).ToArray();
            for (int i = 0; i < textElements.Length; i++)
            {
                string current = textElements[i];
                int expectedIndex = textElements.Take(i).Sum(e => e.Length);
                // Fast-check the expected index.
                Assert.Equal(expectedIndex, decoded.LastIndexOf(current, startIndex: expectedIndex + current.Length - 1, comparison));
                LastIndexOf_SingleElement_Core(current, expectedIndex, ignoreCase);
            }
        }

        void LastIndexOf_SingleElement_Core(string current, int expectedIndex, bool ignoreCase)
        {
            int startIndex = decoded.Length - 1;
            while (true)
            {
                int result = ignoreCase ?
                    decoded.ToLower().LastIndexOf(current.ToUpper(), startIndex, comparison) :
                    decoded.LastIndexOf(current, startIndex, comparison);

                if (result == -1 || result < expectedIndex)
                    Assert.Fail($"'{current}' not found or found too late in '{decoded}'");
                else if (result > expectedIndex)
                    startIndex = result - 1;
                else
                {
                    Assert.Equal(expectedIndex, result);
                    break;
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(TestHelper.DecodedTestData), MemberType = typeof(TestHelper))]
    public void Replace(string decoded)
    {
        Assert.False(decoded.Contains(Dummy));

        foreach (string textElement in TestHelper.GetTextElements(decoded))
        {
            int occurrences = decoded.Split([textElement], StringSplitOptions.None).Length;

            string replaced = decoded.Replace(textElement, Dummy);
            Assert.Equal(occurrences, replaced.Split([Dummy], StringSplitOptions.None).Length);

            if (textElement.Length == 1)
            {
                replaced = decoded.Replace(textElement[0], Dummy[0]);
                Assert.Equal(occurrences, replaced.Split([Dummy], StringSplitOptions.None).Length);
            }
        }
    }

    [Theory]
    [MemberData(nameof(TestHelper.DecodedTestData), MemberType = typeof(TestHelper))]
    public void Split(string decoded)
    {
        Assert.Single(Split(decoded, Dummy));
        Assert.Single(Split(decoded, Dummy[0]));

        string[] textElements = TestHelper.GetTextElements(decoded).ToArray();

        for (int i = 0; i < textElements.Length; i++)
        {
            var result = Split(decoded, textElements[i]);
            Assert.True(result.Length > 1);
        }

        static string[] Split<T>(string str, params T[] separators) =>
            separators switch
            {
                char[] chars => str.Split(chars, StringSplitOptions.None),
                string[] strings => str.Split(strings, StringSplitOptions.None),
                _ => throw new ArgumentException()
            };
    }
}
