// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class RegexConstructorTests
{
    [Fact]
    public static void Ctor()
    {
        // Should not throw
        new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
    }

    [Fact]
    public static void Ctor_Invalid()
    {
        // Pattern is null
        Assert.Throws<ArgumentNullException>(() => new Regex(null));
        Assert.Throws<ArgumentNullException>(() => new Regex(null, RegexOptions.None));
        Assert.Throws<ArgumentNullException>(() => new Regex(null, RegexOptions.None, new TimeSpan()));
        
        // Options is invalid
        Assert.Throws<ArgumentOutOfRangeException>(() => new Regex("foo", (RegexOptions)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Regex("foo", (RegexOptions)(-1), new TimeSpan()));

        Assert.Throws<ArgumentOutOfRangeException>(() => new Regex("foo", (RegexOptions)0x400));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Regex("foo", (RegexOptions)0x400, new TimeSpan()));

        Assert.Throws<ArgumentOutOfRangeException>(() => new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.RightToLeft));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Singleline));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace));
    }
}
