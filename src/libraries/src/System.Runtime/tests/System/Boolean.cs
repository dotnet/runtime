// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Tests.Common;

using Xunit;

public static class BooleanTests
{
    [Fact]
    public static void TestTrueString()
    {
        Assert.Equal("True", bool.TrueString);
    }

    [Fact]
    public static void TestFalseString()
    {
        Assert.Equal("False", bool.FalseString);
    }

    [Theory]
    [InlineData("True", true, true)]
    [InlineData("true", true, true)]
    [InlineData("TRUE", true, true)]
    [InlineData("tRuE", true, true)]
    [InlineData("  True  ", true, true)]
    [InlineData("True\0", true, true)]
    [InlineData(" \0 \0  True   \0 ", true, true)]

    [InlineData("False", false, true)]
    [InlineData("false", false, true)]
    [InlineData("FALSE", false, true)]
    [InlineData("fAlSe", false, true)]
    [InlineData("False  ", false, true)]
    [InlineData("False\0", false, true)]
    [InlineData("  False \0\0\0  ", false, true)]
    
    [InlineData(null, default(bool), false)]
    [InlineData("", default(bool), false)]
    [InlineData(" ", default(bool), false)]
    [InlineData("Garbage", default(bool), false)]
    [InlineData("True\0Garbage", default(bool), false)]
    [InlineData("True\0True", default(bool), false)]
    [InlineData("True True", default(bool), false)]
    [InlineData("True False", default(bool), false)]
    [InlineData("False True", default(bool), false)]
    [InlineData("Fa lse", default(bool), false)]
    [InlineData("T", default(bool), false)]
    [InlineData("0", default(bool), false)]
    [InlineData("1", default(bool), false)]
    public static void TestParse(string value, bool expectedResult, bool shouldSucceed)
    {
        //TryParse
        bool result;
        Assert.Equal(shouldSucceed, bool.TryParse(value, out result));
        Assert.Equal(expectedResult, result);

        //Parse
        if (shouldSucceed)
        {
            Assert.Equal(expectedResult, bool.Parse(value));
        }
        else if (value == null)
        {
            Assert.Throws<ArgumentNullException>("value", () => bool.Parse(value));
        }
        else
        {
            Assert.Throws<FormatException>(() => bool.Parse(value));
        }
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, "1", false)]
    [InlineData(true, "True", false)]
    [InlineData(true, null, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, "0", false)]
    [InlineData(false, "False", false)]
    [InlineData(false, null, false)]
    public static void TestEquals(bool b1, object b2, bool expected)
    {
        if (b2 is bool)
        {
            Assert.Equal(expected, b1.Equals((bool)b2));
        }
        Assert.Equal(expected, b1.Equals(b2));
    }

    [Fact]
    public static void TestGetHashCode()
    {
        Assert.Equal(1, true.GetHashCode());
        Assert.Equal(0, false.GetHashCode());
    }

    [Fact]
    public static void TestToString()
    {
        Assert.Equal(bool.TrueString, true.ToString());
        Assert.Equal(bool.FalseString, false.ToString());
    }

    [Theory]
    [InlineData(true, true, 0)]
    [InlineData(true, false, 1)]
    [InlineData(true, null, 1)]
    [InlineData(false, false, 0)]
    [InlineData(false, true, -1)]
    [InlineData(false, null, 0)]
    public static void TestCompareTo(bool b1, bool b2, int expected)
    {
        int i = CompareHelper.NormalizeCompare(b1.CompareTo(b2));
        Assert.Equal(expected, i);
    }

    [Theory]
    [InlineData(true, true, 0)]
    [InlineData(true, false, 1)]
    [InlineData(true, null, 1)]
    [InlineData(false, false, 0)]
    [InlineData(false, true, -1)]
    [InlineData(false, null, 1)]
    public static void TestIComparableCompareTo(IComparable b1, IComparable b2, int expected)
    {
        int i = CompareHelper.NormalizeCompare(b1.CompareTo(b2));
        Assert.Equal(expected, i);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(true, "true")]
    [InlineData(false, 0)]
    [InlineData(false, "false")]
    private static void VerifyCompareToFailures(IComparable b, object obj)
    {
        Assert.Throws<ArgumentException>(null, () => b.CompareTo(obj));
    }
}
