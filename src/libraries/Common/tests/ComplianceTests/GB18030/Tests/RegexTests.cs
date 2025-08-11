// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Tests;
using System.Threading.Tasks;
using Xunit;

namespace GB18030.Tests;

/// <summary>
/// Regex does not support surrogate pairs, which drastically reduces the number of characters in GB18030 that can be tested.
/// </summary>
public class RegexTests
{
    public enum RegexNamedBlock
    {
        IsCJKUnifiedIdeographs,
        IsCJKUnifiedIdeographsExtensionA,
    }

    private static readonly IEnumerable<string> s_cjkAndCjkExtensionANewChars = TestHelper.CjkNewCodePoints.Union(TestHelper.CjkExtensionANewCodePoints).Select(c => ((char)c).ToString());

    private static readonly List<(RegexNamedBlock, string[])> s_namedBlocks = new()
    {
        (RegexNamedBlock.IsCJKUnifiedIdeographs, TestHelper.CjkNewCodePoints.Select(c => ((char)c).ToString()).ToArray()),
        (RegexNamedBlock.IsCJKUnifiedIdeographsExtensionA, TestHelper.CjkExtensionANewCodePoints.Select(c => ((char)c).ToString()).ToArray())
    };

    public static IEnumerable<object[]> UnicodeCategories_TestData() =>
        RegexHelpers.AvailableEngines.SelectMany(engine =>
        TestHelper.Cultures.Select(culture => new object[] { engine, culture }));

    [Theory]
    [MemberData(nameof(UnicodeCategories_TestData))]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2522617")]
    public async Task UnicodeCategory_InclusionAsync(RegexEngine engine, CultureInfo culture)
    {
        Regex r = await RegexHelpers.GetRegexAsync(engine, @"\p{Lo}", RegexOptions.None, culture);
        foreach (string element in s_cjkAndCjkExtensionANewChars)
            Assert.Matches(r, element);

        r = await RegexHelpers.GetRegexAsync(engine, @"[\p{Lo}]", RegexOptions.None, culture);
        foreach (string element in s_cjkAndCjkExtensionANewChars)
            Assert.Matches(r, element);

        r = await RegexHelpers.GetRegexAsync(engine, @"\p{L}", RegexOptions.None, culture);
        foreach (string element in s_cjkAndCjkExtensionANewChars)
            Assert.Matches(r, element);

        r = await RegexHelpers.GetRegexAsync(engine, @"[\p{L}]", RegexOptions.None, culture);
        foreach (string element in s_cjkAndCjkExtensionANewChars)
            Assert.Matches(r, element);
    }

    [Theory]
    [MemberData(nameof(UnicodeCategories_TestData))]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2522617")]
    public async Task UnicodeCategory_ExclusionAsync(RegexEngine engine, CultureInfo culture)
    {
        Regex r = await RegexHelpers.GetRegexAsync(engine, @"\P{Lo}", RegexOptions.None, culture);
        foreach (string element in s_cjkAndCjkExtensionANewChars)
            Assert.DoesNotMatch(r, element);

        r = await RegexHelpers.GetRegexAsync(engine, @"[^\p{Lo}]", RegexOptions.None, culture);
        foreach (string element in s_cjkAndCjkExtensionANewChars)
            Assert.DoesNotMatch(r, element);

        r = await RegexHelpers.GetRegexAsync(engine, @"\P{L}", RegexOptions.None, culture);
        foreach (string element in s_cjkAndCjkExtensionANewChars)
            Assert.DoesNotMatch(r, element);

        r = await RegexHelpers.GetRegexAsync(engine, @"[^\p{L}]", RegexOptions.None, culture);
        foreach (string element in s_cjkAndCjkExtensionANewChars)
            Assert.DoesNotMatch(r, element);
    }

    public static IEnumerable<object[]> NamedBlock_TestData() =>
        s_namedBlocks.SelectMany(namedBlock =>
        RegexHelpers.AvailableEngines.SelectMany(engine =>
        TestHelper.Cultures.Select(culture => new object[] { namedBlock.Item2, namedBlock.Item1, engine, culture })));

    [Theory]
    [MemberData(nameof(NamedBlock_TestData))]
    public async Task NamedBlock_InclusionAsync(string[]characters, RegexNamedBlock namedBlock, RegexEngine engine, CultureInfo culture)
    {
        Regex r = await RegexHelpers.GetRegexAsync(engine, $@"\p{{{namedBlock}}}", RegexOptions.None, culture);
        foreach (string c in characters)
            Assert.Matches(r, c);

        r = await RegexHelpers.GetRegexAsync(engine, $@"[\p{{{namedBlock}}}]", RegexOptions.None, culture);
        foreach (string c in characters)
            Assert.Matches(r, c);
    }

    [Theory]
    [MemberData(nameof(NamedBlock_TestData))]
    public async Task NamedBlock_ExclusionAsync(string[] characters, RegexNamedBlock namedBlock, RegexEngine engine, CultureInfo culture)
    {
        Regex r = await RegexHelpers.GetRegexAsync(engine, $@"\P{{{namedBlock}}}", RegexOptions.None, culture);
        foreach (string c in characters)
            Assert.DoesNotMatch(r, c);

        r = await RegexHelpers.GetRegexAsync(engine, $@"[^\p{{{namedBlock}}}]", RegexOptions.None, culture);
        foreach (string c in characters)
            Assert.DoesNotMatch(r, c);
    }
}
