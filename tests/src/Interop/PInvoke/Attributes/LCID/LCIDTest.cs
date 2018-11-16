// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//The testcase focus test the BStr with embed null string
using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;

class LCIDTest
{
    [DllImport(@"LCIDNative.dll", EntryPoint = "MarshalStringBuilder_LCID_As_First_Argument")]
    [LCIDConversionAttribute(0)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    private static extern StringBuilder MarshalStringBuilder_LCID_As_First_Argument([In, Out][MarshalAs(UnmanagedType.LPStr)]StringBuilder s);

    [DllImport(@"LCIDNative.dll", EntryPoint = "MarshalStringBuilder_LCID_As_Last_Argument_SetLastError")]
    [LCIDConversionAttribute(1)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    private static extern StringBuilder MarshalStringBuilder_LCID_As_Last_Argument([In, Out][MarshalAs(UnmanagedType.LPStr)]StringBuilder s);

    [DllImport(@"LCIDNative.dll", EntryPoint = "MarshalStringBuilder_LCID_As_Last_Argument_SetLastError", SetLastError = true)]
    [LCIDConversionAttribute(1)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    private static extern StringBuilder MarshalStringBuilder_LCID_As_Last_Argument_SetLastError([In, Out][MarshalAs(UnmanagedType.LPStr)]StringBuilder s);

    [DllImport(@"LCIDNative.dll", EntryPoint = "MarshalStringBuilder_LCID_PreserveSig_SetLastError", PreserveSig = false, SetLastError = true)]
    [LCIDConversionAttribute(1)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    private static extern StringBuilder MarshalStringBuilder_LCID_PreserveSig_SetLastError([In, Out][MarshalAs(UnmanagedType.LPStr)]StringBuilder s);

    //LCID as first argument
    static bool Scenario1()
    {
        Console.WriteLine("Scenairo1 started");

        string strManaged = "Managed";
        StringBuilder expectedStrRet = new StringBuilder("a", 1);
        string strNative = " Native";
        StringBuilder strBNative = new StringBuilder(" Native", 7);

        StringBuilder strPara = new StringBuilder(strManaged, strManaged.Length);
        StringBuilder strRet = MarshalStringBuilder_LCID_As_First_Argument(strPara);

        if (expectedStrRet.ToString() != strRet.ToString())
        {
            Console.WriteLine("Method MarshalStringBuilder_LCID_As_First_Argument[Managed Side],The Return string is wrong");
            return false;
        }

        if (strBNative.ToString() != strPara.ToString())
        {
            Console.WriteLine("Method MarshalStringBuilder_LCID_As_First_Argument[Managed Side],The Passed string is wrong");
            return false;
        }

        Console.WriteLine("Scenairo1 success");
        return true;
    }

    //LCID as last argument
    static bool Scenario2()
    {
        Console.WriteLine("Scenairo2 started");

        string strManaged = "Managed";
        StringBuilder expectedStrRet = new StringBuilder("a", 1);
        string strNative = " Native";
        StringBuilder strBNative = new StringBuilder(" Native", 7);

        StringBuilder strPara = new StringBuilder(strManaged, strManaged.Length);
        StringBuilder strRet = MarshalStringBuilder_LCID_As_Last_Argument(strPara);

        if (expectedStrRet.ToString() != strRet.ToString())
        {
            Console.WriteLine("Method MarshalStringBuilder_LCID_As_Last_Argument[Managed Side],The Return string is wrong");
            return false;
        }

        if (strBNative.ToString() != strPara.ToString())
        {
            Console.WriteLine("Method MarshalStringBuilder_LCID_As_Last_Argument[Managed Side],The Passed string is wrong");
            return false;
        }

        //Verify that error value is set.
        int result = Marshal.GetLastWin32Error();
        if (result != 0)
        {
            Console.WriteLine("MarshalStringBuilder_LCID_As_Last_Argument: GetLasterror returned wrong error code");
            return false;
        }

        Console.WriteLine("Scenairo2 success");
        return true;
    }

    //SetLastError =true
    static bool Scenario3()
    {
        Console.WriteLine("Scenairo3 started");

        string strManaged = "Managed";
        StringBuilder expectedStrRet = new StringBuilder("a", 1);
        string strNative = " Native";
        StringBuilder strBNative = new StringBuilder(" Native", 7);

        StringBuilder strPara = new StringBuilder(strManaged, strManaged.Length);
        StringBuilder strRet = MarshalStringBuilder_LCID_As_Last_Argument_SetLastError(strPara);

        if (expectedStrRet.ToString() != strRet.ToString())
        {
            Console.WriteLine("Method MarshalStringBuilder_LCID_As_Last_Argument_SetLastError[Managed Side],The Return string is wrong");
            return false;
        }

        if (strBNative.ToString() != strPara.ToString())
        {
            Console.WriteLine("Method MarshalStringBuilder_LCID_As_Last_Argument_SetLastError[Managed Side],The Passed string is wrong");
            return false;
        }

        //Verify that error value is set.
        int result = Marshal.GetLastWin32Error();
        if (result != 1090)
        {
            Console.WriteLine("MarshalStringBuilder_LCID_As_Last_Argument_SetLastError: GetLasterror returned wrong error code");
            return false;
        }

        Console.WriteLine("Scenairo3 success");
        return true;
    }

    //PreserveSig = false, SetLastError = true
    static bool Scenario4()
    {
        Console.WriteLine("Scenairo4 started");

        string strManaged = "Managed";
        StringBuilder expectedStrRet = new StringBuilder("a", 1);
        string strNative = " Native";
        StringBuilder strBNative = new StringBuilder(" Native", 7);

        StringBuilder strPara = new StringBuilder(strManaged, strManaged.Length);
        StringBuilder strRet = MarshalStringBuilder_LCID_PreserveSig_SetLastError(strPara);


        if (expectedStrRet.ToString() != strRet.ToString())
        {
            Console.WriteLine("Method MarshalStringBuilder_LCID_As_Last_Argument_SetLastError[Managed Side],The Return string is wrong");
            return false;
        }

        if (strBNative.ToString() != strPara.ToString())
        {
            Console.WriteLine("Method MarshalStringBuilder_LCID_As_Last_Argument_SetLastError[Managed Side],The Passed string is wrong");
            return false;
        }

        //Verify that error value is set.
        int result = Marshal.GetLastWin32Error();
        if (result != 1090)
        {
            Console.WriteLine("MarshalStringBuilder_LCID_As_Last_Argument_SetLastError: GetLasterror returned wrong error code");
            return false;
        }
        
        Console.WriteLine("Scenairo4 success");
        return true;
    }

    public static int Main(string[] args)
    {
        var success = true;
        success = success && Scenario1();
        success = success && Scenario2();
        success = success && Scenario3();
        success = success && Scenario4();

        return success ? 100 : 101;
    }
}
