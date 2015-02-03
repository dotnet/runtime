// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

public class VersionTests
{
    [Fact]
    public void TestMajorMinorConstructor()
    {
        VerifyConstructor(0, 0);
        VerifyConstructor(2, 3);
        VerifyConstructor(int.MaxValue, int.MaxValue);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Version(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Version(0, -1));
    }

    private static void VerifyConstructor(int major, int minor)
    {
        var version = new Version(major, minor);
        VerifyVersion(version, major, minor, -1, -1);
    }

    [Fact]
    public void TestMajorMinorBuildConstructor()
    {
        VerifyConstructor(0, 0, 0);
        VerifyConstructor(2, 3, 4);
        VerifyConstructor(int.MaxValue, int.MaxValue, int.MaxValue);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Version(-1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Version(0, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Version(0, 0, -1));
    }

    private static void VerifyConstructor(int major, int minor, int build)
    {
        var version = new Version(major, minor, build);
        VerifyVersion(version, major, minor, build, -1);
    }

    [Fact]
    public void TestMajorMinorBuildRevisionConstructor()
    {
        VerifyConstructor(0, 0, 0, 0);
        VerifyConstructor(2, 3, 4, 7);
        VerifyConstructor(2, 3, 4, 65535);
        VerifyConstructor(2, 3, 4, 65536);
        VerifyConstructor(2, 3, 4, 32767);
        VerifyConstructor(2, 3, 4, 32768);
        VerifyConstructor(2, 3, 4, 2147483647);
        VerifyConstructor(2, 3, 4, 2147450879);
        VerifyConstructor(2, 3, 4, 2147418112);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Version(-1, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Version(0, -1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Version(0, 0, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Version(0, 0, 0, -1));
    }

    private static void VerifyConstructor(int major, int minor, int build, int revision)
    {
        var version = new Version(major, minor, build, revision);
        VerifyVersion(version, major, minor, build, revision);
    }

    private static void VerifyVersion(Version version, int major, int minor, int build, int revision)
    {
        Assert.Equal(major, version.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(build, version.Build);
        Assert.Equal(revision, version.Revision);
        Assert.Equal((short)(revision >> 16), version.MajorRevision);
        Assert.Equal((short)(revision & 0xFFFF), version.MinorRevision);
    }

    [Fact]
    public void CompareToObject()
    {
        var x = (IComparable)new Version(1, 2);
        Assert.Equal(1, x.CompareTo(null));
        Assert.Equal(0, x.CompareTo(new Version(1, 2)));
        Assert.True(x.CompareTo(new Version(1, 3)) < 0);
        Assert.True(x.CompareTo(new Version(2, 0)) < 0);
        Assert.True(x.CompareTo(new Version(1, 2, 1)) < 0);
        Assert.True(x.CompareTo(new Version(1, 2, 0, 1)) < 0);
        Assert.True(x.CompareTo(new Version(1, 0)) > 0);
        Assert.True(x.CompareTo(new Version(1, 0, 1)) > 0);
        Assert.True(x.CompareTo(new Version(1, 0, 0, 1)) > 0);
    }

    [Fact]
    public void TestCompareTo()
    {
        var x = new Version(1, 2);
        Assert.Equal(0, x.CompareTo(new Version(1, 2)));
        Assert.True(x.CompareTo(new Version(1, 3)) < 0);
        Assert.True(x.CompareTo(new Version(2, 0)) < 0);
        Assert.True(x.CompareTo(new Version(1, 2, 1)) < 0);
        Assert.True(x.CompareTo(new Version(1, 2, 0, 1)) < 0);
        Assert.True(x.CompareTo(new Version(1, 0)) > 0);
        Assert.True(x.CompareTo(new Version(1, 0, 1)) > 0);
        Assert.True(x.CompareTo(new Version(1, 0, 0, 1)) > 0);
    }

    [Fact]
    public void TestEqualsObject()
    {
        var x = new Version(2, 3);
        Assert.Equal(x, (object)x);
        var y = (object)x;
        Assert.Equal(x, y);

        y = (object)new Version(2, 4);
        Assert.NotEqual(x, y);
    }

    [Fact]
    public void TestEquals()
    {
        var x = new Version(2, 3);
        Assert.Equal(x, x);
        var y = new Version(2, 3);
        Assert.Equal(x, y);
        Assert.NotEqual(x, new Version(2, 4));
    }

    [Fact]
    public void TestGetHashCode()
    {
        var x = new Version(2, 3);
        var y = new Version(2, 3);
        Assert.Equal(x.GetHashCode(), y.GetHashCode());

        x = new Version(2, 3, 4);
        y = new Version(2, 3, 4);
        Assert.Equal(x.GetHashCode(), y.GetHashCode());

        x = new Version(2, 3, 4, 0x000E000F);
        y = new Version(2, 3, 4, 0x000E000F);
        Assert.Equal(x.GetHashCode(), y.GetHashCode());

        x = new Version(5, 5);
        y = new Version(5, 4);
        Assert.NotEqual(x.GetHashCode(), y.GetHashCode());

        x = new Version(10, 10, 10);
        y = new Version(10, 10, 2);
        Assert.NotEqual(x.GetHashCode(), y.GetHashCode());

        x = new Version(10, 10, 10, 10);
        y = new Version(10, 10, 10, 3);
        Assert.NotEqual(x.GetHashCode(), y.GetHashCode());

        x = new Version(10, 10, 10, 10);
        y = new Version(10, 10);
        Assert.NotEqual(x.GetHashCode(), y.GetHashCode());
    }

    [Fact]
    public void TestToString()
    {
        Assert.Equal("1.2", new Version(1, 2).ToString());
        Assert.Equal("1.2.3", new Version(1, 2, 3).ToString());
        Assert.Equal("1.2.3.4", new Version(1, 2, 3, 4).ToString());
    }

    [Fact]
    public void TestToStringFieldCount()
    {
        var version = new Version(5, 3);
        Assert.Equal(string.Empty, version.ToString(0));
        Assert.Equal("5", version.ToString(1));
        Assert.Equal("5.3", version.ToString(2));
        Assert.Throws<ArgumentException>(() => version.ToString(3));
        Assert.Throws<ArgumentException>(() => version.ToString(4));
        Assert.Throws<ArgumentException>(() => version.ToString(5));
        Assert.Throws<ArgumentException>(() => version.ToString(-1));

        version = new Version(10, 11, 12);
        Assert.Equal(string.Empty, version.ToString(0));
        Assert.Equal("10", version.ToString(1));
        Assert.Equal("10.11", version.ToString(2));
        Assert.Equal("10.11.12", version.ToString(3));
        Assert.Throws<ArgumentException>(() => version.ToString(4));
        Assert.Throws<ArgumentException>(() => version.ToString(5));
        Assert.Throws<ArgumentException>(() => version.ToString(-1));

        version = new Version(1, 2, 3, 4);
        Assert.Equal(string.Empty, version.ToString(0));
        Assert.Equal("1", version.ToString(1));
        Assert.Equal("1.2", version.ToString(2));
        Assert.Equal("1.2.3", version.ToString(3));
        Assert.Equal("1.2.3.4", version.ToString(4));
        Assert.Throws<ArgumentException>(() => version.ToString(5));
        Assert.Throws<ArgumentException>(() => version.ToString(-1));
    }

    [Fact]
    public void TestParse()
    {
        Assert.Equal(new Version(1, 2), Version.Parse("1.2"));
        Assert.Equal(new Version(1, 2, 3), Version.Parse("1.2.3"));
        Assert.Equal(new Version(1, 2, 3, 4), Version.Parse("1.2.3.4"));
        Assert.Equal(new Version(2, 3, 4, 15), Version.Parse("2  .3.    4.  \t\r\n15  "));

        Assert.Throws<ArgumentException>(() => Version.Parse("1,2,3,4"));
        Assert.Throws<ArgumentException>(() => Version.Parse("1"));
        Assert.Throws<FormatException>(() => Version.Parse("1.b.3.4"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Version.Parse("1.-2"));
    }
}