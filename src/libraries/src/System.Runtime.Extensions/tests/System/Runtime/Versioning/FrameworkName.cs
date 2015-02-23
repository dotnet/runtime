// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Versioning;
using Xunit;

public static class FrameworkNameTests
{
    private const string TestIdentifier = "TestFramework";
    private const string TestProfile = "TestProfile";

    private static readonly Version s_testVersion = new Version(1, 2, 3, 4);
    private static readonly string s_testNameString = string.Format("{0},Version=v{1},Profile={2}", TestIdentifier, s_testVersion, TestProfile);
    private static readonly string s_testNameNoProfileString = string.Format("{0},Version=v{1}", TestIdentifier, s_testVersion);
    private static readonly FrameworkName s_testName = new FrameworkName(s_testNameString);
    private static readonly FrameworkName s_testNameNoProfile = new FrameworkName(s_testNameNoProfileString);

    [Fact]
    public static void ConstructFromString()
    {
        VerifyConstructor(s_testName, s_testNameString, TestIdentifier, s_testVersion, TestProfile);
    }

    [Fact]
    public static void ConstructFromStringWithWhitespace()
    {
        var nameString = string.Format("   \r{0}\r\n, Version = {1}\t,  Profile =  {2} \r\n", TestIdentifier, s_testVersion, TestProfile);
        VerifyConstructor(new FrameworkName(nameString), s_testNameString, TestIdentifier, s_testVersion, TestProfile);
    }

    [Fact]
    public static void ConstructFromStringOmitProfile()
    {
        VerifyConstructor(s_testNameNoProfile, s_testNameNoProfileString, TestIdentifier, s_testVersion, string.Empty);
    }

    [Fact]
    public static void ConstructFromInvalidString()
    {
        Assert.Throws<ArgumentNullException>(() => new FrameworkName(null));
        Assert.Throws<ArgumentException>(() => new FrameworkName(string.Empty));
        Assert.Throws<ArgumentException>(() => new FrameworkName("A"));
        Assert.Throws<ArgumentException>(() => new FrameworkName("A,B"));
        Assert.Throws<ArgumentException>(() => new FrameworkName("A,B,C"));
        Assert.Throws<ArgumentException>(() => new FrameworkName("A,Version=1.0.0.0,C"));
        Assert.Throws<ArgumentException>(() => new FrameworkName("A,1.0.0.0,Profile=C"));
        Assert.Throws<ArgumentException>(() => new FrameworkName("A,Profile=C"));
    }

    [Fact]
    public static void ConstructFromIdentifierVersion()
    {
        VerifyConstructor(new FrameworkName(TestIdentifier, s_testVersion),
            s_testNameNoProfileString, TestIdentifier, s_testVersion, string.Empty);
    }

    [Fact]
    public static void ConstructFromInvalidIdentifierVersion()
    {
        Assert.Throws<ArgumentNullException>(() => new FrameworkName(null, s_testVersion));
        Assert.Throws<ArgumentException>(() => new FrameworkName(string.Empty, s_testVersion));
        Assert.Throws<ArgumentException>(() => new FrameworkName("   \r\n\t", s_testVersion));

        Assert.Throws<ArgumentNullException>(() => new FrameworkName(TestIdentifier, null));
    }

    [Fact]
    public static void ConstructFromIdentifierVersionProfile()
    {
        VerifyConstructor(new FrameworkName(TestIdentifier, s_testVersion, TestProfile),
            s_testNameString, TestIdentifier, s_testVersion, TestProfile);

        VerifyConstructor(new FrameworkName(TestIdentifier, s_testVersion, null),
            s_testNameNoProfileString, TestIdentifier, s_testVersion, string.Empty);
    }

    [Fact]
    public static void ConstructFromInvalidIdentifierVersionProfile()
    {
        Assert.Throws<ArgumentNullException>(() => new FrameworkName(null, s_testVersion, TestProfile));
        Assert.Throws<ArgumentException>(() => new FrameworkName(string.Empty, s_testVersion, TestProfile));
        Assert.Throws<ArgumentException>(() => new FrameworkName("   \r\n\t", s_testVersion, TestProfile));

        Assert.Throws<ArgumentNullException>(() => new FrameworkName(TestIdentifier, null, TestProfile));
    }

    [Fact]
    public static void Equality()
    {
        VerifyEquality(s_testName, s_testName);
        VerifyEquality(s_testName, new FrameworkName(s_testNameString));
        VerifyEquality(s_testName, new FrameworkName(TestIdentifier, s_testVersion, TestProfile));
    }

    [Fact]
    public static void Inequality()
    {
        VerifyInequality(s_testName, null);
        VerifyInequality(s_testName, new FrameworkName("NotTheTestIdentifier", s_testVersion, TestProfile));
        VerifyInequality(s_testName, new FrameworkName(TestIdentifier, s_testVersion));
        VerifyInequality(s_testName, new FrameworkName(TestIdentifier, new Version(9, 8, 7, 6), TestProfile));
    }

    private static void VerifyConstructor(FrameworkName name, string expectedName, string expectedIdentifier, Version expectedVersion, string expectedProfile)
    {
        Assert.Equal(expectedName, name.FullName);
        Assert.Equal(expectedName, name.ToString());
        Assert.Equal(expectedIdentifier, name.Identifier);
        Assert.Equal(expectedProfile, name.Profile);
        Assert.Equal(expectedVersion, name.Version);
    }

    private static void VerifyInequality(FrameworkName x, FrameworkName y)
    {
        Assert.NotNull(x);

        Assert.True(x != y);
        Assert.False(x == y);
        Assert.False(x.Equals(y));
        Assert.False(((IEquatable<FrameworkName>)x).Equals(y));
    }

    private static void VerifyEquality(FrameworkName x, FrameworkName y)
    {
        Assert.NotNull(x);

        Assert.True(x == y);
        Assert.False(x != y);
        Assert.True(x.Equals(y));
        Assert.True(((IEquatable<FrameworkName>)x).Equals(y));
        Assert.Equal(x.GetHashCode(), y.GetHashCode());
    }
}
