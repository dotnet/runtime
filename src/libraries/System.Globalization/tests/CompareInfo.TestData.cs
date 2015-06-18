// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;

public partial class CompareInfoTests
{    
    public static IEnumerable<object[]> CompareToData
    {
        get
        {
            yield return new object[] { "", null, "Test's", -1, CompareOptions.None };
            yield return new object[] { "", "Test's", null, 1, CompareOptions.None };
            yield return new object[] { "", null, null, 0, CompareOptions.None };
            yield return new object[] { "", "\u3042", "\u30A1", 1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3042", "\u30A2", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3042", "\uFF71", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u304D\u3083", "\u30AD\u30E3", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u304D\u3083", "\u30AD\u3083", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u304D \u3083", "\u30AD\u3083", -1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3044", "I", 1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "a", "A", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "a", "\uFF41", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "ABCDE", "\uFF21\uFF22\uFF23\uFF24\uFF25", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "ABCDE", "\uFF21\uFF22\uFF23D\uFF25", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "ABCDE", "a\uFF22\uFF23D\uFF25", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "ABCDE", "\uFF41\uFF42\uFF23D\uFF25", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u6FA4", "\u6CA2", 1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u30D6\u30D9\u30DC", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u3076\u30D9\u30DC", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u3076\u30D9\uFF8E\uFF9E", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D0\u30D3\u3076\u30D9\uFF8E\uFF9E", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\uFF8E\uFF9E", -1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\uFF8E\uFF9E", -1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u3079\uFF8E\uFF9E", -1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D6", -1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3071\u3074\u30D7\u307A", "\uFF8B\uFF9F\uFF8C\uFF9F", -1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u3070\uFF8E\uFF9E\u30D6", 1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C\u3079\u307C", "\u3079\uFF8E\uFF9E", -1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3070\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D6", -1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "ABDDE", "D", -1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "ABCDE", "\uFF43D\uFF25", -1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "ABCDE", "\uFF43D", -1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "ABCDE", "c", -1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3060", "\u305F", 1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3060", "\uFF80\uFF9E", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3060", "\u30C0", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u30C7\u30BF\u30D9\u30B9", "\uFF83\uFF9E\uFF80\uFF8D\uFF9E\uFF7D", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u30C7", "\uFF83\uFF9E", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u30C7\u30BF", "\uFF83\uFF9E\uFF80", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u30C7\u30BF\u30D9", "\uFF83\uFF9E\uFF80\uFF8D\uFF9E", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u30BF", "\uFF80", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\uFF83\uFF9E\uFF70\uFF80\uFF8D\uFF9E\uFF70\uFF7D", "\u3067\u30FC\u305F\u3079\u30FC\u3059", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u68EE\u9D0E\u5916", "\u68EE\u9DD7\u5916", -1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u68EE\u9DD7\u5916", "\u68EE\u9DD7\u5916", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u2019\u2019\u2019\u2019", "''''", 1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u2019", "'", 1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "", "'", -1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u4E00", "\uFF11", 1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u2160", "\uFF11", 1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "0", "\uFF10", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "10", "1\uFF10", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "1\uFF10", "1\uFF10", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "9999\uFF1910", "1\uFF10", 1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "9999\uFF191010", "1\uFF10", 1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "'\u3000'", "' '", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "'\u3000'", "''", 1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\uFF1B", ";", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\uFF08", "(", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u30FC", "\uFF70", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u30FC", "\uFF0D", 1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u30FC", "\u30FC", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u30FC", "\u2015", 1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u30FC", "\u2010", 1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "/", "\uFF0F", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "'", "\uFF07", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\"", "\uFF02", 0, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth };
            yield return new object[] { "", "\u3042", "\u30A1", 1, CompareOptions.None };
            yield return new object[] { "", "\u3042", "\u30A2", 1, CompareOptions.None };
            yield return new object[] { "", "\u3042", "\uFF71", 1, CompareOptions.None };
            yield return new object[] { "", "\u304D\u3083", "\u30AD\u30E3", 1, CompareOptions.None };
            yield return new object[] { "", "\u304D\u3083", "\u30AD\u3083", 1, CompareOptions.None };
            yield return new object[] { "", "\u304D \u3083", "\u30AD\u3083", -1, CompareOptions.None };
            yield return new object[] { "", "\u3044", "I", 1, CompareOptions.None };
            yield return new object[] { "", "a", "A", -1, CompareOptions.None };
            yield return new object[] { "", "a", "\uFF41", -1, CompareOptions.None };
            yield return new object[] { "", "ABCDE", "\uFF21\uFF22\uFF23\uFF24\uFF25", -1, CompareOptions.None };
            yield return new object[] { "", "ABCDE", "\uFF21\uFF22\uFF23D\uFF25", -1, CompareOptions.None };
            yield return new object[] { "", "ABCDE", "a\uFF22\uFF23D\uFF25", -1, CompareOptions.None };
            yield return new object[] { "", "ABCDE", "\uFF41\uFF42\uFF23D\uFF25", 1, CompareOptions.None };
            yield return new object[] { "", "ABCDE", "\uFF41\uFF42\uFF23\uFF24\uFF25", 1, CompareOptions.IgnoreWidth };
            yield return new object[] { "", "ABCDE", "\uFF41\uFF42\uFF23\uFF24\uFF25", 0, CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase };
            yield return new object[] { "", "\u6FA4", "\u6CA2", 1, CompareOptions.None };
            yield return new object[] { "", "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u30D6\u30D9\u30DC", 1, CompareOptions.None };
            yield return new object[] { "", "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u3076\u30D9\u30DC", 1, CompareOptions.None };
            yield return new object[] { "", "\u3070\u3073\u3076\u3079\u307C", "\u30D0\u30D3\u3076\u30D9\uFF8E\uFF9E", 1, CompareOptions.None };
            yield return new object[] { "", "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D0\u30D3\u3076\u30D9\uFF8E\uFF9E", 1, CompareOptions.None };
            yield return new object[] { "", "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\uFF8E\uFF9E", -1, CompareOptions.None };
            yield return new object[] { "", "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\uFF8E\uFF9E", -1, CompareOptions.None };
            yield return new object[] { "", "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u3079\uFF8E\uFF9E", -1, CompareOptions.None };
            yield return new object[] { "", "\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D6", -1, CompareOptions.None };
            yield return new object[] { "", "\u3071\u3074\u30D7\u307A", "\uFF8B\uFF9F\uFF8C\uFF9F", -1, CompareOptions.None };
            yield return new object[] { "", "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u3070\uFF8E\uFF9E\u30D6", 1, CompareOptions.None };
            yield return new object[] { "", "\u3070\u30DC\uFF8C\uFF9E\uFF8D\uFF9E\u307C\u3079\u307C", "\u3079\uFF8E\uFF9E", -1, CompareOptions.None };
            yield return new object[] { "", "\u3070\uFF8C\uFF9E\uFF8D\uFF9E\u307C", "\u30D6", -1, CompareOptions.None };
            yield return new object[] { "", "ABDDE", "D", -1, CompareOptions.None };
            yield return new object[] { "", "ABCDE", "\uFF43D\uFF25", -1, CompareOptions.None };
            yield return new object[] { "", "ABCDE", "\uFF43D", -1, CompareOptions.None };
            yield return new object[] { "", "ABCDE", "c", -1, CompareOptions.None };
            yield return new object[] { "", "\u3060", "\u305F", 1, CompareOptions.None };
            yield return new object[] { "", "\u3060", "\uFF80\uFF9E", 1, CompareOptions.None };
            yield return new object[] { "", "\u3060", "\u30C0", 1, CompareOptions.None };
            yield return new object[] { "", "\u30C7\u30BF\u30D9\u30B9", "\uFF83\uFF9E\uFF80\uFF8D\uFF9E\uFF7D", 1, CompareOptions.None };
            yield return new object[] { "", "\u30C7", "\uFF83\uFF9E", 1, CompareOptions.None };
            yield return new object[] { "", "\u30C7\u30BF", "\uFF83\uFF9E\uFF80", 1, CompareOptions.None };
            yield return new object[] { "", "\u30C7\u30BF\u30D9", "\uFF83\uFF9E\uFF80\uFF8D\uFF9E", 1, CompareOptions.None };
            yield return new object[] { "", "\u30BF", "\uFF80", 1, CompareOptions.None };
            yield return new object[] { "", "\uFF83\uFF9E\uFF70\uFF80\uFF8D\uFF9E\uFF70\uFF7D", "\u3067\u30FC\u305F\u3079\u30FC\u3059", -1, CompareOptions.None };
            yield return new object[] { "", "\u68EE\u9D0E\u5916", "\u68EE\u9DD7\u5916", -1, CompareOptions.None };
            yield return new object[] { "", "\u68EE\u9DD7\u5916", "\u68EE\u9DD7\u5916", 0, CompareOptions.None };
            yield return new object[] { "", "\u2019\u2019\u2019\u2019", "''''", 1, CompareOptions.None };
            yield return new object[] { "", "\u2019", "'", 1, CompareOptions.None };
            yield return new object[] { "", "", "'", -1, CompareOptions.None };
            yield return new object[] { "", "\u4E00", "\uFF11", 1, CompareOptions.None };
            yield return new object[] { "", "\u2160", "\uFF11", 1, CompareOptions.None };
            yield return new object[] { "", "0", "\uFF10", -1, CompareOptions.None };
            yield return new object[] { "", "10", "1\uFF10", -1, CompareOptions.None };
            yield return new object[] { "", "1\uFF10", "1\uFF10", 0, CompareOptions.None };
            yield return new object[] { "", "9999\uFF1910", "1\uFF10", 1, CompareOptions.None };
            yield return new object[] { "", "9999\uFF191010", "1\uFF10", 1, CompareOptions.None };
            yield return new object[] { "", "'\u3000'", "' '", 1, CompareOptions.None };
            yield return new object[] { "", "'\u3000'", "''", 1, CompareOptions.None };
            yield return new object[] { "", "\uFF1B", ";", 1, CompareOptions.None };
            yield return new object[] { "", "\uFF08", "(", 1, CompareOptions.None };
            yield return new object[] { "", "\u30FC", "\uFF70", 0, CompareOptions.None };
            yield return new object[] { "", "\u30FC", "\uFF0D", 1, CompareOptions.None };
            yield return new object[] { "", "\u30FC", "\u30FC", 0, CompareOptions.None };
            yield return new object[] { "", "\u30FC", "\u2015", 1, CompareOptions.None };
            yield return new object[] { "", "\u30FC", "\u2010", 1, CompareOptions.None };
            yield return new object[] { "", "/", "\uFF0F", -1, CompareOptions.None };
            yield return new object[] { "", "'", "\uFF07", -1, CompareOptions.None };
            yield return new object[] { "", "\"", "\uFF02", -1, CompareOptions.None };
            yield return new object[] { "hu-HU", "dzsdzs", "ddzs", 0, CompareOptions.None };
            yield return new object[] { "hu-HU", "dzsdzs", "ddzs", 1, CompareOptions.Ordinal };
            yield return new object[] { "", "dzsdzs", "ddzs", 1, CompareOptions.None };
            yield return new object[] { "", "dzsdzs", "ddzs", 1, CompareOptions.Ordinal };
            yield return new object[] { "tr-TR", "i", "I", 1, CompareOptions.IgnoreCase };
            yield return new object[] { "", "i", "I", 0, CompareOptions.IgnoreCase };
            yield return new object[] { "tr-TR", "i", "\u0130", 0, CompareOptions.IgnoreCase };
            yield return new object[] { "", "i", "\u0130", -1, CompareOptions.IgnoreCase };
            yield return new object[] { "tr-TR", "i", "I", 1, CompareOptions.None };
            yield return new object[] { "", "i", "I", -1, CompareOptions.None };
            yield return new object[] { "tr-TR", "i", "\u0130", -1, CompareOptions.None };
            yield return new object[] { "", "i", "\u0130", -1, CompareOptions.None };
            yield return new object[] { "", "\u00C0", "A\u0300", 0, CompareOptions.None };
            yield return new object[] { "", "\u00C0", "A\u0300", 1, CompareOptions.Ordinal };
            yield return new object[] { "", "\u00C0", "a\u0300", 0, CompareOptions.IgnoreCase };
            yield return new object[] { "", "\u00C0", "a\u0300", 1, CompareOptions.OrdinalIgnoreCase };
            yield return new object[] { "", "\u00C0", "a\u0300", 1, CompareOptions.None };
            yield return new object[] { "", "\u00C0", "a\u0300", 1, CompareOptions.Ordinal };
            yield return new object[] { "", "FooBar", "Foo\u0400Bar", -1, CompareOptions.None };
            yield return new object[] { "", "FooBar", "Foo\u0400Bar", -1, CompareOptions.Ordinal };
            yield return new object[] { "", "FooBar", "Foo\u0400Bar", -1, CompareOptions.IgnoreNonSpace };
            yield return new object[] { "", "FooBA\u0300R", "Foo\u0400B\u00C0R", -1, CompareOptions.IgnoreNonSpace };
            yield return new object[] { "", "Test's", "Tests", 0, CompareOptions.IgnoreSymbols };
            yield return new object[] { "", "Test's", "Tests", 1, CompareOptions.None };
            yield return new object[] { "", "Test's", "Tests", -1, CompareOptions.StringSort };
            yield return new object[] { "", new string('a', 5555), new string('a', 5555), 0, CompareOptions.None };
            yield return new object[] { "", new string('a', 5555), new string('a', 5555) + "b", -1, CompareOptions.None };
        }
    }

    public static IEnumerable<object[]> IndexOfData
    {
        get
        {
            yield return new object[] { "", "foo", "", 0, CompareOptions.None };
            yield return new object[] { "", "", "", 0, CompareOptions.None };
            yield return new object[] { "", new string('b', 100) + new string('a', 5555), "aaaaaaaaaaaaaaa", 100, CompareOptions.None };
            yield return new object[] { "", new string('b', 101) + new string('a', 5555), new string('a', 5000), 101, CompareOptions.None };
            yield return new object[] { "", new string('a', 5555), new string('a', 5000) + "b", -1, CompareOptions.None };
            yield return new object[] { "hu-HU", "foobardzsdzs", "rddzs", 5, CompareOptions.None };
            yield return new object[] { "hu-HU", "foobardzsdzs", "rddzs", -1, CompareOptions.Ordinal };
            yield return new object[] { "", "foobardzsdzs", "rddzs", -1, CompareOptions.None };
            yield return new object[] { "", "foobardzsdzs", "rddzs", -1, CompareOptions.Ordinal };
            yield return new object[] { "tr-TR", "Hi", "I", -1, CompareOptions.IgnoreCase };
            yield return new object[] { "", "Hi", "I", 1, CompareOptions.IgnoreCase };
            yield return new object[] { "", "Hi", "I", 1, CompareOptions.OrdinalIgnoreCase };
            yield return new object[] { "tr-TR", "Hi", "\u0130", 1, CompareOptions.IgnoreCase };
            yield return new object[] { "", "Hi", "\u0130", -1, CompareOptions.IgnoreCase };
            yield return new object[] { "tr-TR", "Hi", "I", -1, CompareOptions.None };
            yield return new object[] { "", "Hi", "I", -1, CompareOptions.None };
            yield return new object[] { "tr-TR", "Hi", "\u0130", -1, CompareOptions.None };
            yield return new object[] { "", "Hi", "\u0130", -1, CompareOptions.None };
            yield return new object[] { "", "Exhibit \u00C0", "A\u0300", 8, CompareOptions.None };
            yield return new object[] { "", "Exhibit \u00C0", "A\u0300", -1, CompareOptions.Ordinal };
            yield return new object[] { "", "Exhibit \u00C0", "a\u0300", 8, CompareOptions.IgnoreCase };
            yield return new object[] { "", "Exhibit \u00C0", "a\u0300", -1, CompareOptions.OrdinalIgnoreCase };
            yield return new object[] { "", "Exhibit \u00C0", "a\u0300", -1, CompareOptions.None };
            yield return new object[] { "", "Exhibit \u00C0", "a\u0300", -1, CompareOptions.Ordinal };
            yield return new object[] { "", "FooBar", "Foo\u0400Bar", -1, CompareOptions.None };
            yield return new object[] { "", "FooBar", "Foo\u0400Bar", -1, CompareOptions.Ordinal };
            yield return new object[] { "", "~FooBar", "Foo\u0400Bar", -1, CompareOptions.IgnoreNonSpace };
            yield return new object[] { "", "TestFooBA\u0300R", "Foo\u0400B\u00C0R", -1, CompareOptions.IgnoreNonSpace };
            yield return new object[] { "", "More Test's", "Tests", 5, CompareOptions.IgnoreSymbols };
            yield return new object[] { "", "More Test's", "Tests", -1, CompareOptions.None };
            yield return new object[] { "", "cbabababdbaba", "ab", 2, CompareOptions.None };
        }
    }

    public static IEnumerable<object[]> LastIndexOfData
    {
        get
        {
            yield return new object[] { "", "foo", "", 2, CompareOptions.None };
            yield return new object[] { "", "", "", 0, CompareOptions.None };
            yield return new object[] { "", new string('a', 5555) + new string('b', 100), "aaaaaaaaaaaaaaa", 5540, CompareOptions.None };
            yield return new object[] { "", new string('b', 101) + new string('a', 5555), new string('a', 5000), 656, CompareOptions.None };
            yield return new object[] { "", new string('a', 5555), new string('a', 5000) + "b", -1, CompareOptions.None };
            yield return new object[] { "hu-HU", "foobardzsdzs", "rddzs", 5, CompareOptions.None };
            yield return new object[] { "hu-HU", "foobardzsdzs", "rddzs", -1, CompareOptions.Ordinal };
            yield return new object[] { "", "foobardzsdzs", "rddzs", -1, CompareOptions.None };
            yield return new object[] { "", "foobardzsdzs", "rddzs", -1, CompareOptions.Ordinal };
            yield return new object[] { "tr-TR", "Hi", "I", -1, CompareOptions.IgnoreCase };
            yield return new object[] { "", "Hi", "I", 1, CompareOptions.IgnoreCase };
            yield return new object[] { "", "Hi", "I", 1, CompareOptions.OrdinalIgnoreCase };
            yield return new object[] { "tr-TR", "Hi", "\u0130", 1, CompareOptions.IgnoreCase };
            yield return new object[] { "", "Hi", "\u0130", -1, CompareOptions.IgnoreCase };
            yield return new object[] { "tr-TR", "Hi", "I", -1, CompareOptions.None };
            yield return new object[] { "", "Hi", "I", -1, CompareOptions.None };
            yield return new object[] { "tr-TR", "Hi", "\u0130", -1, CompareOptions.None };
            yield return new object[] { "", "Hi", "\u0130", -1, CompareOptions.None };
            yield return new object[] { "", "Exhibit \u00C0", "A\u0300", 8, CompareOptions.None };
            yield return new object[] { "", "Exhibit \u00C0", "A\u0300", -1, CompareOptions.Ordinal };
            yield return new object[] { "", "Exhibit \u00C0", "a\u0300", 8, CompareOptions.IgnoreCase };
            yield return new object[] { "", "Exhibit \u00C0", "a\u0300", -1, CompareOptions.OrdinalIgnoreCase };
            yield return new object[] { "", "Exhibit \u00C0", "a\u0300", -1, CompareOptions.None };
            yield return new object[] { "", "Exhibit \u00C0", "a\u0300", -1, CompareOptions.Ordinal };
            yield return new object[] { "", "FooBar", "Foo\u0400Bar", -1, CompareOptions.None };
            yield return new object[] { "", "FooBar", "Foo\u0400Bar", -1, CompareOptions.Ordinal };
            yield return new object[] { "", "~FooBar", "Foo\u0400Bar", -1, CompareOptions.IgnoreNonSpace };
            yield return new object[] { "", "TestFooBA\u0300R", "Foo\u0400B\u00C0R", -1, CompareOptions.IgnoreNonSpace };
            yield return new object[] { "", "More Test's", "Tests", 5, CompareOptions.IgnoreSymbols };
            yield return new object[] { "", "More Test's", "Tests", -1, CompareOptions.None };
            yield return new object[] { "", "cbabababdbaba", "ab", 10, CompareOptions.None };
        }
    }

    public static IEnumerable<object[]> IsPrefixData
    {
        get
        {
            yield return new object[] { "", new string('a', 5555), "aaaaaaaaaaaaaaa", true, CompareOptions.None };
            yield return new object[] { "", new string('a', 5555), new string('a', 5000), true, CompareOptions.None };
            yield return new object[] { "", new string('a', 5555), new string('a', 5000) + "b", false, CompareOptions.None };
            yield return new object[] { "", "foo", "", true, CompareOptions.None };
            yield return new object[] { "", "", "", true, CompareOptions.None };
            yield return new object[] { "hu-HU", "dzsdzsfoobar", "ddzsf", true, CompareOptions.None };
            yield return new object[] { "hu-HU", "dzsdzsfoobar", "ddzsf", false, CompareOptions.Ordinal };
            yield return new object[] { "", "dzsdzsfoobar", "ddzsf", false, CompareOptions.None };
            yield return new object[] { "", "dzsdzsfoobar", "ddzsf", false, CompareOptions.Ordinal };
            yield return new object[] { "tr-TR", "interesting", "I", false, CompareOptions.IgnoreCase };
            yield return new object[] { "", "interesting", "I", true, CompareOptions.IgnoreCase };
            yield return new object[] { "tr-TR", "interesting", "\u0130", true, CompareOptions.IgnoreCase };
            yield return new object[] { "", "interesting", "\u0130", false, CompareOptions.IgnoreCase };
            yield return new object[] { "tr-TR", "interesting", "I", false, CompareOptions.None };
            yield return new object[] { "", "interesting", "I", false, CompareOptions.None };
            yield return new object[] { "tr-TR", "interesting", "\u0130", false, CompareOptions.None };
            yield return new object[] { "", "interesting", "\u0130", false, CompareOptions.None };
            yield return new object[] { "", "\u00C0nimal", "A\u0300", true, CompareOptions.None };
            yield return new object[] { "", "\u00C0nimal", "A\u0300", false, CompareOptions.Ordinal };
            yield return new object[] { "", "\u00C0nimal", "a\u0300", true, CompareOptions.IgnoreCase };
            yield return new object[] { "", "\u00C0nimal", "a\u0300", false, CompareOptions.OrdinalIgnoreCase };
            yield return new object[] { "", "\u00C0nimal", "a\u0300", false, CompareOptions.None };
            yield return new object[] { "", "\u00C0nimal", "a\u0300", false, CompareOptions.Ordinal };
            yield return new object[] { "", "FooBar", "Foo\u0400Bar", false, CompareOptions.None };
            yield return new object[] { "", "FooBar", "Foo\u0400Bar", false, CompareOptions.Ordinal };
            yield return new object[] { "", "FooBar", "Foo\u0400Bar", false, CompareOptions.IgnoreNonSpace };
            yield return new object[] { "", "FooBA\u0300R", "Foo\u0400B\u00C0R", false, CompareOptions.IgnoreNonSpace };
            yield return new object[] { "", "Test's can be interesting", "Tests", true, CompareOptions.IgnoreSymbols };
            yield return new object[] { "", "Test's can be interesting", "Tests", false, CompareOptions.None };
        }
    }

    public static IEnumerable<object[]> IsSuffixData
    {
        get
        {
            yield return new object[] { "", new string('a', 5555), "aaaaaaaaaaaaaaa", true, CompareOptions.None };
            yield return new object[] { "", new string('a', 5555), new string('a', 5000), true, CompareOptions.None };
            yield return new object[] { "", new string('a', 5555), new string('a', 5000) + "b", false, CompareOptions.None };
            yield return new object[] { "", "foo", "", true, CompareOptions.None };
            yield return new object[] { "", "", "", true, CompareOptions.None };
            yield return new object[] { "hu-HU", "foobardzsdzs", "rddzs", true, CompareOptions.None };
            yield return new object[] { "hu-HU", "foobardzsdzs", "rddzs", false, CompareOptions.Ordinal };
            yield return new object[] { "", "foobardzsdzs", "rddzs", false, CompareOptions.None };
            yield return new object[] { "", "foobardzsdzs", "rddzs", false, CompareOptions.Ordinal };
            yield return new object[] { "tr-TR", "Hi", "I", false, CompareOptions.IgnoreCase };
            yield return new object[] { "", "Hi", "I", true, CompareOptions.IgnoreCase };
            yield return new object[] { "tr-TR", "Hi", "\u0130", true, CompareOptions.IgnoreCase };
            yield return new object[] { "", "Hi", "\u0130", false, CompareOptions.IgnoreCase };
            yield return new object[] { "tr-TR", "Hi", "I", false, CompareOptions.None };
            yield return new object[] { "", "Hi", "I", false, CompareOptions.None };
            yield return new object[] { "tr-TR", "Hi", "\u0130", false, CompareOptions.None };
            yield return new object[] { "", "Hi", "\u0130", false, CompareOptions.None };
            yield return new object[] { "", "Exhibit \u00C0", "A\u0300", true, CompareOptions.None };
            yield return new object[] { "", "Exhibit \u00C0", "A\u0300", false, CompareOptions.Ordinal };
            yield return new object[] { "", "Exhibit \u00C0", "a\u0300", true, CompareOptions.IgnoreCase };
            yield return new object[] { "", "Exhibit \u00C0", "a\u0300", false, CompareOptions.OrdinalIgnoreCase };
            yield return new object[] { "", "Exhibit \u00C0", "a\u0300", false, CompareOptions.None };
            yield return new object[] { "", "Exhibit \u00C0", "a\u0300", false, CompareOptions.Ordinal };
            yield return new object[] { "", "FooBar", "Foo\u0400Bar", false, CompareOptions.None };
            yield return new object[] { "", "FooBar", "Foo\u0400Bar", false, CompareOptions.Ordinal };
            yield return new object[] { "", "FooBar", "Foo\u0400Bar", false, CompareOptions.IgnoreNonSpace };
            yield return new object[] { "", "FooBA\u0300R", "Foo\u0400B\u00C0R", false, CompareOptions.IgnoreNonSpace };
            yield return new object[] { "", "More Test's", "Tests", true, CompareOptions.IgnoreSymbols };
            yield return new object[] { "", "More Test's", "Tests", false, CompareOptions.None };
        }
    }
}

