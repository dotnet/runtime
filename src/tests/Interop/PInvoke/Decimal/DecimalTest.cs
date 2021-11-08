// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Xunit
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class SkipOnPlatformAttribute : Attribute
    {
        internal SkipOnPlatformAttribute() { }
        public SkipOnPlatformAttribute(TestPlatforms testPlatforms, string reason) { }
    }

    [Flags]
    public enum TestPlatforms
    {
        Windows = 1,
        Linux = 2,
        OSX = 4,
        FreeBSD = 8,
        NetBSD = 16,
        illumos= 32,
        Solaris = 64,
        iOS = 128,
        tvOS = 256,
        Android = 512,
        Browser = 1024,
        MacCatalyst = 2048,
        AnyUnix = FreeBSD | Linux | NetBSD | OSX | illumos | Solaris | iOS | tvOS | MacCatalyst | Android | Browser,
        Any = ~0
    }
}

public class DecimalTest
{
    private const int StartingIntValue = 42;
    private const int NewIntValue = 18;

    [Fact]
    public static void RunDecimalTests()
    {
        Assert.Equal((decimal)StartingIntValue, DecimalTestNative.CreateDecimalFromInt(StartingIntValue));

        Assert.True(DecimalTestNative.DecimalEqualToInt((decimal)StartingIntValue, StartingIntValue));

        decimal localDecimal = (decimal)StartingIntValue;
        Assert.True(DecimalTestNative.ValidateAndChangeDecimal(ref localDecimal, StartingIntValue, NewIntValue));
        Assert.Equal((decimal)NewIntValue, localDecimal);

        DecimalTestNative.GetDecimalForInt(NewIntValue, out var dec);
        Assert.Equal((decimal)NewIntValue, dec);

        Assert.Equal((decimal)StartingIntValue, DecimalTestNative.CreateWrappedDecimalFromInt(StartingIntValue).dec);

        Assert.True(DecimalTestNative.WrappedDecimalEqualToInt(new DecimalTestNative.DecimalWrapper { dec = (decimal)StartingIntValue }, StartingIntValue));

        var localDecimalWrapper = new DecimalTestNative.DecimalWrapper { dec = (decimal)StartingIntValue };
        Assert.True(DecimalTestNative.ValidateAndChangeWrappedDecimal(ref localDecimalWrapper, StartingIntValue, NewIntValue));
        Assert.Equal((decimal)NewIntValue, localDecimalWrapper.dec);

        DecimalTestNative.GetWrappedDecimalForInt(NewIntValue, out var decWrapper);
        Assert.Equal((decimal)NewIntValue, decWrapper.dec);

        DecimalTestNative.PassThroughDecimalToCallback((decimal)NewIntValue, d => Assert.Equal((decimal)NewIntValue, d));
    }

    [Fact]
    public static void RunLPDecimalTests()
    {
        Assert.Equal((decimal)StartingIntValue, DecimalTestNative.CreateLPDecimalFromInt(StartingIntValue));

        Assert.True(DecimalTestNative.LPDecimalEqualToInt((decimal)StartingIntValue, StartingIntValue));

        decimal localDecimal = (decimal)StartingIntValue;
        Assert.True(DecimalTestNative.ValidateAndChangeLPDecimal(ref localDecimal, StartingIntValue, NewIntValue));
        Assert.Equal((decimal)NewIntValue, localDecimal);

        DecimalTestNative.GetLPDecimalForInt(NewIntValue, out var dec);
        Assert.Equal((decimal)NewIntValue, dec);

        DecimalTestNative.PassThroughLPDecimalToCallback((decimal)NewIntValue, d => Assert.Equal((decimal)NewIntValue, d));
    }

    private static void RunCurrencyTests()
    {
        Assert.Throws<MarshalDirectiveException>(() => DecimalTestNative.CreateCurrencyFromInt(StartingIntValue));

        Assert.True(DecimalTestNative.CurrencyEqualToInt((decimal)StartingIntValue, StartingIntValue));

        decimal localCurrency = (decimal)StartingIntValue;
        Assert.True(DecimalTestNative.ValidateAndChangeCurrency(ref localCurrency, StartingIntValue, NewIntValue));
        Assert.Equal((decimal)NewIntValue, localCurrency);

        DecimalTestNative.GetCurrencyForInt(NewIntValue, out var cy);
        Assert.Equal((decimal)NewIntValue, cy);

        Assert.Equal((decimal)StartingIntValue, DecimalTestNative.CreateWrappedCurrencyFromInt(StartingIntValue).currency);

        Assert.True(DecimalTestNative.WrappedCurrencyEqualToInt(new DecimalTestNative.CurrencyWrapper { currency = (decimal)StartingIntValue }, StartingIntValue));

        var localCurrencyWrapper = new DecimalTestNative.CurrencyWrapper { currency = (decimal)StartingIntValue };
        Assert.True(DecimalTestNative.ValidateAndChangeWrappedCurrency(ref localCurrencyWrapper, StartingIntValue, NewIntValue));
        Assert.Equal((decimal)NewIntValue, localCurrencyWrapper.currency);

        DecimalTestNative.GetWrappedCurrencyForInt(NewIntValue, out var currencyWrapper);
        Assert.Equal((decimal)NewIntValue, currencyWrapper.currency);

        DecimalTestNative.PassThroughCurrencyToCallback((decimal)NewIntValue, d => Assert.Equal((decimal)NewIntValue, d));
    }
}
