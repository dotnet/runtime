// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

public static class GuidTests
{
    private static readonly Guid _testGuid = new Guid("a8a110d5-fc49-43c5-bf46-802db8f843ff");

    [Fact]
    public static void Testctor()
    {
        // Void Guid..ctor(Byte[])
        var g1 = new Guid(_testGuid.ToByteArray());
        Assert.Equal(_testGuid, g1);

        // Void Guid..ctor(Int32, Int16, Int16, Byte, Byte, Byte, Byte, Byte, Byte, Byte, Byte)
        var g2 = new Guid(unchecked((int)0xa8a110d5), unchecked((short)0xfc49), (short)0x43c5, 0xbf, 0x46, 0x80, 0x2d, 0xb8, 0xf8, 0x43, 0xff);
        Assert.Equal(_testGuid, g2);

        // Void Guid..ctor(Int32, Int16, Int16, Byte[])
        var g3 = new Guid(unchecked((int)0xa8a110d5), unchecked((short)0xfc49), (short)0x43c5, new byte[] { 0xbf, 0x46, 0x80, 0x2d, 0xb8, 0xf8, 0x43, 0xff });
        Assert.Equal(_testGuid, g3);

        // Void Guid..ctor(String)
        var g4 = new Guid("a8a110d5-fc49-43c5-bf46-802db8f843ff");
        Assert.Equal(_testGuid, g4);
    }

    [Fact]
    public static void TestEquals()
    {
        // Boolean Guid.Equals(Guid)
        Assert.True(_testGuid.Equals(_testGuid));
        Assert.False(_testGuid.Equals(Guid.Empty));
        Assert.False(Guid.Empty.Equals(_testGuid));

        // Boolean Guid.Equals(Object)
        Assert.False(_testGuid.Equals(null));
        Assert.False(_testGuid.Equals("a8a110d5-fc49-43c5-bf46-802db8f843ff"));

        // Boolean Guid.op_Equality(Guid, Guid)
        Assert.True(_testGuid == new Guid("a8a110d5-fc49-43c5-bf46-802db8f843ff"));
        Assert.True(Guid.Empty == new Guid(0, 0, 0, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }));

        // Boolean Guid.op_Inequality(Guid, Guid)
        Assert.True(_testGuid != Guid.Empty);
        Assert.True(Guid.Empty != _testGuid);
    }

    [Fact]
    public static void TestCompareTo()
    {
        // Int32 Guid.CompareTo(Guid)
        Assert.True(_testGuid.CompareTo(new Guid("98a110d5-fc49-43c5-bf46-802db8f843ff")) > 0);
        Assert.True(_testGuid.CompareTo(new Guid("a8a110d5-fc49-43c5-bf46-802db8f843ff")) == 0);
        Assert.True(_testGuid.CompareTo(new Guid("e8a110d5-fc49-43c5-bf46-802db8f843ff")) < 0);

        // Int32 Guid.System.IComparable.CompareTo(Object)
        IComparable icomp = _testGuid;

        Assert.True(icomp.CompareTo(new Guid("98a110d5-fc49-43c5-bf46-802db8f843ff")) > 0);
        Assert.True(icomp.CompareTo(new Guid("a8a110d5-fc49-43c5-bf46-802db8f843ff")) == 0);
        Assert.True(icomp.CompareTo(new Guid("e8a110d5-fc49-43c5-bf46-802db8f843ff")) < 0);

        Assert.True(icomp.CompareTo(null) > 0);

        Assert.Throws<ArgumentException>(() => icomp.CompareTo("a8a110d5-fc49-43c5-bf46-802db8f843ff"));
    }

    [Fact]
    public static void TestToByteArray()
    {
        // Byte[] Guid.ToByteArray()
        var bytes1 = new byte[] { 0xd5, 0x10, 0xa1, 0xa8, 0x49, 0xfc, 0xc5, 0x43, 0xbf, 0x46, 0x80, 0x2d, 0xb8, 0xf8, 0x43, 0xff };
        var bytes2 = _testGuid.ToByteArray();

        Assert.Equal(bytes1.Length, bytes2.Length);
        for (int i = 0; i < bytes1.Length; i++)
            Assert.Equal(bytes1[i], bytes2[i]);
    }

    [Fact]
    public static void TestEmpty()
    {
        // Guid Guid.Empty
        Assert.Equal(Guid.Empty, new Guid(0, 0, 0, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }));
    }

    [Fact]
    public static void TestNewGuid()
    {
        // Guid Guid.NewGuid()
        var g = Guid.NewGuid();
        Assert.NotEqual(Guid.Empty, g);

        var g2 = Guid.NewGuid();
        Assert.NotEqual(g, g2);
    }

    [Fact]
    public static void TestParse()
    {
        // Guid Guid.Parse(String)
        // Guid Guid.ParseExact(String, String)

        Assert.Equal(_testGuid, Guid.Parse("a8a110d5-fc49-43c5-bf46-802db8f843ff"));
        Assert.Equal(_testGuid, Guid.Parse("a8a110d5fc4943c5bf46802db8f843ff"));
        Assert.Equal(_testGuid, Guid.Parse("a8a110d5-fc49-43c5-bf46-802db8f843ff"));
        Assert.Equal(_testGuid, Guid.Parse("{a8a110d5-fc49-43c5-bf46-802db8f843ff}"));
        Assert.Equal(_testGuid, Guid.Parse("(a8a110d5-fc49-43c5-bf46-802db8f843ff)"));
        Assert.Equal(_testGuid, Guid.Parse("{0xa8a110d5,0xfc49,0x43c5,{0xbf,0x46,0x80,0x2d,0xb8,0xf8,0x43,0xff}}"));

        Assert.Equal(_testGuid, Guid.ParseExact("a8a110d5fc4943c5bf46802db8f843ff", "N"));
        Assert.Equal(_testGuid, Guid.ParseExact("a8a110d5-fc49-43c5-bf46-802db8f843ff", "D"));
        Assert.Equal(_testGuid, Guid.ParseExact("{a8a110d5-fc49-43c5-bf46-802db8f843ff}", "B"));
        Assert.Equal(_testGuid, Guid.ParseExact("(a8a110d5-fc49-43c5-bf46-802db8f843ff)", "P"));
        Assert.Equal(_testGuid, Guid.ParseExact("{0xa8a110d5,0xfc49,0x43c5,{0xbf,0x46,0x80,0x2d,0xb8,0xf8,0x43,0xff}}", "X"));
    }

    [Fact]
    public static void TestTryParse()
    {
        // Boolean Guid.TryParse(String, Guid)
        // Boolean Guid.TryParseExact(String, String, Guid)

        Guid g;
        Assert.True(Guid.TryParse("a8a110d5-fc49-43c5-bf46-802db8f843ff", out g));
        Assert.Equal(_testGuid, g);
        Assert.True(Guid.TryParse("a8a110d5fc4943c5bf46802db8f843ff", out g));
        Assert.Equal(_testGuid, g);
        Assert.True(Guid.TryParse("a8a110d5-fc49-43c5-bf46-802db8f843ff", out g));
        Assert.Equal(_testGuid, g);
        Assert.True(Guid.TryParse("{a8a110d5-fc49-43c5-bf46-802db8f843ff}", out g));
        Assert.Equal(_testGuid, g);
        Assert.True(Guid.TryParse("(a8a110d5-fc49-43c5-bf46-802db8f843ff)", out g));
        Assert.Equal(_testGuid, g);
        Assert.True(Guid.TryParse("{0xa8a110d5,0xfc49,0x43c5,{0xbf,0x46,0x80,0x2d,0xb8,0xf8,0x43,0xff}}", out g));
        Assert.Equal(_testGuid, g);

        Assert.True(Guid.TryParseExact("a8a110d5fc4943c5bf46802db8f843ff", "N", out g));
        Assert.Equal(_testGuid, g);
        Assert.True(Guid.TryParseExact("a8a110d5-fc49-43c5-bf46-802db8f843ff", "D", out g));
        Assert.Equal(_testGuid, g);
        Assert.True(Guid.TryParseExact("{a8a110d5-fc49-43c5-bf46-802db8f843ff}", "B", out g));
        Assert.Equal(_testGuid, g);
        Assert.True(Guid.TryParseExact("(a8a110d5-fc49-43c5-bf46-802db8f843ff)", "P", out g));
        Assert.Equal(_testGuid, g);
        Assert.True(Guid.TryParseExact("{0xa8a110d5,0xfc49,0x43c5,{0xbf,0x46,0x80,0x2d,0xb8,0xf8,0x43,0xff}}", "X", out g));
        Assert.Equal(_testGuid, g);

        Assert.False(Guid.TryParse("a8a110d5fc4943c5bf46802db8f843f", out g)); // One two few digits
        Assert.False(Guid.TryParseExact("a8a110d5-fc49-43c5-bf46-802db8f843ff", "N", out g)); // Contains '-' when "N" doesn't support those
    }

    [Fact]
    public static void TestGetHashCode()
    {
        // Int32 Guid.GetHashCode()
        Assert.NotEqual(_testGuid.GetHashCode(), Guid.Empty.GetHashCode());
    }

    [Fact]
    public static void TestToString()
    {
        // String Guid.ToString()
        // String Guid.ToString(String)
        // String Guid.System.IFormattable.ToString(String, IFormatProvider) // The IFormatProvider is ignored so don't need to test

        Assert.Equal(_testGuid.ToString(), "a8a110d5-fc49-43c5-bf46-802db8f843ff");
        Assert.Equal(_testGuid.ToString("N"), "a8a110d5fc4943c5bf46802db8f843ff");
        Assert.Equal(_testGuid.ToString("D"), "a8a110d5-fc49-43c5-bf46-802db8f843ff");
        Assert.Equal(_testGuid.ToString("B"), "{a8a110d5-fc49-43c5-bf46-802db8f843ff}");
        Assert.Equal(_testGuid.ToString("P"), "(a8a110d5-fc49-43c5-bf46-802db8f843ff)");
        Assert.Equal(_testGuid.ToString("X"), "{0xa8a110d5,0xfc49,0x43c5,{0xbf,0x46,0x80,0x2d,0xb8,0xf8,0x43,0xff}}");
    }
}
