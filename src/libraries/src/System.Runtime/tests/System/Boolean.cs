// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

public static class BooleanTests
{
    private static void VerifyBooleanTryParse(string s, bool expectedResult, bool expectedReturn)
    {
        Boolean b;
        Assert.Equal(expectedReturn, Boolean.TryParse(s, out b));
        Assert.Equal(expectedResult, b);
    }
    [Fact]
    public static void TestTryParse()
    {
        // Boolean Boolean.TryParse(String, Boolean)

        // Success cases
        VerifyBooleanTryParse(Boolean.TrueString, true, true);
        VerifyBooleanTryParse(Boolean.FalseString, false, true);
        VerifyBooleanTryParse("True", true, true);
        VerifyBooleanTryParse("true", true, true);
        VerifyBooleanTryParse("TRUE", true, true);
        VerifyBooleanTryParse("tRuE", true, true);
        VerifyBooleanTryParse("False", false, true);
        VerifyBooleanTryParse("false", false, true);
        VerifyBooleanTryParse("FALSE", false, true);
        VerifyBooleanTryParse("fAlSe", false, true);
        VerifyBooleanTryParse("  True  ", true, true);
        VerifyBooleanTryParse("False  ", false, true);
        VerifyBooleanTryParse("True\0", true, true);
        VerifyBooleanTryParse("False\0", false, true);
        VerifyBooleanTryParse("True\0    ", true, true);
        VerifyBooleanTryParse(" \0 \0  True   \0 ", true, true);
        VerifyBooleanTryParse("  False \0\0\0  ", false, true);

        // Fail cases
        VerifyBooleanTryParse(null, false, false);
        VerifyBooleanTryParse("", false, false);
        VerifyBooleanTryParse(" ", false, false);
        VerifyBooleanTryParse("Garbage", false, false);
        VerifyBooleanTryParse("True\0Garbage", false, false);
        VerifyBooleanTryParse("True\0True", false, false);
        VerifyBooleanTryParse("True True", false, false);
        VerifyBooleanTryParse("True False", false, false);
        VerifyBooleanTryParse("False True", false, false);
        VerifyBooleanTryParse("Fa lse", false, false);
        VerifyBooleanTryParse("T", false, false);
        VerifyBooleanTryParse("0", false, false);
        VerifyBooleanTryParse("1", false, false);
    }

    private static void VerifyBooleanParse(string s, bool expectedResult, bool shouldSucceed)
    {
        Boolean b;
        Boolean succeeded;
        try
        {
            b = Boolean.Parse(s);
            Assert.Equal(expectedResult, b);
            succeeded = true;
        }
        catch (Exception)
        {
            succeeded = false;
        }
        Assert.Equal(shouldSucceed, succeeded);
    }

    [Fact]
    public static void TestParse()
    {
        // Boolean Boolean.Parse(String)
        // Success cases
        VerifyBooleanParse(Boolean.TrueString, true, true);
        VerifyBooleanParse(Boolean.FalseString, false, true);
        VerifyBooleanParse("True", true, true);
        VerifyBooleanParse("true", true, true);
        VerifyBooleanParse("TRUE", true, true);
        VerifyBooleanParse("tRuE", true, true);
        VerifyBooleanParse("False", false, true);
        VerifyBooleanParse("false", false, true);
        VerifyBooleanParse("FALSE", false, true);
        VerifyBooleanParse("fAlSe", false, true);
        VerifyBooleanParse("  True  ", true, true);
        VerifyBooleanParse("False  ", false, true);
        VerifyBooleanParse("True\0", true, true);
        VerifyBooleanParse("False\0", false, true);
        VerifyBooleanParse("True\0    ", true, true);
        VerifyBooleanParse(" \0 \0  True   \0 ", true, true);
        VerifyBooleanParse("  False \0\0\0  ", false, true);

        // Fail cases
        VerifyBooleanParse(null, false, false);
        VerifyBooleanParse("", false, false);
        VerifyBooleanParse(" ", false, false);
        VerifyBooleanParse("Garbage", false, false);
        VerifyBooleanParse("True\0Garbage", false, false);
        VerifyBooleanParse("True\0True", false, false);
        VerifyBooleanParse("True True", false, false);
        VerifyBooleanParse("True False", false, false);
        VerifyBooleanParse("False True", false, false);
        VerifyBooleanParse("Fa lse", false, false);
        VerifyBooleanParse("T", false, false);
        VerifyBooleanParse("0", false, false);
        VerifyBooleanParse("1", false, false);
    }

    [Fact]
    public static void TestFalseString()
    {
        // String Boolean.FalseString
        Assert.Equal("False", Boolean.FalseString);
    }

    [Fact]
    public static void TestEqualsObject()
    {
        // Boolean Boolean.Equals(Object)
        Boolean bTrue = true;
        Boolean bFalse = false;
        Assert.True(bTrue.Equals(true));
        Assert.True(bFalse.Equals(false));
        Assert.False(bTrue.Equals(false));
        Assert.False(bFalse.Equals(true));

        Assert.False(bTrue.Equals(1));
        Assert.False(bTrue.Equals("true"));
        Assert.False(bFalse.Equals(0));
        Assert.False(bFalse.Equals("False"));
        Assert.False(bTrue.Equals(null));
        Assert.False(bFalse.Equals(null));
    }

    [Fact]
    public static void TestEquals()
    {
        // Boolean Boolean.Equals(Boolean)
        Boolean bTrue = true;
        Boolean bFalse = false;
        Assert.True(bTrue.Equals(true));
        Assert.True(bFalse.Equals(false));
        Assert.False(bTrue.Equals(false));
        Assert.False(bFalse.Equals(true));
    }

    [Fact]
    public static void TestTrueString()
    {
        // String Boolean.TrueString
        Assert.Equal("True", Boolean.TrueString);
    }

    [Fact]
    public static void TestGetHashCode()
    {
        // Int32 Boolean.GetHashCode()
        Assert.Equal(1, true.GetHashCode());
        Assert.Equal(0, false.GetHashCode());
    }

    private static void VerifyCompareToFailures(IComparable b, object o)
    {
        Assert.True(b is Boolean);
        Assert.Throws<ArgumentException>(() => b.CompareTo(o));
    }

    [Fact]
    public static void TestCompareTo()
    {
        // Int32 Boolean.CompareTo(Boolean)
        Boolean bTrue = true;
        Boolean bFalse = false;
        Assert.True(bTrue.CompareTo(true) == 0);
        Assert.True(bFalse.CompareTo(false) == 0);
        Assert.False(bTrue.CompareTo(false) < 0);
        Assert.False(bFalse.CompareTo(true) > 0);
    }

    [Fact]
    public static void TestToString()
    {
        // String Boolean.ToString()
        Boolean bTrue = true;
        Boolean bFalse = false;
        Assert.Equal(Boolean.TrueString, bTrue.ToString());
        Assert.Equal(Boolean.FalseString, bFalse.ToString());
    }

    [Fact]
    public static void TestSystemIComparableCompareTo()
    {
        // Int32 Boolean.System.IComparable.CompareTo(Object)
        IComparable bTrue = true;
        IComparable bFalse = false;
        Assert.True(bTrue.CompareTo(true) == 0);
        Assert.True(bFalse.CompareTo(false) == 0);
        Assert.False(bTrue.CompareTo(false) < 0);
        Assert.False(bFalse.CompareTo(true) > 0);

        VerifyCompareToFailures(bTrue, 1);
        VerifyCompareToFailures(bTrue, "true");
        VerifyCompareToFailures(bFalse, 0);
        VerifyCompareToFailures(bFalse, "false");
        Assert.True(bTrue.CompareTo(null) > 0);
        Assert.True(bFalse.CompareTo(null) > 0);
    }
}
