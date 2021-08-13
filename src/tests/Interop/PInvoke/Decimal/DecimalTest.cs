// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

public class DecimalTest
{
    private const int StartingIntValue = 42;
    private const int NewIntValue = 18;

    public static int Main()
    {
        try
        {
            RunDecimalTests();
            RunLPDecimalTests();
            if (OperatingSystem.IsWindows())
            {
                RunCurrencyTests();
            }
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return 101;
        }
        return 100;
    }

    private static void RunDecimalTests()
    {
        Assert.AreEqual((decimal)StartingIntValue, DecimalTestNative.CreateDecimalFromInt(StartingIntValue));

        Assert.IsTrue(DecimalTestNative.DecimalEqualToInt((decimal)StartingIntValue, StartingIntValue));

        decimal localDecimal = (decimal)StartingIntValue;
        Assert.IsTrue(DecimalTestNative.ValidateAndChangeDecimal(ref localDecimal, StartingIntValue, NewIntValue));
        Assert.AreEqual((decimal)NewIntValue, localDecimal);

        DecimalTestNative.GetDecimalForInt(NewIntValue, out var dec);
        Assert.AreEqual((decimal)NewIntValue, dec);
        
        Assert.AreEqual((decimal)StartingIntValue, DecimalTestNative.CreateWrappedDecimalFromInt(StartingIntValue).dec);

        Assert.IsTrue(DecimalTestNative.WrappedDecimalEqualToInt(new DecimalTestNative.DecimalWrapper { dec = (decimal)StartingIntValue }, StartingIntValue));

        var localDecimalWrapper = new DecimalTestNative.DecimalWrapper { dec = (decimal)StartingIntValue };
        Assert.IsTrue(DecimalTestNative.ValidateAndChangeWrappedDecimal(ref localDecimalWrapper, StartingIntValue, NewIntValue));
        Assert.AreEqual((decimal)NewIntValue, localDecimalWrapper.dec);

        DecimalTestNative.GetWrappedDecimalForInt(NewIntValue, out var decWrapper);
        Assert.AreEqual((decimal)NewIntValue, decWrapper.dec);

        DecimalTestNative.PassThroughDecimalToCallback((decimal)NewIntValue, d => Assert.AreEqual((decimal)NewIntValue, d));  
    }

    private static void RunLPDecimalTests()
    {
        Assert.AreEqual((decimal)StartingIntValue, DecimalTestNative.CreateLPDecimalFromInt(StartingIntValue));

        Assert.IsTrue(DecimalTestNative.LPDecimalEqualToInt((decimal)StartingIntValue, StartingIntValue));

        decimal localDecimal = (decimal)StartingIntValue;
        Assert.IsTrue(DecimalTestNative.ValidateAndChangeLPDecimal(ref localDecimal, StartingIntValue, NewIntValue));
        Assert.AreEqual((decimal)NewIntValue, localDecimal);

        DecimalTestNative.GetLPDecimalForInt(NewIntValue, out var dec);
        Assert.AreEqual((decimal)NewIntValue, dec);

        DecimalTestNative.PassThroughLPDecimalToCallback((decimal)NewIntValue, d => Assert.AreEqual((decimal)NewIntValue, d));
    }

    private static void RunCurrencyTests()
    {        
        Assert.Throws<MarshalDirectiveException>(() => DecimalTestNative.CreateCurrencyFromInt(StartingIntValue));

        Assert.IsTrue(DecimalTestNative.CurrencyEqualToInt((decimal)StartingIntValue, StartingIntValue));

        decimal localCurrency = (decimal)StartingIntValue;
        Assert.IsTrue(DecimalTestNative.ValidateAndChangeCurrency(ref localCurrency, StartingIntValue, NewIntValue));
        Assert.AreEqual((decimal)NewIntValue, localCurrency);

        DecimalTestNative.GetCurrencyForInt(NewIntValue, out var cy);
        Assert.AreEqual((decimal)NewIntValue, cy);
        
        Assert.AreEqual((decimal)StartingIntValue, DecimalTestNative.CreateWrappedCurrencyFromInt(StartingIntValue).currency);

        Assert.IsTrue(DecimalTestNative.WrappedCurrencyEqualToInt(new DecimalTestNative.CurrencyWrapper { currency = (decimal)StartingIntValue }, StartingIntValue));

        var localCurrencyWrapper = new DecimalTestNative.CurrencyWrapper { currency = (decimal)StartingIntValue };
        Assert.IsTrue(DecimalTestNative.ValidateAndChangeWrappedCurrency(ref localCurrencyWrapper, StartingIntValue, NewIntValue));
        Assert.AreEqual((decimal)NewIntValue, localCurrencyWrapper.currency);

        DecimalTestNative.GetWrappedCurrencyForInt(NewIntValue, out var currencyWrapper);
        Assert.AreEqual((decimal)NewIntValue, currencyWrapper.currency);      
        
        DecimalTestNative.PassThroughCurrencyToCallback((decimal)NewIntValue, d => Assert.AreEqual((decimal)NewIntValue, d));
    }
}
