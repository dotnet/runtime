// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

#pragma warning disable 618
[StructLayout(LayoutKind.Sequential)]
public struct Stru_Seq_DecAsStructAsFld
{
    public int number;

    [MarshalAs(UnmanagedType.Struct)]
    public decimal dec;
}

[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
public struct Stru_Exp_DecAsCYAsFld
{
    [FieldOffset(0)]
    public char wc;

    [FieldOffset(8)]
    [MarshalAs(UnmanagedType.Currency)]
    public decimal dec;
}

public class CMain
{
    #region Func Sig   

    // Dec As Struct
    [DllImport("RevNative")]
    static extern bool ReverseCall_TakeDecByInOutRef([MarshalAs(UnmanagedType.FunctionPtr)] Dele_DecInOutRef dele);
    [DllImport("RevNative")]
    static extern bool ReverseCall_TakeDecByOutRef([MarshalAs(UnmanagedType.FunctionPtr)] Dele_DecOutRef dele);
    [DllImport("RevNative")]
    static extern bool ReverseCall_DecRet([MarshalAs(UnmanagedType.FunctionPtr)] Dele_DecRet dele);
    [DllImport("RevNative")]
    static extern bool ReverseCall_TakeStru_Seq_DecAsStructAsFldByInOutRef([MarshalAs(UnmanagedType.FunctionPtr)] Dele_Stru_Seq_DecAsStructAsFldInOutRef dele);

    // Dec As CY
    [DllImport("RevNative")]
    static extern bool ReverseCall_TakeCYByInOutRef([MarshalAs(UnmanagedType.FunctionPtr)] Dele_CYInOutRef dele);
    [DllImport("RevNative")]
    static extern bool ReverseCall_TakeCYByOutRef([MarshalAs(UnmanagedType.FunctionPtr)] Dele_CYOutRef dele);
    [DllImport("RevNative")]
    static extern bool ReverseCall_CYRet([MarshalAs(UnmanagedType.FunctionPtr)] Dele_CYRet dele);
    [DllImport("RevNative")]
    static extern bool ReverseCall_TakeStru_Exp_DecAsCYAsFldByOutRef([MarshalAs(UnmanagedType.FunctionPtr)] Dele_Stru_Exp_DecAsCYAsFldOutRef dele);

    // Dec As LPStruct
    [DllImport("RevNative")]
    static extern bool ReverseCall_TakeDecByInOutRefAsLPStruct([MarshalAs(UnmanagedType.FunctionPtr)] Dele_DecInOutRefAsLPStruct dele);
    [DllImport("RevNative")]
    static extern bool ReverseCall_TakeDecByOutRefAsLPStruct([MarshalAs(UnmanagedType.FunctionPtr)] Dele_DecOutRefAsLPStruct dele);
    //************** ReverseCall Return Int From Net **************//
    [DllImport("RevNative")]
    static extern bool ReverseCall_IntRet([MarshalAs(UnmanagedType.FunctionPtr)] Dele_IntRet dele);

    #endregion

    #region Delegate Set

    // Dec As Struct
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate bool Dele_DecInOutRef([MarshalAs(UnmanagedType.Struct), In, Out]ref decimal dec);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate bool Dele_DecOutRef([MarshalAs(UnmanagedType.Struct), Out]out decimal dec);
    [return: MarshalAs(UnmanagedType.Struct)]
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate decimal Dele_DecRet();
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate bool Dele_Stru_Seq_DecAsStructAsFldInOutRef([In, Out]ref Stru_Seq_DecAsStructAsFld s);

    // Dec As CY
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate bool Dele_CYInOutRef([MarshalAs(UnmanagedType.Currency), In, Out]ref decimal dec);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate bool Dele_CYOutRef([MarshalAs(UnmanagedType.Currency), Out]out decimal dec);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Currency)]
    delegate decimal Dele_CYRet();
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate bool Dele_Stru_Exp_DecAsCYAsFldOutRef([Out]out Stru_Exp_DecAsCYAsFld s);

    // Dec As LPStruct
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate bool Dele_DecInOutRefAsLPStruct([MarshalAs(UnmanagedType.LPStruct), In, Out]ref decimal dec);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate bool Dele_DecOutRefAsLPStruct([MarshalAs(UnmanagedType.LPStruct), Out]out decimal dec);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.LPStruct)]
    delegate decimal Dele_DecAsLPStructRet();

    //************** ReverseCall Return Int From Net **************//
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate int Dele_IntRet();

    #endregion

    #region AUX For Testing
    const decimal CY_MAX_VALUE = 922337203685477.5807M;
    const decimal CY_MIN_VALUE = -922337203685477.5808M;

    static bool Equals<T>(T expected, T actual)
    {
        if (expected.Equals(actual))
            return true;
        else
            return false;
    }

    #endregion

    #region Method For Testing

    //Dec As Struct
    static bool TakeDecByInOutRef([MarshalAs(UnmanagedType.Struct), In, Out] ref decimal dec)
    {
        if (Equals(decimal.MaxValue, dec))
        {
            dec = decimal.MinValue;
            return true;
        }
        else
            return false;
    }

    static bool TakeDecByOutRef([MarshalAs(UnmanagedType.Struct), Out] out decimal dec)
    {
        dec = decimal.Zero;

        return true;
    }

    [return: MarshalAs(UnmanagedType.Struct)]
    static decimal DecRet()
    {
        return decimal.MinValue;
    }

    static bool TakeStru_Seq_DecAsStructAsFldByInOutRef([In, Out] ref Stru_Seq_DecAsStructAsFld s)
    {
        if (Equals(decimal.MaxValue, s.dec) && Equals(1, s.number))
        {
            s.dec = decimal.MinValue;
            s.number = 2;
            return true;
        }
        else
            return false;
    }

    // Dec As CY
    static bool TakeCYByInOutRef([MarshalAs(UnmanagedType.Currency), In, Out] ref decimal cy)
    {
        if (Equals(CY_MAX_VALUE, cy))
        {
            cy = CY_MIN_VALUE;
            return true;
        }
        else
            return false;
    }

    [return: MarshalAs(UnmanagedType.Currency)]
    static decimal CYRet()
    {
        return CY_MIN_VALUE;
    }

    static bool TakeCYByOutRef([MarshalAs(UnmanagedType.Currency), Out] out decimal dec)
    {
        dec = decimal.Zero;

        return true;
    }

    static bool ReverseCall_TakeStru_Exp_DecAsCYAsFldByOutRef([Out] out Stru_Exp_DecAsCYAsFld s)
    {
        s.dec = CY_MAX_VALUE;
        s.wc = 'C';

        return true;
    }

    //Dec As LPStruct
    static bool TakeDecByInOutRefAsLPStruct([MarshalAs(UnmanagedType.LPStruct), In, Out] ref decimal dec)
    {
        if (Equals(decimal.MaxValue, dec))
        {
            dec = decimal.MinValue;
            return true;
        }
        else
            return false;
    }

    static bool TakeDecByOutRefAsLPStruct([MarshalAs(UnmanagedType.LPStruct), Out] out decimal dec)
    {
        dec = decimal.Zero;

        return true;
    }

    [return: MarshalAs(UnmanagedType.LPStruct)]
    static decimal DecAsLPStructRet()
    {
        return decimal.MinValue;
    }

    //************** ReverseCall Return Int From Net **************//
    [return: MarshalAs(UnmanagedType.I4)]
    static int IntRet()
    {
        return 0x12345678;
    }

    #endregion

    static bool AsStruct()
    {
        Console.WriteLine("AsStruct started.");
        // Dec As Struct
        if (!ReverseCall_TakeDecByInOutRef(new Dele_DecInOutRef(TakeDecByInOutRef)))
        {
            Console.WriteLine("Test Failed: Decimal <-> DECIMAL, Marshal As Struct/Param, Passed By In / Out / Ref .");
            return false;
        }
        if (!ReverseCall_TakeDecByOutRef(new Dele_DecOutRef(TakeDecByOutRef)))
        {
            Console.WriteLine("Test Failed: Decimal <-> DECIMAL, Marshal As Struct/Param, Passed By Out / Ref .");
            return false;
        }
        if (!ReverseCall_DecRet(new Dele_DecRet(DecRet)))
        {
            Console.WriteLine("Test Failed: Decimal <-> DECIMAL, Marshal As Struct/RetVal .");
            return false;
        }
        if (!ReverseCall_TakeStru_Seq_DecAsStructAsFldByInOutRef(new Dele_Stru_Seq_DecAsStructAsFldInOutRef(TakeStru_Seq_DecAsStructAsFldByInOutRef)))
        {
            Console.WriteLine("Test Failed: Decimal <-> DECIMAL, Marshal As Struct/Field, Passed By In / Out / Ref .");
            return false;
        }

        Console.WriteLine("AsStruct end.");
        return true;
    }

    static bool AsCY()
    {
        Console.WriteLine("AsCY started.");
        // Dec As CY
        if (!(ReverseCall_TakeCYByInOutRef(new Dele_CYInOutRef(TakeCYByInOutRef))))
        {
            Console.WriteLine("Test Failed: (ReverseCall_TakeCYByInOutRef(new Dele_CYInOutRef(TakeCYByInOutRef)))");
            return false;
        }
        if (!(ReverseCall_TakeCYByOutRef(new Dele_CYOutRef(TakeCYByOutRef))))
        {
            Console.WriteLine("Test Failed: (ReverseCall_TakeCYByOutRef(new Dele_CYOutRef(TakeCYByOutRef)))");
            return false;
        }

        bool exceptionThrown = false;

        try
        {
            ReverseCall_CYRet(new Dele_CYRet(CYRet));
        }
        catch (MarshalDirectiveException)
        {
            exceptionThrown = true;
        }
        if (!exceptionThrown)
        {
            Console.WriteLine("Expected MarshalDirectiveException from ReverseCall_CYRet(new Dele_CYRet(CYRet)) not thrown");
            return false;
        }

        if (!(ReverseCall_TakeStru_Exp_DecAsCYAsFldByOutRef(new Dele_Stru_Exp_DecAsCYAsFldOutRef(ReverseCall_TakeStru_Exp_DecAsCYAsFldByOutRef))))
        {
            Console.WriteLine("Test Failed: (ReverseCall_TakeStru_Exp_DecAsCYAsFldByOutRef(new Dele_Stru_Exp_DecAsCYAsFldOutRef(ReverseCall_TakeStru_Exp_DecAsCYAsFldByOutRef)))");
            return false;
        }

        Console.WriteLine("AsCY end.");
        return true;
    }

    static bool AsLPStruct()
    {
        Console.WriteLine("AsLPStruct started.");

        if (!ReverseCall_TakeDecByInOutRefAsLPStruct(new Dele_DecInOutRefAsLPStruct(TakeDecByInOutRefAsLPStruct)))
        {
            Console.WriteLine("Test Failed: Decimal <-> DECIMAL, Marshal As LPStruct/Param, Passed By In / Out / Ref");
            return false;
        }
        if (!ReverseCall_TakeDecByOutRefAsLPStruct(new Dele_DecOutRefAsLPStruct(TakeDecByOutRefAsLPStruct)))
        {
            Console.WriteLine("Test Failed: Decimal <-> DECIMAL, Marshal As LPStruct/Param, Passed By Out / Ref");
            return false;
        }


        Console.WriteLine("AsLPStruct end.");
        return true;
    }

    static bool AsInt()
    {
        if (!ReverseCall_IntRet(new Dele_IntRet(IntRet)))
        {
            Console.WriteLine("Test Failed: RET INT");
            return false;
        }

        return true;
    }

    static int Main()
    {
        bool success = true;
        success = success && AsStruct();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            success = success && AsCY();
        }
        success = success && AsLPStruct();
        success = success && AsInt();

        return success ? 100 : 101;
    }
}
#pragma warning restore 618
