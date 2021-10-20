// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Reflection;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.Globalization.Tests
{
    public class InvariantModeTests
    {
        private static bool PredefinedCulturesOnlyIsDisabled { get; } = !PredefinedCulturesOnly();
        private static bool PredefinedCulturesOnly()
        {
            bool ret;

            try
            {
                ret = (bool) typeof(object).Assembly.GetType("System.Globalization.GlobalizationMode").GetProperty("PredefinedCulturesOnly", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            }
            catch
            {
                ret = false;
            }

            return ret;
        }

        public static IEnumerable<object[]> Cultures_TestData()
        {
            yield return new object[] { "en-US" };
            yield return new object[] { "ja-JP" };
            yield return new object[] { "fr-FR" };
            yield return new object[] { "tr-TR" };
            yield return new object[] { "" };
        }

        private static readonly string[] s_cultureNames = new string[] { "en-US", "ja-JP", "fr-FR", "tr-TR", "" };

        public static IEnumerable<object[]> IndexOf_TestData()
        {
            // Empty string
            yield return new object[] { "foo", "", 0, 3, CompareOptions.None, 0 };
            yield return new object[] { "", "", 0, 0, CompareOptions.None, 0 };

            // OrdinalIgnoreCase
            yield return new object[] { "Hello", "l", 0, 5, CompareOptions.OrdinalIgnoreCase, 2 };
            yield return new object[] { "Hello", "L", 0, 5, CompareOptions.OrdinalIgnoreCase, 2 };
            yield return new object[] { "Hello", "h", 0, 5, CompareOptions.OrdinalIgnoreCase, 0 };

            yield return new object[] { "Hello\u00D3\u00D4", "\u00F3\u00F4", 0, 7, CompareOptions.OrdinalIgnoreCase, 5 };
            yield return new object[] { "Hello\u00D3\u00D4", "\u00F3\u00F5", 0, 7, CompareOptions.OrdinalIgnoreCase, -1 };

            yield return new object[] { "Hello\U00010400", "\U00010428", 0, 7, CompareOptions.OrdinalIgnoreCase, 5 };


            // Long strings
            yield return new object[] { new string('b', 100) + new string('a', 5555), "aaaaaaaaaaaaaaa", 0, 5655, CompareOptions.None, 100 };
            yield return new object[] { new string('b', 101) + new string('a', 5555), new string('a', 5000), 0, 5656, CompareOptions.None, 101 };
            yield return new object[] { new string('a', 5555), new string('a', 5000) + "b", 0, 5555, CompareOptions.None, -1 };

            // Hungarian
            yield return new object[] { "foobardzsdzs", "rddzs", 0, 12, CompareOptions.Ordinal, -1 };
            yield return new object[] { "foobardzsdzs", "rddzs", 0, 12, CompareOptions.None, -1 };
            yield return new object[] { "foobardzsdzs", "rddzs", 0, 12, CompareOptions.Ordinal, -1 };

            // Turkish
            yield return new object[] { "Hi", "I", 0, 2, CompareOptions.None, -1 };
            yield return new object[] { "Hi", "I", 0, 2, CompareOptions.IgnoreCase, 1 };
            yield return new object[] { "Hi", "\u0130", 0, 2, CompareOptions.None, -1 };
            yield return new object[] { "Hi", "\u0130", 0, 2, CompareOptions.IgnoreCase, -1 };
            yield return new object[] { "Hi", "I", 0, 2, CompareOptions.None, -1 };
            yield return new object[] { "Hi", "\u0130", 0, 2, CompareOptions.IgnoreCase, -1 };

            // Unicode
            yield return new object[] { "Hi", "\u0130", 0, 2, CompareOptions.None, -1 };
            yield return new object[] { "Exhibit \u00C0", "A\u0300", 0, 9, CompareOptions.None, -1 };
            yield return new object[] { "Exhibit \u00C0", "A\u0300", 0, 9, CompareOptions.Ordinal, -1 };
            yield return new object[] { "Exhibit \u00C0", "a\u0300", 0, 9, CompareOptions.None, -1 };
            yield return new object[] { "Exhibit \u00C0", "a\u0300", 0, 9, CompareOptions.Ordinal, -1 };
            yield return new object[] { "Exhibit \u00C0", "a\u0300", 0, 9, CompareOptions.IgnoreCase, -1 };
            yield return new object[] { "Exhibit \u00C0", "a\u0300", 0, 9, CompareOptions.OrdinalIgnoreCase, -1 };
            yield return new object[] { "FooBar", "Foo\u0400Bar", 0, 6, CompareOptions.Ordinal, -1 };
            yield return new object[] { "TestFooBA\u0300R", "FooB\u00C0R", 0, 11, CompareOptions.IgnoreNonSpace, -1 };

            // Weightless characters
            yield return new object[] { "", "\u200d", 0, 0, CompareOptions.None, -1 };
            yield return new object[] { "hello", "\u200d", 0, 5, CompareOptions.IgnoreCase, -1 };

            // Ignore symbols
            yield return new object[] { "More Test's", "Tests", 0, 11, CompareOptions.IgnoreSymbols, -1 };
            yield return new object[] { "More Test's", "Tests", 0, 11, CompareOptions.None, -1 };
            yield return new object[] { "cbabababdbaba", "ab", 0, 13, CompareOptions.None, 2 };

            // Ordinal should be case-se
            yield return new object[] { "a", "a", 0, 1, CompareOptions.Ordinal, 0 };
            yield return new object[] { "a", "A", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "abc", "aBc", 0, 3, CompareOptions.Ordinal, -1 };

            // Ordinal with numbers and
            yield return new object[] { "a", "1", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "1", "1", 0, 1, CompareOptions.Ordinal, 0 };
            yield return new object[] { "1", "!", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "a", "-", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "-", "-", 0, 1, CompareOptions.Ordinal, 0 };
            yield return new object[] { "-", "!", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "!", "!", 0, 1, CompareOptions.Ordinal, 0 };

            // Ordinal with unicode
            yield return new object[] { "\uFF21", "\uFE57", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "\uFE57", "\uFF21", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "\uFF21", "a\u0400Bc", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "\uFE57", "a\u0400Bc", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "a", "a\u0400Bc", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "a\u0400Bc", "a", 0, 4, CompareOptions.Ordinal, 0 };

            // Ordinal with I or i
            yield return new object[] { "I", "i", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "I", "I", 0, 1, CompareOptions.Ordinal, 0 };
            yield return new object[] { "i", "I", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "i", "i", 0, 1, CompareOptions.Ordinal, 0 };
            yield return new object[] { "I", "\u0130", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "\u0130", "I", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "i", "\u0130", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "\u0130", "i", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "I", "\u0131", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "\0131", "I", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "i", "\u0131", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "\u0131", "i", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "\u0130", "\u0130", 0, 1, CompareOptions.Ordinal, 0 };
            yield return new object[] { "\u0131", "\u0131", 0, 1, CompareOptions.Ordinal, 0 };
            yield return new object[] { "\u0130", "\u0131", 0, 1, CompareOptions.Ordinal, -1 };
            yield return new object[] { "\u0131", "\u0130", 0, 1, CompareOptions.Ordinal, -1 };

            // Platform differences
            yield return new object[] { "foobardzsdzs", "rddzs", 0, 12, CompareOptions.None, -1 };
        }

        public static IEnumerable<object[]> LastIndexOf_TestData()
        {
            // Empty strings
            yield return new object[] { "foo", "", 2, 3, CompareOptions.None, 3 };
            yield return new object[] { "", "", 0, 0, CompareOptions.None, 0 };
            yield return new object[] { "", "a", 0, 0, CompareOptions.None, -1 };
            yield return new object[] { "", "", -1, 0, CompareOptions.None, 0 };
            yield return new object[] { "", "a", -1, 0, CompareOptions.None, -1 };
            yield return new object[] { "", "", 0, -1, CompareOptions.None, 0 };
            yield return new object[] { "", "a", 0, -1, CompareOptions.None, -1 };

            // Start index = source.Length
            yield return new object[] { "Hello", "l", 5, 5, CompareOptions.None, 3 };
            yield return new object[] { "Hello", "b", 5, 5, CompareOptions.None, -1 };
            yield return new object[] { "Hello", "l", 5, 0, CompareOptions.None, -1 };

            yield return new object[] { "Hello", "", 5, 5, CompareOptions.None, 5 };
            yield return new object[] { "Hello", "", 5, 0, CompareOptions.None, 5 };

            // OrdinalIgnoreCase
            yield return new object[] { "Hello", "l", 4, 5, CompareOptions.OrdinalIgnoreCase, 3 };
            yield return new object[] { "Hello", "L", 4, 5, CompareOptions.OrdinalIgnoreCase, 3 };
            yield return new object[] { "Hello", "h", 4, 5, CompareOptions.OrdinalIgnoreCase, 0 };


            yield return new object[] { "Hello\u00D3\u00D4\u00D3\u00D4", "\u00F3\u00F4", 8, 9, CompareOptions.OrdinalIgnoreCase, 7 };
            yield return new object[] { "Hello\u00D3\u00D4\u00D3\u00D4", "\u00F3\u00F5", 8, 9, CompareOptions.OrdinalIgnoreCase, -1 };

            yield return new object[] { "Hello\U00010400\U00010400", "\U00010428", 8, 9, CompareOptions.OrdinalIgnoreCase, 7 };

            // Long strings
            yield return new object[] { new string('a', 5555) + new string('b', 100), "aaaaaaaaaaaaaaa", 5654, 5655, CompareOptions.None, 5540 };
            yield return new object[] { new string('b', 101) + new string('a', 5555), new string('a', 5000), 5655, 5656, CompareOptions.None, 656 };
            yield return new object[] { new string('a', 5555), new string('a', 5000) + "b", 5554, 5555, CompareOptions.None, -1 };

            // Hungarian
            yield return new object[] { "foobardzsdzs", "rddzs", 11, 12, CompareOptions.Ordinal, -1 };
            yield return new object[] { "foobardzsdzs", "rddzs", 11, 12, CompareOptions.None, -1 };
            yield return new object[] { "foobardzsdzs", "rddzs", 11, 12, CompareOptions.Ordinal, -1 };

            // Turkish
            yield return new object[] { "Hi", "I", 1, 2, CompareOptions.None, -1 };
            yield return new object[] { "Hi", "I", 1, 2, CompareOptions.IgnoreCase, 1 };
            yield return new object[] { "Hi", "\u0130", 1, 2, CompareOptions.None, -1 };
            yield return new object[] { "Hi", "\u0130", 1, 2, CompareOptions.IgnoreCase, -1 };

            yield return new object[] { "Hi", "I", 1, 2, CompareOptions.None, -1 };
            yield return new object[] { "Hi", "I", 1, 2, CompareOptions.IgnoreCase, 1 };
            yield return new object[] { "Hi", "\u0130", 1, 2, CompareOptions.None, -1 };
            yield return new object[] { "Hi", "\u0130", 1, 2, CompareOptions.IgnoreCase, -1 };

            // Unicode
            yield return new object[] { "Exhibit \u00C0", "A\u0300", 8, 9, CompareOptions.None, -1 };
            yield return new object[] { "Exhibit \u00C0", "A\u0300", 8, 9, CompareOptions.Ordinal, -1 };
            yield return new object[] { "Exhibit \u00C0", "a\u0300", 8, 9, CompareOptions.None, -1 };
            yield return new object[] { "Exhibit \u00C0", "a\u0300", 8, 9, CompareOptions.IgnoreCase, -1 };
            yield return new object[] { "Exhibit \u00C0", "a\u0300", 8, 9, CompareOptions.OrdinalIgnoreCase, -1 };
            yield return new object[] { "Exhibit \u00C0", "a\u0300", 8, 9, CompareOptions.Ordinal, -1 };
            yield return new object[] { "FooBar", "Foo\u0400Bar", 5, 6, CompareOptions.Ordinal, -1 };
            yield return new object[] { "TestFooBA\u0300R", "FooB\u00C0R", 10, 11, CompareOptions.IgnoreNonSpace, -1 };

            // Weightless characters
            yield return new object[] { "", "\u200d", 0, 0, CompareOptions.None, -1 };
            yield return new object[] { "", "\u200d", -1, 0, CompareOptions.None, -1 };
            yield return new object[] { "hello", "\u200d", 4, 5, CompareOptions.IgnoreCase, -1 };

            // Ignore symbols
            yield return new object[] { "More Test's", "Tests", 10, 11, CompareOptions.IgnoreSymbols, -1 };
            yield return new object[] { "More Test's", "Tests", 10, 11, CompareOptions.None, -1 };
            yield return new object[] { "cbabababdbaba", "ab", 12, 13, CompareOptions.None, 10 };

            // Platform differences
            yield return new object[] { "foobardzsdzs", "rddzs", 11, 12, CompareOptions.None, -1 };
        }

        public static IEnumerable<object[]> IsPrefix_TestData()
        {
            // Empty strings
            yield return new object[] { "foo", "", CompareOptions.None, true };
            yield return new object[] { "", "", CompareOptions.None, true };

            // Early exit for empty values before 'options' is validated
            yield return new object[] { "hello", "", (CompareOptions)(-1), true };

            // Long strings
            yield return new object[] { new string('a', 5555), "aaaaaaaaaaaaaaa", CompareOptions.None, true };
            yield return new object[] { new string('a', 5555), new string('a', 5000), CompareOptions.None, true };
            yield return new object[] { new string('a', 5555), new string('a', 5000) + "b", CompareOptions.None, false };

            // Hungarian
            yield return new object[] { "dzsdzsfoobar", "ddzsf", CompareOptions.None, false };
            yield return new object[] { "dzsdzsfoobar", "ddzsf", CompareOptions.Ordinal, false };
            yield return new object[] { "dzsdzsfoobar", "ddzsf", CompareOptions.Ordinal, false };

            // Turkish
            yield return new object[] { "interesting", "I", CompareOptions.None, false };
            yield return new object[] { "interesting", "I", CompareOptions.IgnoreCase, true };
            yield return new object[] { "interesting", "\u0130", CompareOptions.None, false };
            yield return new object[] { "interesting", "\u0130", CompareOptions.IgnoreCase, false };

            // Unicode
            yield return new object[] { "\u00C0nimal", "A\u0300", CompareOptions.None, false };
            yield return new object[] { "\u00C0nimal", "A\u0300", CompareOptions.Ordinal, false };
            yield return new object[] { "\u00C0nimal", "a\u0300", CompareOptions.IgnoreCase, false };
            yield return new object[] { "\u00C0nimal", "a\u0300", CompareOptions.OrdinalIgnoreCase, false };
            yield return new object[] { "FooBar", "Foo\u0400Bar", CompareOptions.Ordinal, false };
            yield return new object[] { "FooBA\u0300R", "FooB\u00C0R", CompareOptions.IgnoreNonSpace, false };

            yield return new object[] { "\u00D3\u00D4\u00D3\u00D4Hello", "\u00F3\u00F4", CompareOptions.OrdinalIgnoreCase, true };
            yield return new object[] { "\u00D3\u00D4Hello\u00D3\u00D4", "\u00F3\u00F5", CompareOptions.OrdinalIgnoreCase, false };
            yield return new object[] { "\U00010400\U00010400Hello", "\U00010428", CompareOptions.OrdinalIgnoreCase, true };

            // Ignore symbols
            yield return new object[] { "Test's can be interesting", "Tests", CompareOptions.IgnoreSymbols, false };
            yield return new object[] { "Test's can be interesting", "Tests", CompareOptions.None, false };

            // Platform differences
            yield return new object[] { "dzsdzsfoobar", "ddzsf", CompareOptions.None, false };
        }

        public static IEnumerable<object[]> IsSuffix_TestData()
        {
            // Empty strings
            yield return new object[] { "foo", "", CompareOptions.None, true };
            yield return new object[] { "", "", CompareOptions.None, true };

            // Early exit for empty values before 'options' is validated
            yield return new object[] { "hello", "", (CompareOptions)(-1), true };

            // Long strings
            yield return new object[] { new string('a', 5555), "aaaaaaaaaaaaaaa", CompareOptions.None, true };
            yield return new object[] { new string('a', 5555), new string('a', 5000), CompareOptions.None, true };
            yield return new object[] { new string('a', 5555), new string('a', 5000) + "b", CompareOptions.None, false };

            // Hungarian
            yield return new object[] { "foobardzsdzs", "rddzs", CompareOptions.Ordinal, false };
            yield return new object[] { "foobardzsdzs", "rddzs", CompareOptions.None, false };

            // Turkish
            yield return new object[] { "Hi", "I", CompareOptions.None, false };
            yield return new object[] { "Hi", "I", CompareOptions.IgnoreCase, true };
            yield return new object[] { "Hi", "\u0130", CompareOptions.None, false };
            yield return new object[] { "Hi", "\u0130", CompareOptions.IgnoreCase, false };

            // Unicode
            yield return new object[] { "Exhibit \u00C0", "A\u0300", CompareOptions.None, false };
            yield return new object[] { "Exhibit \u00C0", "A\u0300", CompareOptions.Ordinal, false };
            yield return new object[] { "Exhibit \u00C0", "a\u0300", CompareOptions.IgnoreCase, false };
            yield return new object[] { "Exhibit \u00C0", "a\u0300", CompareOptions.OrdinalIgnoreCase, false };
            yield return new object[] { "FooBar", "Foo\u0400Bar", CompareOptions.Ordinal, false };
            yield return new object[] { "FooBA\u0300R", "FooB\u00C0R", CompareOptions.IgnoreNonSpace, false };

            yield return new object[] { "\u00D3\u00D4\u00D3\u00D4Hello", "\u00F3\u00F4", CompareOptions.OrdinalIgnoreCase, false };
            yield return new object[] { "\u00D3\u00D4Hello\u00D3\u00D4", "\u00F3\u00F4", CompareOptions.OrdinalIgnoreCase, true };
            yield return new object[] { "\U00010400\U00010400Hello", "\U00010428", CompareOptions.OrdinalIgnoreCase, false };
            yield return new object[] { "Hello\U00010400", "\U00010428", CompareOptions.OrdinalIgnoreCase, true };

            // Weightless characters
            yield return new object[] { "", "\u200d", CompareOptions.None, false };
            yield return new object[] { "", "\u200d", CompareOptions.IgnoreCase, false };

            // Ignore symbols
            yield return new object[] { "More Test's", "Tests", CompareOptions.IgnoreSymbols, false };
            yield return new object[] { "More Test's", "Tests", CompareOptions.None, false };

            // Platform differences
            yield return new object[] { "foobardzsdzs", "rddzs", CompareOptions.None, false };
        }

        public static IEnumerable<object[]> Compare_TestData()
        {
            CompareOptions ignoreKanaIgnoreWidthIgnoreCase = CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase;
            yield return new object[] { "\u3042", "\u30A2", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { "\u3042", "\uFF71", ignoreKanaIgnoreWidthIgnoreCase, -1 };

            yield return new object[] { "\u304D\u3083", "\u30AD\u30E3", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { "\u304D\u3083", "\u30AD\u3083", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { "\u304D \u3083", "\u30AD\u3083", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { "\u3044", "I", ignoreKanaIgnoreWidthIgnoreCase, 1 };

            yield return new object[] { "a", "A", ignoreKanaIgnoreWidthIgnoreCase, 0 };
            yield return new object[] { "a", "\uFF41", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { "ABCDE", "\uFF21\uFF22\uFF23\uFF24\uFF25", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { "ABCDE", "\uFF21\uFF22\uFF23D\uFF25", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { "ABCDE", "a\uFF22\uFF23D\uFF25", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { "ABCDE", "\uFF41\uFF42\uFF23D\uFF25", ignoreKanaIgnoreWidthIgnoreCase, -1 };

            yield return new object[] { "\u6FA4", "\u6CA2", ignoreKanaIgnoreWidthIgnoreCase, 1 };

            yield return new object[] { "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u30D6\u30D9\u30DC", ignoreKanaIgnoreWidthIgnoreCase, -1 };

            yield return new object[] { "ABDDE", "D", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { "ABCDE", "\uFF43D", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { "ABCDE", "c", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { "\u3060", "\u305F", ignoreKanaIgnoreWidthIgnoreCase, 1 };
            yield return new object[] { "\u3060", "\uFF80\uFF9E", ignoreKanaIgnoreWidthIgnoreCase, -1 };
            yield return new object[] { "\u3060", "\u30C0", ignoreKanaIgnoreWidthIgnoreCase, -1 };

            yield return new object[] { "\u3042", "\u30A1", CompareOptions.None, -1 };

            yield return new object[] { "\u304D \u3083", "\u30AD\u3083", CompareOptions.None, -1 };
            yield return new object[] { "\u3044", "I", CompareOptions.None, 1 };
            yield return new object[] { "a", "A", CompareOptions.None, 1 };
            yield return new object[] { "a", "\uFF41", CompareOptions.None, -1 };

            yield return new object[] { "", "'", CompareOptions.None, -1 };

            yield return new object[] { "\u00D3\u00D4", "\u00F3\u00F4", CompareOptions.OrdinalIgnoreCase, 0 };
            yield return new object[] { "\U00010400", "\U00010428", CompareOptions.OrdinalIgnoreCase, 0 };
            yield return new object[] { "\u00D3\u00D4", "\u00F3\u00F4", CompareOptions.IgnoreCase, 0 };
            yield return new object[] { "\U00010400", "\U00010428", CompareOptions.IgnoreCase, 0 };

            yield return new object[] { "\u00D3\u00D4G", "\u00F3\u00F4", CompareOptions.OrdinalIgnoreCase, 1 };
            yield return new object[] { "\U00010400G", "\U00010428", CompareOptions.OrdinalIgnoreCase, 1 };
            yield return new object[] { "\u00D3\u00D4G", "\u00F3\u00F4", CompareOptions.IgnoreCase, 1 };
            yield return new object[] { "\U00010400G", "\U00010428", CompareOptions.IgnoreCase, 1 };

            yield return new object[] { "\u00D3\u00D4", "\u00F3\u00F4G", CompareOptions.OrdinalIgnoreCase, -1 };
            yield return new object[] { "\U00010400", "\U00010428G", CompareOptions.OrdinalIgnoreCase, -1 };
            yield return new object[] { "\u00D3\u00D4", "\u00F3\u00F4G", CompareOptions.IgnoreCase, -1 };
            yield return new object[] { "\U00010400", "\U00010428G", CompareOptions.IgnoreCase, -1 };

            // Hungarian
            yield return new object[] { "dzsdzs", "ddzs", CompareOptions.Ordinal, 1 };
            yield return new object[] { "dzsdzs", "ddzs", CompareOptions.None, 1 };

            // Turkish
            yield return new object[] { "i", "I", CompareOptions.None, 1 };
            yield return new object[] { "i", "I", CompareOptions.IgnoreCase, 0 };
            yield return new object[] { "i", "\u0130", CompareOptions.None, -1 };
            yield return new object[] { "i", "\u0130", CompareOptions.IgnoreCase, -1 };

            yield return new object[] { "\u00C0", "A\u0300", CompareOptions.None, 1 };
            yield return new object[] { "\u00C0", "A\u0300", CompareOptions.Ordinal, 1 };
            yield return new object[] { "FooBar", "Foo\u0400Bar", CompareOptions.Ordinal, -1 };
            yield return new object[] { "FooBA\u0300R", "FooB\u00C0R", CompareOptions.IgnoreNonSpace, -1 };

            yield return new object[] { "Test's", "Tests", CompareOptions.IgnoreSymbols, -1 };
            yield return new object[] { "Test's", "Tests", CompareOptions.StringSort, -1 };

            // Spanish
            yield return new object[] { "llegar", "lugar", CompareOptions.None, -1 };

            yield return new object[] { "\u3042", "\u30A1", CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase, -1 };

            // Surrogates

            yield return new object[] { "Hello\uFE6A", "Hello\U0001F601", CompareOptions.IgnoreCase, -1 };
            yield return new object[] { "Hello\U0001F601", "Hello\uFE6A", CompareOptions.IgnoreCase,  1 };
            yield return new object[] { "\uDBFF", "\uD800\uDC00", CompareOptions.IgnoreCase,  -1 };
            yield return new object[] { "\uD800\uDC00", "\uDBFF", CompareOptions.IgnoreCase,   1 };
            yield return new object[] { "abcdefg\uDBFF", "abcdefg\uD800\uDC00", CompareOptions.IgnoreCase,  -1 };
        }

        public static IEnumerable<object[]> ToLower_TestData()
        {
            yield return new object[] { "", "", true };

            yield return new object[] { "A", "a", true };
            yield return new object[] { "a", "a", true };
            yield return new object[] { "ABC", "abc", true };
            yield return new object[] { "abc", "abc", true };

            yield return new object[] { "1", "1", true };
            yield return new object[] { "123", "123", true };
            yield return new object[] { "!", "!", true };

            yield return new object[] { "HELLOWOR!LD123", "hellowor!ld123", true };
            yield return new object[] { "HelloWor!ld123", "hellowor!ld123", true };
            yield return new object[] { "Hello\n\0World\u0009!", "hello\n\0world\t!", true };

            yield return new object[] { "THIS IS A LONGER TEST CASE", "this is a longer test case", true };
            yield return new object[] { "this Is A LONGER mIXEd casE test case", "this is a longer mixed case test case", true };

            yield return new object[] { "THIS \t hAs \t SOMe \t tabs", "this \t has \t some \t tabs", true };
            yield return new object[] { "EMBEDDED\0NuLL\0Byte\0", "embedded\0null\0byte\0", true };

            // LATIN CAPITAL LETTER O WITH ACUTE, which has a lower case variant.
            yield return new object[] { "\u00D3", "\u00F3", true };

            // SNOWMAN, which does not have a lower case variant.
            yield return new object[] { "\u2603", "\u2603", true };

            // RAINBOW (outside the BMP and does not case)
            yield return new object[] { "\U0001F308", "\U0001F308", true };

            // Surrogate casing
            yield return new object[] { "\U00010400", "\U00010428", true };

            // Unicode defines some codepoints which expand into multiple codepoints
            // when cased (see SpecialCasing.txt from UNIDATA for some examples). We have never done
            // these sorts of expansions, since it would cause string lengths to change when cased,
            // which is non-intuitive. In addition, there are some context sensitive mappings which
            // we also don't preform.
            // Greek Capital Letter Sigma (does not to case to U+03C2 with "final sigma" rule).
            yield return new object[] { "\u03A3", "\u03C3", true };
        }

        public static IEnumerable<object[]> ToUpper_TestData()
        {
            yield return new object[] { "", "" , true};

            yield return new object[] { "a", "A", true };
            yield return new object[] { "abc", "ABC", true };
            yield return new object[] { "A", "A", true };
            yield return new object[] { "ABC", "ABC", true };

            yield return new object[] { "1", "1", true };
            yield return new object[] { "123", "123", true };
            yield return new object[] { "!", "!", true };

            yield return new object[] { "HelloWor!ld123", "HELLOWOR!LD123", true };
            yield return new object[] { "HELLOWOR!LD123", "HELLOWOR!LD123", true };
            yield return new object[] { "Hello\n\0World\u0009!", "HELLO\n\0WORLD\t!", true };

            yield return new object[] { "this is a longer test case", "THIS IS A LONGER TEST CASE", true };
            yield return new object[] { "this Is A LONGER mIXEd casE test case", "THIS IS A LONGER MIXED CASE TEST CASE", true };
            yield return new object[] { "this \t HaS \t somE \t TABS", "THIS \t HAS \t SOME \t TABS", true };

            yield return new object[] { "embedded\0NuLL\0Byte\0", "EMBEDDED\0NULL\0BYTE\0", true };

            // LATIN SMALL LETTER O WITH ACUTE, mapped to LATIN CAPITAL LETTER O WITH ACUTE.
            yield return new object[] { "\u00F3", "\u00D3", true };

            // SNOWMAN, which does not have an upper case variant.
            yield return new object[] { "\u2603", "\u2603", true };

            // RAINBOW (outside the BMP and does not case)
            yield return new object[] { "\U0001F308", "\U0001F308", true };

            // Surrogate casing
            yield return new object[] { "\U00010428", "\U00010400", true };

            // Unicode defines some codepoints which expand into multiple codepoints
            // when cased (see SpecialCasing.txt from UNIDATA for some examples). We have never done
            // these sorts of expansions, since it would cause string lengths to change when cased,
            // which is non-intuitive. In addition, there are some context sensitive mappings which
            // we also don't preform.
            // es-zed does not case to SS when uppercased.
            yield return new object[] { "\u00DF", "\u00DF", true };

            // Ligatures do not expand when cased.
            yield return new object[] { "\uFB00", "\uFB00", true };

            // Precomposed character with no uppercase variant, we don't want to "decompose" this
            // as part of casing.
            yield return new object[] { "\u0149", "\u0149", true };

            yield return new object[] { "\u03C3", "\u03A3", true };
        }

        public static IEnumerable<object[]> GetAscii_TestData()
        {
            yield return new object[] { "\u0101", 0, 1, "xn--yda" };
            yield return new object[] { "\u0101\u0061\u0041", 0, 3, "xn--aa-cla" };
            yield return new object[] { "\u0061\u0101\u0062", 0, 3, "xn--ab-dla" };
            yield return new object[] { "\u0061\u0062\u0101", 0, 3, "xn--ab-ela" };

            yield return new object[] { "\uD800\uDF00\uD800\uDF01\uD800\uDF02", 0, 6, "xn--097ccd" }; // Surrogate pairs
            yield return new object[] { "\uD800\uDF00\u0061\uD800\uDF01\u0042\uD800\uDF02", 0, 8, "xn--ab-ic6nfag" }; // Surrogate pairs separated by ASCII
            yield return new object[] { "\uD800\uDF00\u0101\uD800\uDF01\u305D\uD800\uDF02", 0, 8, "xn--yda263v6b6kfag" }; // Surrogate pairs separated by non-ASCII
            yield return new object[] { "\uD800\uDF00\u0101\uD800\uDF01\u0061\uD800\uDF02", 0, 8, "xn--a-nha4529qfag" }; // Surrogate pairs separated by ASCII and non-ASCII
            yield return new object[] { "\u0061\u0062\u0063", 0, 3, "\u0061\u0062\u0063" }; // ASCII only code points
            yield return new object[] { "\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067", 0, 7, "xn--d9juau41awczczp" }; // Non-ASCII only code points
            yield return new object[] { "\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 0, 9, "xn--de-jg4avhby1noc0d" }; // ASCII and non-ASCII code points
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 0, 21, "abc.xn--d9juau41awczczp.xn--de-jg4avhby1noc0d" }; // Fully qualified domain name

            // Embedded domain name conversion (NLS + only)(Priority 1)
            // Per the spec [7], "The index and count parameters (when provided) allow the
            // conversion to be done on a larger string where the domain name is embedded
            // (such as a URI or IRI). The output string is only the converted FQDN or
            // label, not the whole input string (if the input string contains more
            // character than the substring to convert)."
            // Fully Qualified Domain Name (Label1.Label2.Label3)
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 0, 21, "abc.xn--d9juau41awczczp.xn--de-jg4avhby1noc0d" };
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 0, 11, "abc.xn--d9juau41awczczp" };
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 0, 12, "abc.xn--d9juau41awczczp." };
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 4, 17, "xn--d9juau41awczczp.xn--de-jg4avhby1noc0d" };
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 4, 7, "xn--d9juau41awczczp" };
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 4, 8, "xn--d9juau41awczczp." };
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 12, 9, "xn--de-jg4avhby1noc0d" };
        }

        public static IEnumerable<object[]> GetUnicode_TestData()
        {
            yield return new object[] { "xn--yda", 0, 7, "\u0101" };
            yield return new object[] { "axn--ydab", 1, 7, "\u0101" };

            yield return new object[] { "xn--aa-cla", 0, 10, "\u0101\u0061a" };
            yield return new object[] { "xn--ab-dla", 0, 10, "\u0061\u0101\u0062" };
            yield return new object[] { "xn--ab-ela", 0, 10, "\u0061\u0062\u0101"  };

            yield return new object[] { "xn--097ccd", 0, 10, "\uD800\uDF00\uD800\uDF01\uD800\uDF02" }; // Surrogate pairs
            yield return new object[] { "xn--ab-ic6nfag", 0, 14, "\uD800\uDF00\u0061\uD800\uDF01b\uD800\uDF02" }; // Surrogate pairs separated by ASCII
            yield return new object[] { "xn--yda263v6b6kfag", 0, 18, "\uD800\uDF00\u0101\uD800\uDF01\u305D\uD800\uDF02" }; // Surrogate pairs separated by non-ASCII
            yield return new object[] { "xn--a-nha4529qfag", 0, 17, "\uD800\uDF00\u0101\uD800\uDF01\u0061\uD800\uDF02" }; // Surrogate pairs separated by ASCII and non-ASCII
            yield return new object[] { "\u0061\u0062\u0063", 0, 3, "\u0061\u0062\u0063" }; // ASCII only code points
            yield return new object[] { "xn--d9juau41awczczp", 0, 19, "\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067" }; // Non-ASCII only code points
            yield return new object[] { "xn--de-jg4avhby1noc0d", 0, 21, "\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0" }; // ASCII and non-ASCII code points
            yield return new object[] { "abc.xn--d9juau41awczczp.xn--de-jg4avhby1noc0d", 0, 45, "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0" }; // Fully qualified domain name

            // Embedded domain name conversion (NLS + only)(Priority 1)
            // Per the spec [7], "The index and count parameters (when provided) allow the
            // conversion to be done on a larger string where the domain name is embedded
            // (such as a URI or IRI). The output string is only the converted FQDN or
            // label, not the whole input string (if the input string contains more
            // character than the substring to convert)."
            // Fully Qualified Domain Name (Label1.Label2.Label3)
            yield return new object[] { "abc.xn--d9juau41awczczp.xn--de-jg4avhby1noc0d", 0, 45, "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0" };
            yield return new object[] { "abc.xn--d9juau41awczczp", 0, 23, "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067" };
            yield return new object[] { "abc.xn--d9juau41awczczp.", 0, 24, "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067." };
            yield return new object[] { "xn--d9juau41awczczp.xn--de-jg4avhby1noc0d", 0, 41, "\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0" };
            yield return new object[] { "xn--d9juau41awczczp", 0, 19, "\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067" };
            yield return new object[] { "xn--d9juau41awczczp.", 0, 20, "\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067." };
            yield return new object[] { "xn--de-jg4avhby1noc0d", 0, 21, "\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0" };
        }

        [ConditionalFact(nameof(PredefinedCulturesOnlyIsDisabled))]
        public static void IcuShouldNotBeLoaded()
        {
            Assert.False(PlatformDetection.IsIcuGlobalization);
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [MemberData(nameof(Cultures_TestData))]
        public void TestCultureData(string cultureName)
        {
            CultureInfo ci = new CultureInfo(cultureName);

            //
            // DateTimeInfo
            //

            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedDayNames, ci.DateTimeFormat.AbbreviatedDayNames);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthGenitiveNames, ci.DateTimeFormat.AbbreviatedMonthGenitiveNames);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames, ci.DateTimeFormat.AbbreviatedMonthNames);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.AMDesignator, ci.DateTimeFormat.AMDesignator);
            Assert.True(ci.DateTimeFormat.Calendar is GregorianCalendar);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.CalendarWeekRule, ci.DateTimeFormat.CalendarWeekRule);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.DateSeparator, ci.DateTimeFormat.DateSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.DayNames, ci.DateTimeFormat.DayNames);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.FirstDayOfWeek, ci.DateTimeFormat.FirstDayOfWeek);

            for (DayOfWeek dow = DayOfWeek.Sunday; dow < DayOfWeek.Saturday; dow++)
                Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedDayName(dow), ci.DateTimeFormat.GetAbbreviatedDayName(dow));
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedEraName(1), ci.DateTimeFormat.GetAbbreviatedEraName(1));

            for (int i = 1; i <= 12; i++)
                Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(i), ci.DateTimeFormat.GetAbbreviatedMonthName(i));

            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.GetAllDateTimePatterns(), ci.DateTimeFormat.GetAllDateTimePatterns());

            for (DayOfWeek dow = DayOfWeek.Sunday; dow < DayOfWeek.Saturday; dow++)
                Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.GetDayName(dow), ci.DateTimeFormat.GetDayName(dow));

            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.GetEra(CultureInfo.InvariantCulture.DateTimeFormat.GetEraName(1)), ci.DateTimeFormat.GetEra(ci.DateTimeFormat.GetEraName(1)));

            for (int i = 1; i <= 12; i++)
                Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(i), ci.DateTimeFormat.GetMonthName(i));
            for (DayOfWeek dow = DayOfWeek.Sunday; dow < DayOfWeek.Saturday; dow++)
                Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.GetShortestDayName(dow), ci.DateTimeFormat.GetShortestDayName(dow));

            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.LongDatePattern, ci.DateTimeFormat.LongDatePattern);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.LongTimePattern, ci.DateTimeFormat.LongTimePattern);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.MonthDayPattern, ci.DateTimeFormat.MonthDayPattern);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.MonthGenitiveNames, ci.DateTimeFormat.MonthGenitiveNames);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.MonthNames, ci.DateTimeFormat.MonthNames);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.NativeCalendarName, ci.DateTimeFormat.NativeCalendarName);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.PMDesignator, ci.DateTimeFormat.PMDesignator);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.RFC1123Pattern, ci.DateTimeFormat.RFC1123Pattern);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.ShortDatePattern, ci.DateTimeFormat.ShortDatePattern);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.ShortestDayNames, ci.DateTimeFormat.ShortestDayNames);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.ShortTimePattern, ci.DateTimeFormat.ShortTimePattern);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.TimeSeparator, ci.DateTimeFormat.TimeSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.YearMonthPattern, ci.DateTimeFormat.YearMonthPattern);

            //
            // Culture data
            //

            Assert.True(ci.Calendar is GregorianCalendar);

            CultureTypes ct = ci.Name == "" ? CultureInfo.InvariantCulture.CultureTypes : CultureInfo.InvariantCulture.CultureTypes | CultureTypes.UserCustomCulture;
            Assert.Equal(ct, ci.CultureTypes);
            Assert.Equal(CultureInfo.InvariantCulture.NativeName, ci.DisplayName);
            Assert.Equal(CultureInfo.InvariantCulture.EnglishName, ci.EnglishName);
            Assert.Equal(CultureInfo.InvariantCulture.GetConsoleFallbackUICulture(), ci.GetConsoleFallbackUICulture());
            Assert.Equal(cultureName, ci.IetfLanguageTag);
            Assert.Equal(CultureInfo.InvariantCulture.IsNeutralCulture, ci.IsNeutralCulture);
            Assert.Equal(CultureInfo.InvariantCulture.KeyboardLayoutId, ci.KeyboardLayoutId);
            Assert.Equal(ci.Name == "" ? 0x7F : 0x1000, ci.LCID);
            Assert.Equal(cultureName, ci.Name);
            Assert.Equal(CultureInfo.InvariantCulture.NativeName, ci.NativeName);
            Assert.Equal(1, ci.OptionalCalendars.Length);
            Assert.True(ci.OptionalCalendars[0] is GregorianCalendar);
            Assert.Equal(CultureInfo.InvariantCulture.Parent, ci.Parent);
            Assert.Equal(CultureInfo.InvariantCulture.ThreeLetterISOLanguageName, ci.ThreeLetterISOLanguageName);
            Assert.Equal(CultureInfo.InvariantCulture.ThreeLetterWindowsLanguageName, ci.ThreeLetterWindowsLanguageName);
            Assert.Equal(CultureInfo.InvariantCulture.TwoLetterISOLanguageName, ci.TwoLetterISOLanguageName);
            Assert.Equal(ci.Name == "" ? false : true, ci.UseUserOverride);

            //
            // Culture Creations
            //
            Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.CurrentCulture);
            Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.CurrentUICulture);
            Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.InstalledUICulture);
            Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.CreateSpecificCulture("en"));
            Assert.Equal(ci, CultureInfo.GetCultureInfo(cultureName).Clone());
            Assert.Equal(ci, CultureInfo.GetCultureInfoByIetfLanguageTag(cultureName));

            //
            // NumberFormatInfo
            //

            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.CurrencyDecimalDigits, ci.NumberFormat.CurrencyDecimalDigits);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.CurrencyDecimalSeparator, ci.NumberFormat.CurrencyDecimalSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.CurrencyGroupSeparator, ci.NumberFormat.CurrencyGroupSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.CurrencyGroupSizes, ci.NumberFormat.CurrencyGroupSizes);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.CurrencyNegativePattern, ci.NumberFormat.CurrencyNegativePattern);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.CurrencyPositivePattern, ci.NumberFormat.CurrencyPositivePattern);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.CurrencySymbol, ci.NumberFormat.CurrencySymbol);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.DigitSubstitution, ci.NumberFormat.DigitSubstitution);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NaNSymbol, ci.NumberFormat.NaNSymbol);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NativeDigits, ci.NumberFormat.NativeDigits);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NegativeInfinitySymbol, ci.NumberFormat.NegativeInfinitySymbol);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NegativeSign, ci.NumberFormat.NegativeSign);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NumberDecimalDigits, ci.NumberFormat.NumberDecimalDigits);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator, ci.NumberFormat.NumberDecimalSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NumberGroupSeparator, ci.NumberFormat.NumberGroupSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NumberGroupSizes, ci.NumberFormat.NumberGroupSizes);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NumberNegativePattern, ci.NumberFormat.NumberNegativePattern);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PercentDecimalDigits, ci.NumberFormat.PercentDecimalDigits);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PercentDecimalSeparator, ci.NumberFormat.PercentDecimalSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PercentGroupSeparator, ci.NumberFormat.PercentGroupSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PercentGroupSizes, ci.NumberFormat.PercentGroupSizes);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PercentNegativePattern, ci.NumberFormat.PercentNegativePattern);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PercentPositivePattern, ci.NumberFormat.PercentPositivePattern);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PercentSymbol, ci.NumberFormat.PercentSymbol);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PerMilleSymbol, ci.NumberFormat.PerMilleSymbol);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PositiveInfinitySymbol, ci.NumberFormat.PositiveInfinitySymbol);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PositiveSign, ci.NumberFormat.PositiveSign);

            //
            // TextInfo
            //

            Assert.Equal(CultureInfo.InvariantCulture.TextInfo.ANSICodePage, ci.TextInfo.ANSICodePage);
            Assert.Equal(cultureName, ci.TextInfo.CultureName);
            Assert.Equal(CultureInfo.InvariantCulture.TextInfo.EBCDICCodePage, ci.TextInfo.EBCDICCodePage);
            Assert.Equal(CultureInfo.InvariantCulture.TextInfo.IsRightToLeft, ci.TextInfo.IsRightToLeft);
            Assert.Equal(ci.Name == "" ? 0x7F : 0x1000, ci.TextInfo.LCID);
            Assert.Equal(CultureInfo.InvariantCulture.TextInfo.ListSeparator, ci.TextInfo.ListSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.TextInfo.MacCodePage, ci.TextInfo.MacCodePage);
            Assert.Equal(CultureInfo.InvariantCulture.TextInfo.OEMCodePage, ci.TextInfo.OEMCodePage);

            //
            // CompareInfo
            //
            Assert.Equal(ci.Name == "" ? 0x7F : 0x1000, ci.CompareInfo.LCID);
            Assert.True(cultureName.Equals(ci.CompareInfo.Name, StringComparison.OrdinalIgnoreCase));
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [MemberData(nameof(Cultures_TestData))]
        public void SetCultureData(string cultureName)
        {
            CultureInfo ci = new CultureInfo(cultureName);

            //
            // DateTimeInfo
            //
            var calendar = new GregorianCalendar();
            ci.DateTimeFormat.Calendar = calendar;
            Assert.Equal(calendar, ci.DateTimeFormat.Calendar);

            Assert.Throws<ArgumentOutOfRangeException>(() => ci.DateTimeFormat.Calendar = new TaiwanCalendar());
        }

        [ConditionalFact(nameof(PredefinedCulturesOnlyIsDisabled))]
        public void TestEnum()
        {
            Assert.Equal(new CultureInfo[1] { CultureInfo.InvariantCulture }, CultureInfo.GetCultures(CultureTypes.AllCultures));
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [MemberData(nameof(Cultures_TestData))]
        public void TestSortVersion(string cultureName)
        {
            SortVersion version = new SortVersion(0, new Guid(0, 0, 0, 0, 0, 0, 0,
                                                            (byte)(0x7F >> 24),
                                                            (byte)((0x7F & 0x00FF0000) >> 16),
                                                            (byte)((0x7F & 0x0000FF00) >> 8),
                                                            (byte)(0x7F & 0xFF)));
            Assert.Equal(version, new CultureInfo(cultureName).CompareInfo.Version);
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [InlineData(0, 0)]
        [InlineData(1, 2)]
        [InlineData(100_000, 200_000)]
        [InlineData(0x3FFF_FFFF, 0x7FFF_FFFE)]
        public void TestGetSortKeyLength_Valid(int inputLength, int expectedSortKeyLength)
        {
            using BoundedMemory<char> boundedMemory = BoundedMemory.Allocate<char>(0); // AV if dereferenced
            boundedMemory.MakeReadonly();
            ReadOnlySpan<char> dummySpan = MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference(boundedMemory.Span), inputLength);
            Assert.Equal(expectedSortKeyLength, CultureInfo.InvariantCulture.CompareInfo.GetSortKeyLength(dummySpan));
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [InlineData(0x4000_0000)]
        [InlineData(int.MaxValue)]
        public unsafe void TestGetSortKeyLength_OverlongArgument(int inputLength)
        {
            using BoundedMemory<char> boundedMemory = BoundedMemory.Allocate<char>(0); // AV if dereferenced
            boundedMemory.MakeReadonly();

            Assert.Throws<ArgumentException>("source", () =>
            {
                ReadOnlySpan<char> dummySpan = MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference(boundedMemory.Span), inputLength);
                CultureInfo.InvariantCulture.CompareInfo.GetSortKeyLength(dummySpan);
            });
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [InlineData("Hello", CompareOptions.None, "Hello")]
        [InlineData("Hello", CompareOptions.IgnoreWidth, "Hello")]
        [InlineData("Hello", CompareOptions.IgnoreCase, "HELLO")]
        [InlineData("Hello", CompareOptions.IgnoreCase | CompareOptions.IgnoreWidth, "HELLO")]
        [InlineData("Hell\u00F6", CompareOptions.None, "Hell\u00F6")] // U+00F6 = LATIN SMALL LETTER O WITH DIAERESIS
        [InlineData("Hell\u00F6", CompareOptions.IgnoreCase, "HELL\u00D6")]
        public unsafe void TestSortKey_FromSpan(string input, CompareOptions options, string expected)
        {
            byte[] expectedOutputBytes = GetExpectedInvariantOrdinalSortKey(expected);

            CompareInfo compareInfo = CultureInfo.InvariantCulture.CompareInfo;

            // First, validate that too short a buffer throws

            Assert.Throws<ArgumentException>("destination", () => compareInfo.GetSortKey(input, new byte[expectedOutputBytes.Length - 1], options));

            // Next, validate that using a properly-sized buffer succeeds
            // We'll use BoundedMemory to check for buffer overruns

            using BoundedMemory<char> boundedInputMemory = BoundedMemory.AllocateFromExistingData<char>(input);
            boundedInputMemory.MakeReadonly();
            ReadOnlySpan<char> boundedInputSpan = boundedInputMemory.Span;

            using BoundedMemory<byte> boundedOutputMemory = BoundedMemory.Allocate<byte>(expectedOutputBytes.Length);
            Span<byte> boundedOutputSpan = boundedOutputMemory.Span;

            Assert.Equal(expectedOutputBytes.Length, compareInfo.GetSortKey(boundedInputSpan, boundedOutputSpan, options));
            Assert.Equal(expectedOutputBytes, boundedOutputSpan[0..expectedOutputBytes.Length].ToArray());

            // Now try it once more, passing a larger span where the last byte points to unallocated memory.
            // If GetSortKey attempts to write beyond the number of bytes we expect, the unit test will AV.

            boundedOutputSpan.Clear();

            fixed (byte* pBoundedOutputSpan = boundedOutputSpan)
            {
                boundedOutputSpan = new Span<byte>(pBoundedOutputSpan, boundedOutputSpan.Length + 1); // last byte is unallocated memory
                Assert.Equal(expectedOutputBytes.Length, compareInfo.GetSortKey(boundedInputSpan, boundedOutputSpan, options));
                Assert.Equal(expectedOutputBytes, boundedOutputSpan[0..expectedOutputBytes.Length].ToArray());
            }
        }

        [ConditionalFact(nameof(PredefinedCulturesOnlyIsDisabled))]
        public void TestSortKey_ZeroWeightCodePoints()
        {
            // In the invariant globalization mode, there's no such thing as a zero-weight code point,
            // so the U+200C ZERO WIDTH NON-JOINER code point contributes to the final sort key value.

            CompareInfo compareInfo = CultureInfo.InvariantCulture.CompareInfo;
            SortKey sortKeyForEmptyString = compareInfo.GetSortKey("");
            SortKey sortKeyForZeroWidthJoiner = compareInfo.GetSortKey("\u200c");
            Assert.NotEqual(0, SortKey.Compare(sortKeyForEmptyString, sortKeyForZeroWidthJoiner));
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [InlineData("", "", 0)]
        [InlineData("", "not-empty", -1)]
        [InlineData("not-empty", "", 1)]
        [InlineData("hello", "hello", 0)]
        [InlineData("prefix", "prefix-with-more-data", -1)]
        [InlineData("prefix-with-more-data", "prefix", 1)]
        [InlineData("e", "\u0115", -1)] // U+0115 = LATIN SMALL LETTER E WITH BREVE, tests endianness handling
        public void TestSortKey_Compare_And_Equals(string value1, string value2, int expectedSign)
        {
            // These tests are in the "invariant" unit test project because we rely on Invariant mode
            // copying the input data directly into the sort key.

            SortKey sortKey1 = CultureInfo.InvariantCulture.CompareInfo.GetSortKey(value1);
            SortKey sortKey2 = CultureInfo.InvariantCulture.CompareInfo.GetSortKey(value2);

            Assert.Equal(expectedSign, Math.Sign(SortKey.Compare(sortKey1, sortKey2)));
            Assert.Equal(expectedSign == 0, sortKey1.Equals(sortKey2));
        }

        private static StringComparison GetStringComparison(CompareOptions options)
        {
            StringComparison sc = (StringComparison) 0;

            if ((options & CompareOptions.IgnoreCase) != 0)
                sc |= StringComparison.CurrentCultureIgnoreCase;

            if ((options & CompareOptions.Ordinal) != 0)
                sc |= StringComparison.Ordinal;

            if ((options & CompareOptions.OrdinalIgnoreCase) != 0)
                sc |= StringComparison.OrdinalIgnoreCase;

            if (sc == (StringComparison)0)
            {
                sc = StringComparison.CurrentCulture;
            }

            return sc;
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [MemberData(nameof(IndexOf_TestData))]
        public void TestIndexOf(string source, string value, int startIndex, int count, CompareOptions options, int result)
        {
            foreach (string cul in s_cultureNames)
            {
                CompareInfo compareInfo = CultureInfo.GetCultureInfo(cul).CompareInfo;
                TestCore(compareInfo, source, value, startIndex, count, options, result);
            }

            // static test helper method to avoid mutating input args when called in a loop
            static void TestCore(CompareInfo compareInfo, string source, string value, int startIndex, int count, CompareOptions options, int result)
            {
                Assert.Equal(result, compareInfo.IndexOf(source, value, startIndex, count, options));
                Assert.Equal(result, source.IndexOf(value, startIndex, count, GetStringComparison(options)));

                // Span versions - using BoundedMemory to check for buffer overruns

                using BoundedMemory<char> sourceBoundedMemory = BoundedMemory.AllocateFromExistingData<char>(source.AsSpan(startIndex, count));
                sourceBoundedMemory.MakeReadonly();
                ReadOnlySpan<char> sourceBoundedSpan = sourceBoundedMemory.Span;

                using BoundedMemory<char> valueBoundedMemory = BoundedMemory.AllocateFromExistingData<char>(value);
                valueBoundedMemory.MakeReadonly();
                ReadOnlySpan<char> valueBoundedSpan = valueBoundedMemory.Span;

                int offsetResult = result;
                if (offsetResult >= 0)
                {
                    offsetResult -= startIndex; // account for span slicing
                    Assert.True(offsetResult >= 0, "Shouldn't have made an affirmative result go negative.");
                }

                Assert.Equal(offsetResult, sourceBoundedSpan.IndexOf(valueBoundedSpan, GetStringComparison(options)));
                Assert.Equal(offsetResult, compareInfo.IndexOf(sourceBoundedSpan, valueBoundedSpan, options));
                Assert.Equal(offsetResult, compareInfo.IndexOf(sourceBoundedSpan, valueBoundedSpan, options, out int matchLength));
                if (offsetResult >= 0)
                {
                    Assert.Equal(valueBoundedSpan.Length, matchLength); // Invariant mode should perform non-linguistic comparisons
                }
                else
                {
                    Assert.Equal(0, matchLength); // not found
                }
            }
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [MemberData(nameof(LastIndexOf_TestData))]
        public void TestLastIndexOf(string source, string value, int startIndex, int count, CompareOptions options, int result)
        {
            foreach (string cul in s_cultureNames)
            {
                CompareInfo compareInfo = CultureInfo.GetCultureInfo(cul).CompareInfo;
                TestCore(compareInfo, source, value, startIndex, count, options, result);
            }

            // static test helper method to avoid mutating input args when called in a loop
            static void TestCore(CompareInfo compareInfo, string source, string value, int startIndex, int count, CompareOptions options, int result)
            {
                Assert.Equal(result, compareInfo.LastIndexOf(source, value, startIndex, count, options));
                Assert.Equal(result, source.LastIndexOf(value, startIndex, count, GetStringComparison(options)));

                // Filter differences betweeen string-based and Span-based LastIndexOf
                // - Empty value handling - https://github.com/dotnet/runtime/issues/13382
                // - Negative count
                if (value.Length == 0 || count < 0)
                    return;

                if (startIndex == source.Length)
                {
                    startIndex--;
                    if (count > 0)
                        count--;
                }
                int leftStartIndex = (startIndex - count + 1);

                // Span versions - using BoundedMemory to check for buffer overruns

                using BoundedMemory<char> sourceBoundedMemory = BoundedMemory.AllocateFromExistingData<char>(source.AsSpan(leftStartIndex, count));
                sourceBoundedMemory.MakeReadonly();
                ReadOnlySpan<char> sourceBoundedSpan = sourceBoundedMemory.Span;

                using BoundedMemory<char> valueBoundedMemory = BoundedMemory.AllocateFromExistingData<char>(value);
                valueBoundedMemory.MakeReadonly();
                ReadOnlySpan<char> valueBoundedSpan = valueBoundedMemory.Span;

                if (result >= 0)
                {
                    result -= leftStartIndex; // account for span slicing
                    Assert.True(result >= 0, "Shouldn't have made an affirmative result go negative.");
                }

                Assert.Equal(result, sourceBoundedSpan.LastIndexOf(valueBoundedSpan, GetStringComparison(options)));
                Assert.Equal(result, compareInfo.LastIndexOf(sourceBoundedSpan, valueBoundedSpan, options));
                Assert.Equal(result, compareInfo.LastIndexOf(sourceBoundedSpan, valueBoundedSpan, options, out int matchLength));
                if (result >= 0)
                {
                    Assert.Equal(valueBoundedSpan.Length, matchLength); // Invariant mode should perform non-linguistic comparisons
                }
                else
                {
                    Assert.Equal(0, matchLength); // not found
                }
            }
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [MemberData(nameof(IsPrefix_TestData))]
        public void TestIsPrefix(string source, string value, CompareOptions options, bool result)
        {
            foreach (string cul in s_cultureNames)
            {
                CompareInfo compareInfo = CultureInfo.GetCultureInfo(cul).CompareInfo;

                Assert.Equal(result, compareInfo.IsPrefix(source, value, options));
                Assert.Equal(result, source.StartsWith(value, GetStringComparison(options)));

                // Span versions - using BoundedMemory to check for buffer overruns

                using BoundedMemory<char> sourceBoundedMemory = BoundedMemory.AllocateFromExistingData<char>(source);
                sourceBoundedMemory.MakeReadonly();
                ReadOnlySpan<char> sourceBoundedSpan = sourceBoundedMemory.Span;

                using BoundedMemory<char> valueBoundedMemory = BoundedMemory.AllocateFromExistingData<char>(value);
                valueBoundedMemory.MakeReadonly();
                ReadOnlySpan<char> valueBoundedSpan = valueBoundedMemory.Span;

                Assert.Equal(result, sourceBoundedSpan.StartsWith(valueBoundedSpan, GetStringComparison(options)));
                Assert.Equal(result, compareInfo.IsPrefix(sourceBoundedSpan, valueBoundedSpan, options));
                Assert.Equal(result, compareInfo.IsPrefix(sourceBoundedSpan, valueBoundedSpan, options, out int matchLength));
                if (result)
                {
                    Assert.Equal(valueBoundedSpan.Length, matchLength); // Invariant mode should perform non-linguistic comparisons
                }
                else
                {
                    Assert.Equal(0, matchLength); // not found
                }
            }
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [MemberData(nameof(IsSuffix_TestData))]
        public void TestIsSuffix(string source, string value, CompareOptions options, bool result)
        {
            foreach (string cul in s_cultureNames)
            {
                CompareInfo compareInfo = CultureInfo.GetCultureInfo(cul).CompareInfo;

                Assert.Equal(result, compareInfo.IsSuffix(source, value, options));
                Assert.Equal(result, source.EndsWith(value, GetStringComparison(options)));

                // Span versions - using BoundedMemory to check for buffer overruns

                using BoundedMemory<char> sourceBoundedMemory = BoundedMemory.AllocateFromExistingData<char>(source);
                sourceBoundedMemory.MakeReadonly();
                ReadOnlySpan<char> sourceBoundedSpan = sourceBoundedMemory.Span;

                using BoundedMemory<char> valueBoundedMemory = BoundedMemory.AllocateFromExistingData<char>(value);
                valueBoundedMemory.MakeReadonly();
                ReadOnlySpan<char> valueBoundedSpan = valueBoundedMemory.Span;

                Assert.Equal(result, sourceBoundedSpan.EndsWith(valueBoundedSpan, GetStringComparison(options)));
                Assert.Equal(result, compareInfo.IsSuffix(sourceBoundedSpan, valueBoundedSpan, options));
                Assert.Equal(result, compareInfo.IsSuffix(sourceBoundedSpan, valueBoundedSpan, options, out int matchLength));
                if (result)
                {
                    Assert.Equal(valueBoundedSpan.Length, matchLength); // Invariant mode should perform non-linguistic comparisons
                }
                else
                {
                    Assert.Equal(0, matchLength); // not found
                }
            }
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [InlineData("", false)]
        [InlineData('x', true)]
        [InlineData('\ud800', true)] // standalone high surrogate
        [InlineData("hello", true)]
        public void TestIsSortable(object sourceObj, bool expectedResult)
        {
            if (sourceObj is string s)
            {
                Assert.Equal(expectedResult, CompareInfo.IsSortable(s));
            }
            else
            {
                Assert.Equal(expectedResult, CompareInfo.IsSortable((char)sourceObj));
            }
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [MemberData(nameof(Compare_TestData))]
        public void TestCompare(string source, string value, CompareOptions options, int result)
        {
            foreach (string cul in s_cultureNames)
            {
                int res = CultureInfo.GetCultureInfo(cul).CompareInfo.Compare(source, value, options);
                Assert.Equal(result, Math.Sign(res));

                res = string.Compare(source, value, GetStringComparison(options));
                Assert.Equal(result, Math.Sign(res));

                // Span versions - using BoundedMemory to check for buffer overruns

                using BoundedMemory<char> sourceBoundedMemory = BoundedMemory.AllocateFromExistingData<char>(source);
                sourceBoundedMemory.MakeReadonly();
                ReadOnlySpan<char> sourceBoundedSpan = sourceBoundedMemory.Span;

                using BoundedMemory<char> valueBoundedMemory = BoundedMemory.AllocateFromExistingData<char>(value);
                valueBoundedMemory.MakeReadonly();
                ReadOnlySpan<char> valueBoundedSpan = valueBoundedMemory.Span;

                res = CultureInfo.GetCultureInfo(cul).CompareInfo.Compare(sourceBoundedSpan, valueBoundedSpan, options);
                Assert.Equal(result, Math.Sign(res));

                res = sourceBoundedSpan.CompareTo(valueBoundedSpan, GetStringComparison(options));
                Assert.Equal(result, Math.Sign(res));
            }
        }


        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [MemberData(nameof(ToLower_TestData))]
        public void TestToLower(string upper, string lower, bool result)
        {
            foreach (string cul in s_cultureNames)
            {
                Assert.Equal(result, CultureInfo.GetCultureInfo(cul).TextInfo.ToLower(upper).Equals(lower, StringComparison.Ordinal));
                Assert.Equal(result, upper.ToLower().Equals(lower, StringComparison.Ordinal));
            }
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [MemberData(nameof(ToUpper_TestData))]
        public void TestToUpper(string lower, string upper, bool result)
        {
            foreach (string cul in s_cultureNames)
            {
                Assert.Equal(result, CultureInfo.GetCultureInfo(cul).TextInfo.ToUpper(lower).Equals(upper, StringComparison.Ordinal));
                Assert.Equal(result, lower.ToUpper().Equals(upper, StringComparison.Ordinal));
            }
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [InlineData("", NormalizationForm.FormC)]
        [InlineData("\uFB01", NormalizationForm.FormC)]
        [InlineData("\uFB01", NormalizationForm.FormD)]
        [InlineData("\uFB01", NormalizationForm.FormKC)]
        [InlineData("\uFB01", NormalizationForm.FormKD)]
        [InlineData("\u1E9b\u0323", NormalizationForm.FormC)]
        [InlineData("\u1E9b\u0323", NormalizationForm.FormD)]
        [InlineData("\u1E9b\u0323", NormalizationForm.FormKC)]
        [InlineData("\u1E9b\u0323", NormalizationForm.FormKD)]
        [InlineData("\u00C4\u00C7", NormalizationForm.FormC)]
        [InlineData("\u00C4\u00C7", NormalizationForm.FormD)]
        [InlineData("A\u0308C\u0327", NormalizationForm.FormC)]
        [InlineData("A\u0308C\u0327", NormalizationForm.FormD)]
        public void TestNormalization(string s, NormalizationForm form)
        {
            Assert.True(s.IsNormalized());
            Assert.True(s.IsNormalized(form));
            Assert.Equal(s, s.Normalize());
            Assert.Equal(s, s.Normalize(form));
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [MemberData(nameof(GetAscii_TestData))]
        public void GetAscii(string unicode, int index, int count, string expected)
        {
            if (index + count == unicode.Length)
            {
                if (index == 0)
                {
                    Assert.Equal(expected, new IdnMapping().GetAscii(unicode));
                }
                Assert.Equal(expected, new IdnMapping().GetAscii(unicode, index));
            }
            Assert.Equal(expected, new IdnMapping().GetAscii(unicode, index, count));
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [MemberData(nameof(GetUnicode_TestData))]
        public void GetUnicode(string ascii, int index, int count, string expected)
        {
            if (index + count == ascii.Length)
            {
                if (index == 0)
                {
                    Assert.Equal(expected, new IdnMapping().GetUnicode(ascii));
                }
                Assert.Equal(expected, new IdnMapping().GetUnicode(ascii, index));
            }
            Assert.Equal(expected, new IdnMapping().GetUnicode(ascii, index, count));
        }

        [ConditionalFact(nameof(PredefinedCulturesOnlyIsDisabled))]
        public void TestHashing()
        {
            StringComparer cultureComparer = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), true);
            StringComparer ordinalComparer = StringComparer.OrdinalIgnoreCase;
            string turkishString = "i\u0130";
            Assert.Equal(ordinalComparer.GetHashCode(turkishString), cultureComparer.GetHashCode(turkishString));
        }

        [ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [InlineData('a', 'A', 'a')]
        [InlineData('A', 'A', 'a')]
        [InlineData('i', 'I', 'i')] // to verify that we don't special-case the Turkish I in the invariant globalization mode
        [InlineData('I', 'I', 'i')]
        [InlineData('\u017f', '\u017f', '\u017f')] // Latin small letter long S shouldn't be case mapped in the invariant mode.
        [InlineData(0x00C1, 0x00C1, 0x00E1)] // U+00C1 LATIN CAPITAL LETTER A WITH ACUTE
        [InlineData(0x00E1, 0x00C1, 0x00E1)] // U+00E1 LATIN SMALL LETTER A WITH ACUTE
        [InlineData(0x00D7, 0x00D7, 0x00D7)] // U+00D7 MULTIPLICATION SIGN
        public void TestRune(int original, int expectedToUpper, int expectedToLower)
        {
            Rune originalRune = new Rune(original);

            Assert.Equal(expectedToUpper, Rune.ToUpperInvariant(originalRune).Value);
            Assert.Equal(expectedToUpper, Rune.ToUpper(originalRune, CultureInfo.GetCultureInfo("tr-TR")).Value);

            Assert.Equal(expectedToLower, Rune.ToLowerInvariant(originalRune).Value);
            Assert.Equal(expectedToLower, Rune.ToLower(originalRune, CultureInfo.GetCultureInfo("tr-TR")).Value);
        }

        [ConditionalFact(nameof(PredefinedCulturesOnlyIsDisabled))]
        public void TestGetCultureInfo_PredefinedOnly_ReturnsSame()
        {
            Assert.Equal(CultureInfo.GetCultureInfo("en-US"), CultureInfo.GetCultureInfo("en-US", predefinedOnly: true));
        }

        private static byte[] GetExpectedInvariantOrdinalSortKey(ReadOnlySpan<char> input)
        {
            MemoryStream memoryStream = new MemoryStream();
            Span<byte> tempBuffer = stackalloc byte[sizeof(char)];

            foreach (char ch in input)
            {
                BinaryPrimitives.WriteUInt16BigEndian(tempBuffer, (ushort)ch);
                memoryStream.Write(tempBuffer);
            }

            return memoryStream.ToArray();
        }
    }
}
