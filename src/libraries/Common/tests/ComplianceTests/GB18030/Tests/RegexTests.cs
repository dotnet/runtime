// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Tests;
using System.Text.Unicode;
using System.Threading.Tasks;
using Xunit;

namespace GB18030.Tests;

/// <summary>
/// Regex does not support surrogate pairs, which drastically reduces the number of characters in GB18030 that can be tested.
/// </summary>
public class RegexTests
{
    // Ranges added in GB18030-2020
    private static readonly UnicodeRange s_cjkNewRange = UnicodeRange.Create((char)0x9FF0, (char)0x9FFF);
    private static readonly UnicodeRange s_cjkExtensionANewRange = UnicodeRange.Create((char)0x4DB6, (char)0x4DBF);

    private static readonly IEnumerable<string> s_cjkNewCharacters = Enumerable.Range(s_cjkNewRange.FirstCodePoint, s_cjkNewRange.Length).Select(c => ((char)c).ToString());
    private static readonly IEnumerable<string> s_cjkExtensionANewCharacters = Enumerable.Range(s_cjkExtensionANewRange.FirstCodePoint, s_cjkExtensionANewRange.Length).Select(c => ((char)c).ToString());
    private static readonly IEnumerable<string> s_allNewCharacters = s_cjkNewCharacters.Union(s_cjkExtensionANewCharacters);

    private static readonly Dictionary<UnicodeRange, (string, string[])> s_rangeToRegexMap = new()
    {
        { s_cjkNewRange, ("IsCJKUnifiedIdeographs", s_cjkNewCharacters.ToArray()) },
        { s_cjkExtensionANewRange, ("IsCJKUnifiedIdeographsExtensionA", s_cjkExtensionANewCharacters.ToArray()) }
    };

    public static IEnumerable<object[]> UnicodeCategories_TestData() =>
        RegexHelpers.AvailableEngines.SelectMany(engine =>
        TestHelper.s_cultures.Select(culture => new object[] { engine, culture }));

    [Theory]
    [MemberData(nameof(UnicodeCategories_TestData))]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2522617")]
    public async Task UnicodeCategory_InclusionAsync(RegexEngine engine, CultureInfo culture)
    {
        Regex r = await RegexHelpers.GetRegexAsync(engine, @"\p{Lo}", RegexOptions.None, culture);
        foreach (string element in s_allNewCharacters)
            Assert.Matches(r, element);

        r = await RegexHelpers.GetRegexAsync(engine, @"[\p{Lo}]", RegexOptions.None, culture);
        foreach (string element in s_allNewCharacters)
            Assert.Matches(r, element);

        r = await RegexHelpers.GetRegexAsync(engine, @"\p{L}", RegexOptions.None, culture);
        foreach (string element in s_allNewCharacters)
            Assert.Matches(r, element);

        r = await RegexHelpers.GetRegexAsync(engine, @"[\p{L}]", RegexOptions.None, culture);
        foreach (string element in s_allNewCharacters)
            Assert.Matches(r, element);
    }

    [Theory]
    [MemberData(nameof(UnicodeCategories_TestData))]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2522617")]
    public async Task UnicodeCategory_ExclusionAsync(RegexEngine engine, CultureInfo culture)
    {
        Regex r = await RegexHelpers.GetRegexAsync(engine, @"\P{Lo}", RegexOptions.None, culture);
        foreach (string element in s_allNewCharacters)
            Assert.DoesNotMatch(r, element);

        r = await RegexHelpers.GetRegexAsync(engine, @"[^\p{Lo}]", RegexOptions.None, culture);
        foreach (string element in s_allNewCharacters)
            Assert.DoesNotMatch(r, element);

        r = await RegexHelpers.GetRegexAsync(engine, @"\P{L}", RegexOptions.None, culture);
        foreach (string element in s_allNewCharacters)
            Assert.DoesNotMatch(r, element);

        r = await RegexHelpers.GetRegexAsync(engine, @"[^\p{L}]", RegexOptions.None, culture);
        foreach (string element in s_allNewCharacters)
            Assert.DoesNotMatch(r, element);
    }

    public static IEnumerable<object[]> NamedBlock_TestData() =>
        s_rangeToRegexMap.SelectMany(rangeKvp =>
        RegexHelpers.AvailableEngines.SelectMany(engine =>
        TestHelper.s_cultures.Select(culture => new object[] { rangeKvp.Key, engine, culture })));

    [Theory]
    [MemberData(nameof(NamedBlock_TestData))]
    public async Task NamedBlock_InclusionAsync(UnicodeRange range, RegexEngine engine, CultureInfo culture)
    {
        (string namedBlock, string[] charactersInRange) = s_rangeToRegexMap[range];

        Regex r = await RegexHelpers.GetRegexAsync(engine, $@"\p{{{namedBlock}}}", RegexOptions.None, culture);
        foreach (string element in charactersInRange)
            Assert.Matches(r, element);

        r = await RegexHelpers.GetRegexAsync(engine, $@"[\p{{{namedBlock}}}]", RegexOptions.None, culture);
        foreach (string element in charactersInRange)
            Assert.Matches(r, element);
    }

    [Theory]
    [MemberData(nameof(NamedBlock_TestData))]
    public async Task NamedBlock_ExclusionAsync(UnicodeRange range, RegexEngine engine, CultureInfo culture)
    {
        (string namedBlock, string[] charactersInRange) = s_rangeToRegexMap[range];

        Regex r = await RegexHelpers.GetRegexAsync(engine, $@"\P{{{namedBlock}}}", RegexOptions.None, culture);
        foreach (string element in charactersInRange)
        {
            Assert.DoesNotMatch(r, element);
        }

        r = await RegexHelpers.GetRegexAsync(engine, $@"[^\p{{{namedBlock}}}]", RegexOptions.None, culture);
        foreach (string element in charactersInRange)
        {
            Assert.DoesNotMatch(r, element);
        }
    }
}
