// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;
using TestLibrary;
using Xunit;

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public class BestFitMapping
{

    #region "Imported Pinvoke Methods"

    #region "Cdecl Methods"
    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string CLPStr_In([In, MarshalAs(UnmanagedType.LPStr)]string str);

    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string CLPStr_Out([Out, MarshalAs(UnmanagedType.LPStr)]StringBuilder str);

    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string CLPStr_InOut([In, Out, MarshalAs(UnmanagedType.LPStr)]StringBuilder str);

    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string CLPStr_InByRef([In, MarshalAs(UnmanagedType.LPStr)]ref StringBuilder str);

    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string CLPStr_OutByRef([MarshalAs(UnmanagedType.LPStr)]out StringBuilder str);

    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string CLPStr_InOutByRef([In, Out, MarshalAs(UnmanagedType.LPStr)]ref StringBuilder str);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public delegate string DelegatePInvoke_Cdecl([In, Out, MarshalAs(UnmanagedType.LPStr)]ref StringBuilder str);

    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegatePInvoke_Cdecl CLPStr_DelegatePInvoke();

    #endregion

    #region "StdCall"
    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string SLPStr_In([In, MarshalAs(UnmanagedType.LPStr)]string str);

    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string SLPStr_Out([Out, MarshalAs(UnmanagedType.LPStr)]StringBuilder str);

    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string SLPStr_InOut([In, Out, MarshalAs(UnmanagedType.LPStr)]StringBuilder str);

    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string SLPStr_InByRef([In, MarshalAs(UnmanagedType.LPStr)]ref StringBuilder str);

    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string SLPStr_OutByRef([MarshalAs(UnmanagedType.LPStr)]out StringBuilder str);

    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string SLPStr_InOutByRef([In, Out, MarshalAs(UnmanagedType.LPStr)]ref StringBuilder str);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public delegate string DelegatePInvoke_StdCall([In, Out, MarshalAs(UnmanagedType.LPStr)]ref StringBuilder str);

    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegatePInvoke_StdCall SLPStr_DelegatePInvoke();
    #endregion

    #endregion

    #region "Imported Reverse Pinovke Methods"

    #region "CCallBackIn"
    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern void DoCCallBack_LPSTR_In(CCallBackIn callback);

    [return: MarshalAs(UnmanagedType.LPStr)]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate string CCallBackIn([In, MarshalAs(UnmanagedType.LPStr)]string str);

    #endregion

    #region "CCallBackOut"
    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern void DoCCallBack_LPSTR_Out(CCallBackOut callback);

    [return: MarshalAs(UnmanagedType.LPStr)]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate string CCallBackOut([Out, MarshalAs(UnmanagedType.LPStr)]StringBuilder str);

    #endregion

    #region "CCallBackInOut"
    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern void DoCCallBack_LPSTR_InOut(CCallBackInOut callback);

    [return: MarshalAs(UnmanagedType.LPStr)]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate string CCallBackInOut([In, Out, MarshalAs(UnmanagedType.LPStr)]StringBuilder str);

    #endregion

    #region "CCallBackInByRef"
    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern void DoCCallBack_LPSTR_InByRef(CCallBackInByRef callback);

    [return: MarshalAs(UnmanagedType.LPStr)]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate string CCallBackInByRef([In, MarshalAs(UnmanagedType.LPStr)]ref string str);

    #endregion

    #region "CCallBackOutByRef"
    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern void DoCCallBack_LPSTR_OutByRef(CCallBackOutByRef callback);

    [return: MarshalAs(UnmanagedType.LPStr)]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate string CCallBackOutByRef([MarshalAs(UnmanagedType.LPStr)]out string str);


    #endregion

    #region "CCallBackInOutByRef"
    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern void DoCCallBack_LPSTR_InOutByRef(CCallBackInOutByRef callback);

    [return: MarshalAs(UnmanagedType.LPStr)]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate string CCallBackInOutByRef([In, Out, MarshalAs(UnmanagedType.LPStr)]ref string str);


    #endregion

    #region "SCallBackIn"
    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern void DoSCallBack_LPSTR_In(SCallBackIn callback);

    [return: MarshalAs(UnmanagedType.LPStr)]
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate string SCallBackIn([In, MarshalAs(UnmanagedType.LPStr)]string str);


    #endregion

    #region "SCallBackOut"
    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern void DoSCallBack_LPSTR_Out(SCallBackOut callback);

    [return: MarshalAs(UnmanagedType.LPStr)]
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate string SCallBackOut([Out, MarshalAs(UnmanagedType.LPStr)]StringBuilder str);


    #endregion

    #region "SCallBackInOut"
    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern void DoSCallBack_LPSTR_InOut(SCallBackInOut callback);

    [return: MarshalAs(UnmanagedType.LPStr)]
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate string SCallBackInOut([In, Out, MarshalAs(UnmanagedType.LPStr)]StringBuilder str);


    #endregion

    #region "SCallBackInByRef"
    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern void DoSCallBack_LPSTR_InByRef(SCallBackInByRef callback);

    [return: MarshalAs(UnmanagedType.LPStr)]
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate string SCallBackInByRef([In, MarshalAs(UnmanagedType.LPStr)]ref string str);


    #endregion

    #region "SCallBackOutByRef"
    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern void DoSCallBack_LPSTR_OutByRef(SCallBackOutByRef callback);

    [return: MarshalAs(UnmanagedType.LPStr)]
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate string SCallBackOutByRef([MarshalAs(UnmanagedType.LPStr)]out string str);


    #endregion

    #region "SCallBackInOutByRef"
    [DllImport("BestFitMappingNative", BestFitMapping = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern void DoSCallBack_LPSTR_InOutByRef(SCallBackInOutByRef callback);

    [return: MarshalAs(UnmanagedType.LPStr)]
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate string SCallBackInOutByRef([In, Out, MarshalAs(UnmanagedType.LPStr)]ref string str);


    #endregion

    #endregion


    static StringBuilder GetInvalidString()
    {
        StringBuilder sbl = new StringBuilder();
        sbl.Append((char)0x263c);
        return sbl;
    }

    [DllImport("BestFitMappingNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern byte GetByteForWideChar();

    //DefaultEncoding
    //x86: Managed(Encoding: utf8)---->Marshaler(Encoding:ASCII)---->Native(Encoding:utf8)
    //MAC(x64):Managed(Encoding:utf8)----->Marshaler(Encoding:utf8)---->Native(Encoding:utf8)
    //Now  both side(Managed Side and native side) takes the utf8 encoding when comparing string

    static bool Compare(string lstr, string rstr)
    {
        //Windows
        if (TestLibrary.Utilities.IsWindows)
        {
            //Since the CoreCLR doesnt support Encoding.ASCII, so i have to hardcode this. or maybe get it from Native SIde
            //This value can be gotten through GetByteForWideChar()
            byte[] b = new byte[1];
            b[0] = GetByteForWideChar();

            if (b.Length != rstr.Length)
            {
                return false;
            }
            for (int i = 0; i < b.Length; ++i)
            {
                if (b[i] != rstr[i])
                {
                    Console.WriteLine("Compare:" + lstr);
                    Console.WriteLine("Compare:" + rstr);
                    return false;
                }
            }
            return true;
        }
        else //Mac
        {
            if (!lstr.Equals(rstr))
            {
                return false;
            }
            return true;
        }
    }

    //Result From Native Side, since the Return value and Parameter are used in TestScenario.
    //So have to use an extra variable to record errors occurred in Native Side.

    [DllImport("BestFitMappingNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetResult();

    private static int Fails = 0;
    #region "TestMethod"


    private static void TestMethod_CLPStr_In()
    {
        TestFramework.BeginScenario("Cdecl,LPStr,In");

        try
        {
            StringBuilder sb = GetInvalidString();
            string str = sb.ToString();
            string rstr = CLPStr_In(str);

            //Check the return value and the parameter.
            StringBuilder sbTemp = GetInvalidString();
            if (!Compare(str, rstr))
            {
                Fails++;
                TestFramework.LogError("001", "TestMethod_CLPStr_In");
            }
            if (!sb.Equals(sbTemp))
            {
                Fails++;
                TestFramework.LogError("002", "TestMethod_CLPStr_In");
            }

        }
        catch (Exception e)
        {
            Fails++;
            TestFramework.LogError("e01", "TestMethod_CLPStr_In:Unexpected Exception Occurred:" + e.ToString());
        }

    }


    private static void TestMethod_CLPStr_Out()
    {
        TestFramework.BeginScenario("Cdecl,LPStr,Out");
        try
        {
            StringBuilder sb = new StringBuilder();
            string rstr = CLPStr_Out(sb);

            string NativeString = "AAAA";
            //check the return value
            if (!NativeString.Equals(rstr))
            {
                Fails++;
                TestFramework.LogError("003", "TestMethod_CLPStr_Out:The Return value is wrong.");
            }
            //Check the Parameter
            if (!NativeString.Equals(sb.ToString()))
            {
                Fails++;
                TestFramework.LogError("004", "TestMethod_CLPStr_Out:The Parameter value is wrong.");
            }
        }
        catch (Exception e)
        {
            Fails++;
            TestFramework.LogError("e02", "TestMethod_CLPStr_Out:Unexpected Exception Occurred:" + e.ToString());
        }
    }


    private static void TestMethod_CLPStr_InOut()
    {
        TestFramework.BeginScenario("Cdecl,LPStr,InOut");
        try
        {

            StringBuilder sb = GetInvalidString();
            string rstr = CLPStr_InOut(sb);

            string sTemp = (GetInvalidString()).ToString();
            //Check the Parameter
            if (!Compare(sTemp, sb.ToString()))
            {
                Fails++;
                TestFramework.LogError("005", "TestMethod_CLPStr_InOut:The Parameter value is wrong.");
            }
            //Check the return value
            if (!Compare(sTemp, rstr))
            {
                Fails++;
                TestFramework.LogError("006", "TestMethod_CLPStr_InOut:The Return value is wrong.");
            }
        }
        catch (Exception e)
        {
            Fails++;
            TestFramework.LogError("e03", "TestMethod_CLPStr_InOut:Unexpected Exception Occurred:" + e.ToString());
        }
    }


    private static void TestMethod_CLPStr_InRef()
    {
        TestFramework.BeginScenario("Cdecl,LPStr,InRef");
        try
        {
            StringBuilder sb = GetInvalidString();
            string rstr = CLPStr_InByRef(ref sb);

            //Check the return value
            string sTemp = (GetInvalidString()).ToString();
            if (!Compare(sTemp, rstr))
            {
                Fails++;
                TestFramework.LogError("007", "TestMethod_CLPStr_InRef:The Return value is wrong.");
            }
            //Check the Parameter
            if (!sTemp.Equals(sb.ToString()))
            {
                Fails++;
                TestFramework.LogError("008", "TestMethod_CLPStr_InRef:The Parameter value is wrong.");
            }
        }
        catch (Exception e)
        {
            Fails++;
            TestFramework.LogError("e04", "TestMethod_CLPStr_InRef:Unexpected Exception Occurred:" + e.ToString());
        }
    }


    private static void TestMethod_CLPStr_OutRef()
    {
        TestFramework.BeginScenario("Cdecl,LPStr,OutRef");
        try
        {
            StringBuilder sb = new StringBuilder(10);
            string rstr = CLPStr_OutByRef(out sb);

            //Check the return value
            string sNative = "AAAA";
            if (!sNative.Equals(rstr))
            {
                Fails++;
                TestFramework.LogError("009", "TestMethod_CLPStr_OutRef:The Return value is wrong.");
            }
            //Check the Parameter
            if (!sNative.Equals(sb.ToString()))
            {
                Fails++;
                Console.WriteLine(sb.ToString());
                TestFramework.LogError("010", "TestMethod_CLPStr_OutRef:The Parameter value is wrong.");
            }
        }
        catch (Exception e)
        {
            Fails++;
            TestFramework.LogError("e05", "TestMethod_CLPStr_OutRef:Unexpected Exception Occurred:" + e.ToString());
        }
    }


    private static void TestMethod_CLPStr_InOutRef()
    {
        TestFramework.BeginScenario("Cdecl,LPStr,InOutRef");
        try
        {

            StringBuilder sb = GetInvalidString();
            string rstr = CLPStr_InOutByRef(ref sb);

            //Check the return value
            string sTemp = (GetInvalidString()).ToString();
            if (!Compare(sTemp, rstr))
            {
                Fails++;
                TestFramework.LogError("011", "TestMethod_CLPStr_InOutRef:The Return value is wrong.");
            }
            //Check the Parameter
            if (!Compare(sTemp, sb.ToString()))
            {
                Fails++;
                TestFramework.LogError("012", "TestMethod_CLPStr_InOutRef:The Parameter value is wrong.");
            }
        }
        catch (Exception e)
        {
            Fails++;
            TestFramework.LogError("e06", "TestMethod_CLPStr_InOutRef:Unexpected Exception Occurred:" + e.ToString());
        }
    }


    private static void TestMethod_CLPStr_DelegatePInvoke()
    {
        TestFramework.BeginScenario("Cdecl,LPStr,DelegatePInvoke");
        try
        {

            DelegatePInvoke_Cdecl caller = CLPStr_DelegatePInvoke();
            StringBuilder sb = GetInvalidString();
            string rstr = caller(ref sb);

            //Check the return value
            string sTemp = (GetInvalidString()).ToString();
            if (!Compare(sTemp, rstr))
            {
                Fails++;
                TestFramework.LogError("013", "TestMethod_CLPStr_DelegatePInvoke:The Return value is wrong.");
            }
            //Check the Parameter
            if (!Compare(sTemp, sb.ToString()))
            {
                Fails++;
                TestFramework.LogError("014", "TestMethod_CLPStr_DelegatePInvoke:The Parameter value is wrong.");
            }
        }
        catch (Exception e)
        {
            Fails++;
            TestFramework.LogError("e07", "TestMethod_CLPStr_DelegatePInvoke:Unexpected Exception Occurred:" + e.ToString());
        }
    }

    //Stdcall

    private static void TestMethod_SLPStr_In()
    {
        TestFramework.BeginScenario("StdCall,LPStr,In");
        try
        {

            StringBuilder sb = GetInvalidString();
            string rstr = SLPStr_In(sb.ToString());
            //Check the return value and the parameter.
            StringBuilder sbTemp = GetInvalidString();
            if (!Compare(sbTemp.ToString(), rstr))
            {
                Fails++;
                TestFramework.LogError("015", "TestMethod_SLPStr_In:The Return value is wrong.");
            }
            if (!sb.Equals(sbTemp))
            {
                Fails++;
                TestFramework.LogError("016", "TestMethod_SLPStr_In:The Parameter value is wrong.");
            }
        }
        catch (Exception e)
        {
            Fails++;
            TestFramework.LogError("e08", "TestMethod_SLPStr_In:Unexpected Exception Occurred:" + e.ToString());
        }
    }


    private static void TestMethod_SLPStr_Out()
    {
        TestFramework.BeginScenario("StdCall,LPStr,Out");

        try
        {

            StringBuilder sb = new StringBuilder();
            string rstr = SLPStr_Out(sb);

            string NativeString = "AAAA";
            //check the return value
            if (!NativeString.Equals(rstr))
            {
                Fails++;
                TestFramework.LogError("017", "TestMethod_SLPStr_Out:The Return value is wrong.");
            }
            //Check the Parameter
            if (!NativeString.Equals(sb.ToString()))
            {
                Fails++;
                TestFramework.LogError("018", "TestMethod_SLPStr_Out:The Parameter value is wrong.");
            }
        }
        catch (Exception e)
        {
            Fails++;
            TestFramework.LogError("e09", "TestMethod_SLPStr_Out:Unexpected Exception Occurred:" + e.ToString());
        }
    }


    private static void TestMethod_SLPStr_InOut()
    {
        TestFramework.BeginScenario("StdCall,LPStr,InOut");
        try
        {
            StringBuilder sb = GetInvalidString();
            string rstr = SLPStr_InOut(sb);

            string sTemp = (GetInvalidString()).ToString();
            //Check the Parameter
            if (!Compare(sTemp, sb.ToString()))
            {
                Fails++;
                TestFramework.LogError("019", "TestMethod_SLPStr_InOut:The Parameter value is wrong.");
            }
            //Check the return value
            if (!Compare(sTemp, rstr))
            {
                Fails++;
                TestFramework.LogError("020", "TestMethod_SLPStr_InOut:The Return value is wrong.");
            }
        }
        catch (Exception e)
        {
            Fails++;
            TestFramework.LogError("e10", "TestMethod_SLPStr_InOut:Unexpected Exception Occurred:" + e.ToString());
        }
    }


    private static void TestMethod_SLPStr_InRef()
    {
        TestFramework.BeginScenario("StdCall,LPStr,InRef");

        try
        {

            StringBuilder sb = GetInvalidString();
            string rstr = SLPStr_InByRef(ref sb);

            //Check the return value
            string sTemp = (GetInvalidString()).ToString();
            if (!Compare(sTemp, rstr))
            {
                Fails++;
                TestFramework.LogError("021", "TestMethod_SLPStr_InRef:The Return value is wrong.");
            }
            //Check the Parameter
            if (!sTemp.Equals(sb.ToString()))
            {
                Fails++;
                TestFramework.LogError("022", "TestMethod_SLPStr_InRef:The Parameters value is wrong.");
            }
        }
        catch (Exception e)
        {
            Fails++;
            TestFramework.LogError("e11", "TestMethod_SLPStr_InRef:Unexpected Exception Occurred:" + e.ToString());
        }
    }


    private static void TestMethod_SLPStr_OutRef()
    {
        TestFramework.BeginScenario("StdCall,LPStr,OutRef");

        try
        {

            StringBuilder sb = new StringBuilder(10);
            string rstr = SLPStr_OutByRef(out sb);

            //Check the return value
            string sNative = "AAAA";
            if (!sNative.Equals(rstr))
            {
                Fails++;
                TestFramework.LogError("023", "TestMethod_CLPStr_OutRef:The Return value is wrong.");
            }
            //Check the Parameter
            if (!sNative.Equals(sb.ToString()))
            {
                Fails++;
                Console.WriteLine(sb.ToString());
                TestFramework.LogError("024", "TestMethod_CLPStr_OutRef:The Parameter value is wrong.");
            }

        }
        catch (Exception e)
        {
            Fails++;
            TestFramework.LogError("e12", "TestMethod_SLPStr_OutRef:Unexpected Exception Occurred:" + e.ToString());
        }
    }


    private static void TestMethod_SLPStr_InOutRef()
    {
        TestFramework.BeginScenario("StdCall,LPStr,InOutRef");

        try
        {
            StringBuilder sb = GetInvalidString();
            string rstr = SLPStr_InOutByRef(ref sb);

            //Check the return value
            string sTemp = (GetInvalidString()).ToString();
            if (!Compare(sTemp, rstr))
            {
                Fails++;
                TestFramework.LogError("025", "TestMethod_SLPStr_InOutRef:The Return value is wrong.");
            }
            //Check the Parameter
            if (!Compare(sTemp, sb.ToString()))
            {
                Fails++;
                TestFramework.LogError("026", "TestMethod_SLPStr_InOutRef:The Parameter value is wrong.");
            }
        }
        catch (Exception e)
        {
            Fails++;
            TestFramework.LogError("e13", "TestMethod_SLPStr_InOutRef:Unexpected Exception Occurred:" + e.ToString());
        }
    }


    private static void TestMethod_SLPStr_DelegatePInvoke()
    {
        TestFramework.BeginScenario("StdCall,LPStr,DelegatePInvoke");
        try
        {
            DelegatePInvoke_StdCall caller = SLPStr_DelegatePInvoke();
            StringBuilder sb = GetInvalidString();
            string rstr = caller(ref sb);

            //Check the return value
            string sTemp = (GetInvalidString()).ToString();
            if (!Compare(sTemp, rstr))
            {
                Fails++;
                TestFramework.LogError("027", "TestMethod_SLPStr_DelegatePInvoke:The Return value is wrong.");
            }
            //Check the Parameter
            if (!Compare(sTemp, sb.ToString()))
            {
                Fails++;
                TestFramework.LogError("028", "TestMethod_SLPStr_DelegatePInvoke:The Parameter value is wrong.");
            }
        }
        catch (Exception e)
        {
            Fails++;
            TestFramework.LogError("e14", "TestMethod_SLPStr_DelegatePInvoke:Unexpected Exception Occurred:" + e.ToString());
        }
    }

    #endregion

    #region "Reverse Pinvoke"


    public static string TestMethod_CCallBackIn(string str)
    {
        //Check Input
        string sNative = "AAAA";
        if (sNative != str)
        {
            Fails++;//Use Fails variable since the parameters and the return value are used by scenario
            TestFramework.LogError("029", "TestMethod_CCallBackIn:The Input value is wrong");
        }

        StringBuilder sb = GetInvalidString();
        return sb.ToString();
    }


    public static string TestMethod_CCallBackOut(StringBuilder str)
    {
        StringBuilder sb = GetInvalidString();
        str.Append(sb.ToString());
        return sb.ToString();
    }


    public static string TestMethod_CCallBackInOut(StringBuilder str)
    {
        //Check Input
        string sNative = "AAAA";
        if (!sNative.Equals(str.ToString()))
        {
            Fails++;//Use Fails variable since the parameters and the return value are used by scenario
            TestFramework.LogError("030", "TestMethod_CCallBackInOut:The Input value is wrong");
        }

        StringBuilder sb = GetInvalidString();

        str.Remove(0, str.Length);
        str.Append(sb.ToString());

        return sb.ToString();
    }


    public static string TestMethod_CCallBackInByRef(ref string str)
    {
        //Check Input
        string sNative = "AAAA";
        if (sNative != str)
        {
            Fails++;//Use Fails variable since the parameters and the return value are used by scenario
            TestFramework.LogError("031", "TestMethod_CCallBackInByRef:The Input value is wrong");
        }
        StringBuilder sb = GetInvalidString();
        return sb.ToString();
    }


    public static string TestMethod_CCallBackOutByRef(out string str)
    {
        StringBuilder sb = GetInvalidString();

        str = sb.ToString();
        return sb.ToString();
    }


    public static string TestMethod_CCallBackInOutByRef(ref string str)
    {
        //Check Input
        string sNative = "AAAA";
        if (sNative != str)
        {
            Fails++;//Use Fails variable since the parameters and the return value are used by scenario
            TestFramework.LogError("032", "TestMethod_CCallBackInOutByRef:The Input value is wrong");
        }

        StringBuilder sb = GetInvalidString();
        str = sb.ToString();
        return sb.ToString();
    }


    public static string TestMethod_SCallBackIn(string str)
    {
        //Check Input
        string sNative = "AAAA";
        if (sNative != str)
        {
            Fails++;//Use Fails variable since the parameters and the return value are used by scenario
            TestFramework.LogError("033", "TestMethod_SCallBackIn,Managed Side:The Input value is wrong");
        }

        StringBuilder sb = GetInvalidString();
        return sb.ToString();
    }


    public static string TestMethod_SCallBackOut(StringBuilder str)
    {
        StringBuilder sb = GetInvalidString();
        str.Append(sb);
        return sb.ToString();
    }


    public static string TestMethod_SCallBackInOut(StringBuilder str)
    {
        //Check Input
        string sNative = "AAAA";
        if (!sNative.Equals(str.ToString()))
        {
            Fails++;//Use Fails variable since the parameters and the return value are used by scenario
            TestFramework.LogError("034", "TestMethod_SCallBackInOut,Managed Side:The Input value is wrong");
        }

        StringBuilder sb = GetInvalidString();
        str.Remove(0, str.Length);
        str.Append(sb.ToString());
        return sb.ToString();
    }


    public static string TestMethod_SCallBackInByRef(ref string str)
    {
        //Check Input
        string sNative = "AAAA";
        if (sNative != str)
        {
            Fails++;//Use Fails variable since the parameters and the return value are used by scenario
            TestFramework.LogError("035", "TestMethod_SCallBackInByRef,Managed Side:The Input value is wrong");
        }

        StringBuilder sb = GetInvalidString();
        str = sb.ToString();
        return sb.ToString();
    }


    public static string TestMethod_SCallBackOutByRef(out string str)
    {
        StringBuilder sb = GetInvalidString();

        str = sb.ToString();
        return sb.ToString();
    }


    public static string TestMethod_SCallBackInOutByRef(ref string str)
    {
        //Check Input
        string sNative = "AAAA";
        if (sNative != str)
        {
            Fails++;//Use Fails variable since the parameters and the return value are used by scenario
            TestFramework.LogError("036", "TestMethod_SCallBackInOutByRef,Managed Side:The Input value is wrong");
        }

        StringBuilder sb = GetInvalidString();
        str = sb.ToString();
        return sb.ToString();
    }
    #endregion




    [Fact]
    public static int TestEntryPoint()
    {

        ////Cdecl
        TestMethod_CLPStr_In();
        TestMethod_CLPStr_Out();
        TestMethod_CLPStr_InOut();

        TestMethod_CLPStr_InRef();
        TestMethod_CLPStr_OutRef();
        TestMethod_CLPStr_InOutRef();

        TestMethod_CLPStr_DelegatePInvoke();

        ////Stdcall
        TestMethod_SLPStr_In();
        TestMethod_SLPStr_Out();
        TestMethod_SLPStr_InOut();

        TestMethod_SLPStr_InRef();
        TestMethod_SLPStr_OutRef();
        TestMethod_SLPStr_InOutRef();
        TestMethod_SLPStr_DelegatePInvoke();

        //Cdecl Delegate
        DoCCallBack_LPSTR_In(new CCallBackIn(TestMethod_CCallBackIn));
        DoCCallBack_LPSTR_Out(new CCallBackOut(TestMethod_CCallBackOut));
        DoCCallBack_LPSTR_InOut(new CCallBackInOut(TestMethod_CCallBackInOut));

        DoCCallBack_LPSTR_InByRef(new CCallBackInByRef(TestMethod_CCallBackInByRef));
        DoCCallBack_LPSTR_OutByRef(new CCallBackOutByRef(TestMethod_CCallBackOutByRef));
        DoCCallBack_LPSTR_InOutByRef(new CCallBackInOutByRef(TestMethod_CCallBackInOutByRef));


        //Stdcall Delegate
        DoSCallBack_LPSTR_In(new SCallBackIn(TestMethod_SCallBackIn));
        DoSCallBack_LPSTR_Out(new SCallBackOut(TestMethod_SCallBackOut));
        DoSCallBack_LPSTR_InOut(new SCallBackInOut(TestMethod_SCallBackInOut));

        DoSCallBack_LPSTR_InByRef(new SCallBackInByRef(TestMethod_SCallBackInByRef));
        DoSCallBack_LPSTR_OutByRef(new SCallBackOutByRef(TestMethod_SCallBackOutByRef));
        DoSCallBack_LPSTR_InOutByRef(new SCallBackInOutByRef(TestMethod_SCallBackInOutByRef));

        //GetResult() return error occurred in Native Side,Fails is equal to errors occurred in ManagedSide
        if (GetResult() > 0 || Fails > 0)
        {
            Console.WriteLine("Failed!");
            return 101;
        }
        else
        {
            Console.WriteLine("Succeed!");
            return 100;
        }
    }
}
