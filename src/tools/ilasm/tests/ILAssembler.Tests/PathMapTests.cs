// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ILAssembler.Tests;

/// <summary>
/// The rules of <see cref="PathMap"/>, which follow the C# compiler's <c>-pathmap</c>: the syntax of the text, the
/// directory separator completed on each key and value, and how <see cref="PathMap.Map"/> picks an entry and writes
/// the result. The paths are strings only; nothing here touches the file system.
/// </summary>
public class PathMapTests
{
    /// <summary>Parses a path map that the test expects to be valid.</summary>
    internal static PathMap Parse(string text)
    {
        Assert.True(PathMap.TryParse(text, out PathMap? pathMap, out string? error), error);
        return pathMap;
    }

    private static KeyValuePair<string, string> Entry(string key, string value) => new(key, value);

    [Fact]
    public void TryParse_OneEntry()
    {
        Assert.Equal(new[] { Entry("/goo/", "/bar/") }, Parse("/goo/=/bar/").Entries);
    }

    [Fact]
    public void TryParse_SeveralEntries_KeepTheirOrder()
    {
        Assert.Equal(
            new[] { Entry("/b/", "/y/"), Entry("/a/", "/x/") },
            Parse("/b/=/y/,/a/=/x/").Entries);
    }

    [Fact]
    public void TryParse_EmptyEntry_IsSkipped()
    {
        // As in the C# compiler, a trailing comma leaves an empty entry, which is not an error.
        Assert.Equal(new[] { Entry("/a/", "/x/") }, Parse("/a/=/x/,").Entries);
    }

    [Fact]
    public void TryParse_DoubledComma_IsACommaInThePath()
    {
        Assert.Equal(new[] { Entry("/a,b/", "/x/") }, Parse("/a,,b/=/x/").Entries);
    }

    [Fact]
    public void TryParse_DoubledEqualsSign_IsAnEqualsSignInThePath()
    {
        Assert.Equal(new[] { Entry("/a=b/", "/x/") }, Parse("/a==b/=/x/").Entries);
    }

    [Theory]
    [InlineData("/a/=/x/,/goo", "/goo")]
    [InlineData("=/x/", "=/x/")]
    [InlineData("/a/=", "/a/=")]
    [InlineData("/a/=/x/=/y/", "/a/=/x/=/y/")]
    public void TryParse_InvalidEntry_FailsWithAnErrorThatNamesIt(string text, string invalidEntry)
    {
        // An entry without '=', with an empty path, with an empty source path, and with two '='.
        Assert.False(PathMap.TryParse(text, out PathMap? pathMap, out string? error));
        Assert.Null(pathMap);
        Assert.Contains($"'{invalidEntry}'", error);
    }

    [Fact]
    public void TryParse_EmptyText_IsTheEmptyMap()
    {
        Assert.Empty(Parse(string.Empty).Entries);
    }

    [Theory]
    [InlineData("/goo", "/goo/")]
    [InlineData("C:\\goo", "C:\\goo\\")]
    [InlineData("/goo/", "/goo/")]
    [InlineData("C:\\goo\\", "C:\\goo\\")]
    public void TryParse_Key_EndsWithTheSeparatorItUses(string key, string expectedKey)
    {
        Assert.Equal(expectedKey, Assert.Single(Parse($"{key}=/x/").Entries).Key);
    }

    [Theory]
    [InlineData("/bar", "/bar/")]
    [InlineData("C:\\bar", "C:\\bar\\")]
    [InlineData("/bar/", "/bar/")]
    public void TryParse_Value_EndsWithTheSeparatorItUses(string value, string expectedValue)
    {
        Assert.Equal(expectedValue, Assert.Single(Parse($"/goo/={value}").Entries).Value);
    }

    [Theory]
    [InlineData("goo")]
    [InlineData("C:/goo\\bar")]
    public void TryParse_KeyWithoutOrWithMixedSeparators_EndsWithThePlatformSeparator(string key)
    {
        Assert.Equal(key + Path.DirectorySeparatorChar, Assert.Single(Parse($"{key}=/x/").Entries).Key);
    }

    [Fact]
    public void Map_ReplacesTheKeyWithTheValue()
    {
        Assert.Equal("/bar/x/y.il", Parse("/goo=/bar").Map("/goo/x/y.il"));
    }

    [Fact]
    public void Map_KeyMatchesWholeDirectoriesOnly()
    {
        Assert.Equal("/goooo/x", Parse("/goo=/bar").Map("/goooo/x"));
    }

    [Fact]
    public void Map_FirstMatchingEntryWins_WhenKeysNest()
    {
        Assert.Equal("/outer/b/c.il", Parse("/a=/outer,/a/b=/inner").Map("/a/b/c.il"));
    }

    [Fact]
    public void Map_FirstMatchingEntryWins_NotTheLongestKey()
    {
        Assert.Equal("/inner/c.il", Parse("/a/b=/inner,/a=/outer").Map("/a/b/c.il"));
    }

    [Fact]
    public void Map_ValueWithOnlySlashes_MakesEverySeparatorASlash()
    {
        Assert.Equal("/_/dir/file.il", Parse("C:\\src=/_").Map("C:\\src\\dir\\file.il"));
    }

    [Fact]
    public void Map_ValueWithOnlyBackslashes_MakesEverySeparatorABackslash()
    {
        Assert.Equal("D:\\out\\dir\\file.il", Parse("/src=D:\\out").Map("/src/dir/file.il"));
    }

    [Fact]
    public void Map_ValueWithBothSeparators_KeepsTheSeparatorsOfTheRest()
    {
        Assert.Equal("D:/out\\x/dir\\file.il", Parse("/src/=D:/out\\x/").Map("/src/dir\\file.il"));
    }

    [Fact]
    public void Map_PathThatNoKeyMatches_IsUnchanged()
    {
        Assert.Equal("relative/a.cs", Parse("/src=/_").Map("relative/a.cs"));
    }

    [Fact]
    public void Map_ComparesKeysOrdinally_SoCaseMatters()
    {
        Assert.Equal("/src/a.il", Parse("/Src=/_").Map("/src/a.il"));
    }

    [Fact]
    public void Empty_MapsEveryPathToItself()
    {
        Assert.Empty(PathMap.Empty.Entries);
        Assert.Equal("/src/a.il", PathMap.Empty.Map("/src/a.il"));
    }

    [Fact]
    public void Concat_KeepsTheEntriesOfTheFirstMapFirst()
    {
        Assert.Equal(
            new[] { Entry("/a/", "/x/"), Entry("/b/", "/y/") },
            Parse("/a=/x").Concat(Parse("/b=/y")).Entries);
    }
}
