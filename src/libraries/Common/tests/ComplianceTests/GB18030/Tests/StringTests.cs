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
    [MemberData(nameof(TestHelper.EncodedMemberData), MemberType = typeof(TestHelper))]
    public unsafe void Ctor(byte[] encoded)
    {
        fixed (sbyte* p = (sbyte[])(object)encoded)
        {
            string s = new string(p, 0, encoded.Length, TestHelper.GB18030Encoding);
            Assert.True(encoded.AsSpan().SequenceEqual(TestHelper.GB18030Encoding.GetBytes(s)));
        }
    }

    public static IEnumerable<object[]> Compare_MemberData() =>
        TestHelper.DecodedTestData.SelectMany(testData =>
        TestHelper.Cultures.SelectMany(culture =>
        TestHelper.CompareOptions.Select(option => new object[] { testData, culture, option })));

    [Theory]
    [MemberData(nameof(Compare_MemberData))]
    public void Compare(string decoded, CultureInfo culture, CompareOptions option)
    {
#pragma warning disable 0618 // suppress obsolete warning for String.Copy
        string copy = string.Copy(decoded);
#pragma warning restore 0618
        Assert.True(string.Compare(decoded, copy, culture, option) == 0);
    }

    public static IEnumerable<object[]> Contains_MemberData() =>
        TestHelper.DecodedTestData.SelectMany(testData =>
        TestHelper.NonOrdinalStringComparisons.Select(comparison => new object[] { testData, comparison  }));

    [Theory]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
    [MemberData(nameof(Contains_MemberData))]
    public void Contains(string decoded, StringComparison comparison)
    {
        string current = string.Empty;
        foreach (string element in TestHelper.GetTextElements(decoded))
        {
            current += element;
            Assert.True(decoded.Contains(current, comparison));
        }

        current = string.Empty;
        foreach (string element in TestHelper.GetTextElements(decoded).Reverse())
        {
            current = element + current;
            Assert.True(decoded.Contains(current, comparison));
        }
    }

    public static IEnumerable<object[]> StringComparison_MemberData() =>
        TestHelper.DecodedTestData.SelectMany(decoded =>
        TestHelper.NonOrdinalStringComparisons.Select(comparison => new object[] { decoded, comparison }));

    [Theory]
    [MemberData(nameof(StringComparison_MemberData))]
    public void String_Equals(string decoded, StringComparison comparison)
    {
#pragma warning disable 0618 // suppress obsolete warning for String.Copy
        string copy = string.Copy(decoded);
#pragma warning restore 0618

        Assert.True(decoded.Equals(copy, comparison));
        Assert.True(string.Equals(decoded, copy, comparison));

        string[] elements = TestHelper.GetTextElements(decoded).ToArray();
        for (int i = 0; i < elements.Length; i++)
        {
            string left = string.Concat(elements.Take(i));
            string right = string.Concat(elements.Skip(i));
            Assert.True(decoded.Equals(left + '\0' + right, comparison));
        }
    }

    public static IEnumerable<object[]> EndsStartsWith_MemberData() =>
        TestHelper.DecodedTestData.SelectMany(testData =>
        TestHelper.Cultures.Select(culture => new object[] { testData, culture }));

    [Theory]
    [MemberData(nameof(EndsStartsWith_MemberData))]
    public void EndsWith(string decoded, CultureInfo culture)
    {
        string suffix = string.Empty;
        foreach (string textElement in TestHelper.GetTextElements(decoded).Reverse())
        {
            suffix = textElement + suffix;
            Assert.True(decoded.EndsWith(suffix, ignoreCase: false, culture));
        }
    }

    [Theory]
    [MemberData(nameof(EndsStartsWith_MemberData))]
    public void StartsWith(string decoded, CultureInfo culture)
    {
        string prefix = string.Empty;
        foreach (string textElement in TestHelper.GetTextElements(decoded))
        {
            prefix += textElement;
            Assert.True(decoded.StartsWith(prefix, ignoreCase: false, culture));
        }
    }

    [Theory]
    [MemberData(nameof(StringComparison_MemberData))]
    public void IndexOf_MultipleElements(string decoded, StringComparison comparison)
    {
        Assert.NotEqual(StringComparison.Ordinal, comparison);
        Assert.NotEqual(StringComparison.OrdinalIgnoreCase, comparison);

        string[] textElements = TestHelper.GetTextElements(decoded).ToArray();
        for (int i = 0; i < textElements.Length; i++)
        {
            string left = string.Concat(textElements.Take(i));
            string right = string.Concat(textElements.Skip(i));
            Assert.Equal(decoded.Length, left.Length + right.Length);

            Assert.Equal(0, decoded.IndexOf(left, comparison));
            Assert.Equal(left.Length, decoded.IndexOf(right, startIndex: left.Length, comparison));
        }
    }

    [Theory]
    [MemberData(nameof(StringComparison_MemberData))]
    public void IndexOf_SingleElement(string decoded, StringComparison comparison)
    {
        Assert.NotEqual(StringComparison.Ordinal, comparison);
        Assert.NotEqual(StringComparison.OrdinalIgnoreCase, comparison);

        string[] textElements = TestHelper.GetTextElements(decoded).ToArray();
        for (int i = 0; i < textElements.Length; i++)
        {
            string current = textElements[i];
            int expectedIndex = textElements.Take(i).Sum(e => e.Length);
            // Fast-check the expected index.
            Assert.Equal(expectedIndex, decoded.IndexOf(current, startIndex: expectedIndex, comparison));
            IndexOf_SingleElement_Slow(current, expectedIndex);
        }

        void IndexOf_SingleElement_Slow(string current, int expectedIndex)
        {
            int startIndex = 0;
            while (true)
            {
                int result = decoded.IndexOf(current, startIndex, comparison);

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
    [MemberData(nameof(StringComparison_MemberData))]
    public void LastIndexOf_MultipleElements(string decoded, StringComparison comparison)
    {
        Assert.NotEqual(StringComparison.Ordinal, comparison);
        Assert.NotEqual(StringComparison.OrdinalIgnoreCase, comparison);

        // Don't deal with LastIndexOf(string.Empty) nuances, test against full length outside of the loop.
        // see https://learn.microsoft.com/dotnet/core/compatibility/core-libraries/5.0/lastindexof-improved-handling-of-empty-values
        Assert.Equal(0, decoded.LastIndexOf(decoded, comparison));

        string[] textElements = TestHelper.GetTextElements(decoded).ToArray();
        for (int i = 1; i < textElements.Length; i++)
        {
            string left = string.Concat(textElements.Take(i));
            string right = string.Concat(textElements.Skip(i));
            Assert.Equal(decoded.Length, left.Length + right.Length);

            Assert.Equal(0, decoded.LastIndexOf(left, startIndex: left.Length - 1, comparison));
            Assert.Equal(left.Length, decoded.LastIndexOf(right, comparison));
        }
    }

    [Theory]
    [MemberData(nameof(StringComparison_MemberData))]
    public void LastIndexOf_SingleElement(string decoded, StringComparison comparison)
    {
        Assert.NotEqual(StringComparison.Ordinal, comparison);
        Assert.NotEqual(StringComparison.OrdinalIgnoreCase, comparison);

        string[] textElements = TestHelper.GetTextElements(decoded).ToArray();
        for (int i = 0; i < textElements.Length; i++)
        {
            string current = textElements[i];
            int expectedIndex = textElements.Take(i).Sum(e => e.Length);
            // Fast-check the expected index.
            Assert.Equal(expectedIndex, decoded.LastIndexOf(current, startIndex: expectedIndex + current.Length - 1, comparison));
            LastIndexOf_SingleElement_Slow(current, expectedIndex);
        }

        void LastIndexOf_SingleElement_Slow(string current, int expectedIndex)
        {
            int startIndex = decoded.Length - 1;
            while (true)
            {
                int result = decoded.LastIndexOf(current, startIndex, comparison);

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
    [MemberData(nameof(TestHelper.DecodedMemberData), MemberType = typeof(TestHelper))]
    public void Replace(string decoded)
    {
        Assert.False(decoded.Contains(Dummy));

        foreach (string textElement in TestHelper.GetTextElements(decoded))
        {
            int occurrences = SplitHelper(decoded, textElement).Length;
            string replaced = decoded.Replace(textElement, Dummy);
            Assert.Equal(occurrences, SplitHelper(replaced, Dummy).Length);

            if (textElement.Length == 1)
            {
                replaced = decoded.Replace(textElement[0], Dummy[0]);
                Assert.Equal(occurrences, SplitHelper(replaced, Dummy).Length);
            }
        }
    }

    public static IEnumerable<object[]> Replace_NetCore_MemberData() =>
        TestHelper.DecodedTestData.SelectMany(testData =>
        TestHelper.Cultures.Select(culture => new object[] { testData, culture }));

#if NETCOREAPP
    [Theory]
    [MemberData(nameof(Replace_NetCore_MemberData))]
    public void Replace_CultureInfo(string decoded, CultureInfo culture)
    {
        Assert.False(decoded.Contains(Dummy));

        foreach (string textElement in TestHelper.GetTextElements(decoded))
        {
            int expected = SplitHelper(decoded, textElement).Length;
            string replaced = decoded.Replace(textElement, Dummy, ignoreCase: false, culture);
            Assert.True(expected == SplitHelper(replaced, Dummy).Length ||
                // Exception for non zh-CN culture, where '0' and '〇' are considered equal.
                (culture.Name != "zh-CN" && textElement == "\u3007" && decoded.Contains('0')) ||
                (culture.Name != "zh-CN" && textElement == "0" && decoded.Contains('\u3007')),
                $"Values differ for text element {textElement}");
        }
    }
#endif

    [Theory]
    [MemberData(nameof(TestHelper.DecodedMemberData), MemberType = typeof(TestHelper))]
    public void Split(string decoded)
    {
        string[] textElements = TestHelper.GetTextElements(decoded).ToArray();

        for (int i = 0; i < textElements.Length; i++)
        {
            var result = SplitHelper(decoded, textElements[i]);
            Assert.True(result.Length > 1);
        }
    }

    private static string[] SplitHelper<T>(string str, params T[] separators) =>
        separators switch
        {
            char[] chars => str.Split(chars, StringSplitOptions.None),
            string[] strings => str.Split(strings, StringSplitOptions.None),
            _ => throw new ArgumentException()
        };
}
