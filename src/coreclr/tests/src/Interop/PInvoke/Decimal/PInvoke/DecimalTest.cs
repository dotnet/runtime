// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

#pragma warning disable 618
[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
public struct Stru_Exp_DecAsCYAsFld
{
    [FieldOffset(0)]
    public char wc;

    [FieldOffset(8)]
    [MarshalAs(UnmanagedType.Currency)]
    public decimal cy;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct Stru_Seq_DecAsLPStructAsFld
{
    public double dblVal;

    public char cVal;

    [MarshalAs(UnmanagedType.LPStruct)]
    public decimal dec;
}

public struct NestedCurrency
{
    [MarshalAs(UnmanagedType.Currency)]
    public decimal dec;
}

public class CMain
{
    //DECIMAL
    [DllImport("DecNative")]
    static extern bool TakeDecAsInOutParamAsLPStructByRef([MarshalAs(UnmanagedType.LPStruct), In, Out] ref decimal dec);
    [DllImport("DecNative")]
    static extern bool TakeDecAsOutParamAsLPStructByRef([MarshalAs(UnmanagedType.LPStruct), Out] out decimal dec);
    [DllImport("DecNative")]
    [return: MarshalAs(UnmanagedType.LPStruct)]
    static extern decimal RetDec();

    //CY
    [DllImport("DecNative")]
    static extern bool TakeCYAsInOutParamAsLPStructByRef([MarshalAs(UnmanagedType.Currency), In, Out] ref decimal cy);
    [DllImport("DecNative")]
    static extern bool TakeCYAsOutParamAsLPStructByRef([MarshalAs(UnmanagedType.Currency), Out] out decimal cy);
    [DllImport("DecNative")]
    [return: MarshalAs(UnmanagedType.Currency)]
    static extern decimal RetCY();
    [DllImport("DecNative", EntryPoint = "RetCY")]
    static extern NestedCurrency RetCYStruct();
    [DllImport("DecNative")]
    static extern bool TakeStru_Exp_DecAsCYAsFldByInOutRef([Out] out Stru_Exp_DecAsCYAsFld s);

    static int fails = 0;
    static decimal CY_MAX_VALUE = 922337203685477.5807M;
    static decimal CY_MIN_VALUE = -922337203685477.5808M;

    static bool MarshalAsLPStruct()
    {
        Console.WriteLine("MarshalAsLPStruct started.");
        // DECIMAL
        decimal dec = decimal.MaxValue;
        if (!TakeDecAsInOutParamAsLPStructByRef(ref dec))
        {
            Console.WriteLine("Test Failed: TakeDecAsInOutParamAsLPStructByRef : Returned false");
            return false;
        }
        if (decimal.MinValue != dec)
        {
            Console.WriteLine($"Test Failed: Expected 'decimal.MinValue'. Got {dec}.");
            return false;
        }

        dec = decimal.Zero;
        if (!TakeDecAsOutParamAsLPStructByRef(out dec))
        {
            Console.WriteLine("Test Failed: TakeDecAsOutParamAsLPStructByRef : Returned false");
            return false;
        }
        if (decimal.MinValue != dec)
        {
            Console.WriteLine($"Test Failed: Expected 'decimal.MinValue'. Got {dec}.");
            return false;
        }

        dec = RetDec();
        if (dec != decimal.MaxValue)
        {
            Console.WriteLine($"Test Failed. Expected 'decimal.MaxValue'. Got {dec}");
        }

        Console.WriteLine("MarshalAsLPStruct end.");
        return true;
    }

    static bool MarshalAsCurrencyScenario()
    {
        Console.WriteLine("MarshalAsCurrencyScenario started.");
        //CY
        decimal cy = CY_MAX_VALUE;
        if (!TakeCYAsInOutParamAsLPStructByRef(ref cy))
        { 
            Console.WriteLine("Test Failed: TakeCYAsInOutParamAsLPStructByRef : Returned false");
            return false;
        }
        if (CY_MIN_VALUE != cy)
        { 
            Console.WriteLine($"Test Failed: Expected 'CY_MIN_VALUE'. Got {cy}.");
            return false;
        }

        cy = decimal.MaxValue;
        if (!TakeCYAsOutParamAsLPStructByRef(out cy))
        { 
            Console.WriteLine("Test Failed: TakeCYAsOutParamAsLPStructByRef : Returned false");
            return false;
        }
        if (CY_MIN_VALUE != cy)
        { 
            Console.WriteLine($"Test Failed: Expected 'CY_MIN_VALUE'. Got {cy}.");
            return false;
        }

        bool exceptionThrown = false;

        try
        {
            RetCY();
        }
        catch (MarshalDirectiveException)
        {
            exceptionThrown = true;
        }
        if (!exceptionThrown)
        {
            Console.WriteLine("Expected MarshalDirectiveException from RetCY() not thrown");
            return false;
        }

        cy = RetCYStruct().dec;
        if (cy != CY_MIN_VALUE)
        {
            Console.WriteLine($"Test Failed: RetCYStruct. Expected 'CY_MIN_VALUE'. Got '{cy}'.");
            return false;
        }

        Stru_Exp_DecAsCYAsFld s = new Stru_Exp_DecAsCYAsFld();
        s.cy = CY_MAX_VALUE;
        s.wc = 'I';
        if (!TakeStru_Exp_DecAsCYAsFldByInOutRef(out s))
        {
            Console.WriteLine("Test Failed: TakeStru_Exp_DecAsCYAsFldByInOutRef : Returned false");
            return false;
        }
        if (!TakeStru_Exp_DecAsCYAsFldByInOutRef(out s))
            if (CY_MAX_VALUE != s.cy)
            {
                Console.WriteLine($"Test Failed: Expected 'CY_MAX_VALUE'. Got {s.cy}.");
                return false;
            }
        if ('C' != s.wc)
        {
            Console.WriteLine($"Test Failed: Expected 'C'. Got {s.wc}.");
            return false;
        }

        Console.WriteLine("MarshalAsCurrencyScenario end.");
        return true;
    }

    static int Main()
    {
        bool success = true;
        success = success && MarshalAsLPStruct();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            success = success && MarshalAsCurrencyScenario();
        }

        return success ? 100 : 101;
    }
}
#pragma warning restore 618
