// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace GB18030.Tests;

public class CharTests
{
    [Theory]
    [MemberData(nameof(TestHelper.SplitNewLineDecodedTestData), MemberType = typeof(TestHelper))]
    public void ConvertRoundtrips(string decoded)
    {
        foreach (string element in TestHelper.GetTextElements(decoded))
        {
            Assert.Equal(element, char.ConvertFromUtf32(char.ConvertToUtf32(element, 0)));
        }
    }

    [Theory]
    [MemberData(nameof(TestHelper.SplitNewLineDecodedTestData), MemberType = typeof(TestHelper))]
    public void Surrogate(string decoded)
    {
        foreach (string element in TestHelper.GetTextElements(decoded).Where(e => e.Length > 1))
        {
            Assert.Equal(2, element.Length);
            char high = element[0];
            char low = element[1];
            Assert.True(char.IsSurrogate(low));
            Assert.True(char.IsSurrogate(high));
            Assert.True(char.IsLowSurrogate(low));
            Assert.True(char.IsHighSurrogate(high));
            Assert.True(char.IsSurrogatePair(high, low));
        }
    }

    [Theory]
    [MemberData(nameof(TestHelper.SplitNewLineDecodedTestData), MemberType = typeof(TestHelper))]
    public void NonSurrogate(string decoded)
    {
        foreach (string element in TestHelper.GetTextElements(decoded).Where(e => e.Length <= 1))
        {
            Assert.Equal(1, element.Length);
            char c = element[0];
            Assert.False(char.IsSurrogate(c));
            Assert.False(char.IsLowSurrogate(c));
            Assert.False(char.IsHighSurrogate(c));
            Assert.False(char.IsSurrogatePair(c, c));
        }
    }
}
