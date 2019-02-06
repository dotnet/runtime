// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//The testcase focus test the BStr with embed null string

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;
using NativeDefs;

class Test
{

    #region "Reprot Failure"
    static int fails = 0; //record the fail numbers
    // Overload methods for reportfailure	
    static int ReportFailure(string s)
    {
        Console.WriteLine(" === Fail:" + s);
        return (++fails);
    }
    static int ReportFailure(string expect, string actual)
    {
        Console.WriteLine(" === Fail: Expected:" + expect + "\n          Actual:" + actual);
        return (++fails);
    }
    static int ReportFailure(string describe, string expect, string actual)
    {
        Console.WriteLine(" === Fail: " + describe + "\n\tExpected:" + expect + "\n\tActual:" + actual);
        return (++fails);
    }
    static bool IsEqual(StringBuilder s1, StringBuilder s2)
    {
        if (!s1.ToString().Equals(s2.ToString()))
        {
            return false;
        }
        return true;
    }
    #endregion

    #region "Helper"
    // ************************************************************
    // Returns the appropriate exit code
    // *************************************************************
    static int ExitTest()
    {
        if (fails == 0)
        {
            Console.WriteLine("PASS");
            return 100;
        }
        else
        {
            Console.WriteLine("FAIL - " + fails + " failure(s) occurred");
            return 101;
        }
    }
    #endregion
    
    #region ReversePinvoke
    public static StringBuilder Call_Del_MarshalStrB_InOut(StringBuilder r)
    {        
        StringBuilder retstr = new StringBuilder("Native\0Native");
        StringBuilder expetcedstr = new StringBuilder("ă",1);
        if (!r.ToString().Equals(expetcedstr.ToString()))
        {
            ReportFailure("Method Call_Del_MarshalStrB_InOut[Managed Side] Failure. String is different than expected", "ă", r.ToString());
        }
        r.Replace('ă', 'm');
        //r = new StringBuilder("Managed", 7);
        return retstr;
    }

    public static StringBuilder Call_Del_MarshalStrB_Out(out StringBuilder r)
    {
        StringBuilder retstr = new StringBuilder("Native\0Native");
        r = new StringBuilder("Managed", 7);
        return retstr;
    }

    #endregion

    public static int Main(string[] args)
    {
        string strManaged = "Managed";
        string strRet = "a";

        StringBuilder strBRet = new StringBuilder("a", 1);
        string strNative = " Native";
        StringBuilder strBNative = new StringBuilder(" Native", 7);

        Console.WriteLine("[Calling PInvokeDef.Marshal_In]");
        string strPara1 = strManaged;
        string strRet1 = PInvokeDef.Marshal_In(strPara1);
        if (!strRet1.Equals(strRet))
        {
            ReportFailure("Method PInvokeDef.Marshal_InOut[Managed Side],The Return string is wrong", strRet, strRet1);
        }

        Console.WriteLine("[Calling PInvokeDef.Marshal_InOut]");
        bool outByValueFails = false;
        try
        {
            string strPara2 = strManaged;
            PInvokeDef.Marshal_InOut(strPara2);
        }
        catch (MarshalDirectiveException)
        {
            outByValueFails = true;
        }

        if (!outByValueFails)
        {
            ReportFailure("Method PInvokeDef.Marshal_Out did not throw a MarshalDirectiveException.");
        }

        //TestMethod3
        Console.WriteLine("[Calling PInvokeDef.Marshal_Out]");
        outByValueFails = false;
        try
        {
            string strPara3 = strManaged;
            PInvokeDef.Marshal_Out(strPara3);
        }
        catch (MarshalDirectiveException)
        {
            outByValueFails = true;
        }

        if (!outByValueFails)
        {
            ReportFailure("Method PInvokeDef.Marshal_Out did not throw a MarshalDirectiveException.");
        }
        
        //TestMethod5
        Console.WriteLine("[Calling PInvokeDef.MarshalPointer_InOut]");
        string strPara5 = strManaged;
        string strRet5 = PInvokeDef.MarshalPointer_InOut(ref strPara5);
        if (!strRet5.Equals(strRet))
        {
            ReportFailure("Method PInvokeDef.MarshalPointer_InOut[Managed Side],The Return string is wrong", strRet, strRet5);
        }
        if (!strPara5.Equals(strNative))
        {
            ReportFailure("Method PInvokeDef.MarshalPointer_InOut[Managed Side],The Passed string is wrong", strNative, strPara5);
        }

        //TestMethod6
        Console.WriteLine("[Calling PInvokeDef.MarshalPointer_Out]");
        string strPara6;// = String.Copy(strManaged);
        string strRet6 = PInvokeDef.MarshalPointer_Out(out strPara6);
        if (!strRet6.Equals(strRet))
        {
            ReportFailure("Method PInvokeDef.MarshalPointer_Out[Managed Side],The Return string is wrong", strRet, strRet6);
        }
        if (!strPara6.Equals(strNative))
        {
            ReportFailure("Method PInvokeDef.MarshalPointer_Out[Managed Side],The Passed string is wrong", strNative, strPara6);
        }


        //TestMethod7
        Console.WriteLine("[Calling PInvokeDef.MarshalStrB_InOut]");
        StringBuilder strPara7 = new StringBuilder(strManaged);
        StringBuilder strRet7 = PInvokeDef.MarshalStrB_InOut(strPara7);

        if (!IsEqual(strRet7,strBRet))
        {
            ReportFailure("Method PInvokeDef.MarshalStrB_InOut[Managed Side],The Return string is wrong", strRet, strRet7.ToString());
        }
        if (!strPara7.Equals(new StringBuilder(strNative)))
        {
            ReportFailure("Method PInvokeDef.MarshalStrB_InOut[Managed Side],The Passed string is wrong", strNative, strPara7.ToString());
        }

        //TestMethod8
        Console.WriteLine("[Calling PInvokeDef.MarshalStrB_Out]");
        StringBuilder strPara8;// = new StringBuilder(strManaged);
        StringBuilder strRet8 = PInvokeDef.MarshalStrB_Out(out strPara8);
        if (!IsEqual(strRet8, strBRet))
        {
            ReportFailure("Method PInvokeDef.MarshalStrB_Out[Managed Side],The Return string is wrong", strRet, strRet8.ToString());
        }
        if (!IsEqual(strPara8,strBNative))
        {
            ReportFailure("Method PInvokeDef.MarshalStrB_Out[Managed Side],The Passed string is wrong", strNative, strPara8.ToString());
        }
        
        //TestMethod9
        Console.WriteLine("[Calling PInvokeDef.MarshalStrWB_InOut]");
        StringBuilder strPara9 = new StringBuilder(strManaged, strManaged.Length);
        StringBuilder strRet9 = PInvokeDef.MarshalStrWB_InOut(strPara9);
        
        if (!IsEqual(strRet9, strBRet))
        {
            ReportFailure("Method PInvokeDef.MarshalStrWB_InOut[Managed Side],The Return string is wrong", strRet, strRet9.ToString());
        }
        if (!IsEqual(strPara9,strBNative))
        {
            ReportFailure("Method PInvokeDef.MarshalStrWB_InOut[Managed Side],The Passed string is wrong", strNative, strPara9.ToString());
        }

        #region DelegatePinvoke
        //TestMethod11
        Del_MarshalPointer_InOut d1 = new Del_MarshalPointer_InOut(PInvokeDef.MarshalPointer_InOut);
        string strPara11 = new string(strManaged.ToCharArray());
        string strRet11 = d1(ref strPara11);
        if (!strRet11.Equals(strRet))
        {
            ReportFailure("Method Del_MarshalPointer_InOut[Managed Side],The Return string is wrong", strRet, strRet11);
        }
        if (!strPara11.Equals(strNative))
        {
            ReportFailure("Method Del_MarshalPointer_InOut[Managed Side],The Passed string is wrong", strNative, strPara11);
        }

        outByValueFails = false;
        Del_Marshal_Out d2 = new Del_Marshal_Out(PInvokeDef.Marshal_Out);
        try
        {
            string strPara12 = strManaged;
            d2(strPara12);
        }
        catch (MarshalDirectiveException)
        {
            outByValueFails = true;
        }

        if (!outByValueFails)
        {
            ReportFailure("Method PInvokeDef.Marshal_Out did not throw a MarshalDirectiveException.");
        }
        #endregion

        #region ReversePInvoke
        Del_MarshalStrB_InOut d3 = new Del_MarshalStrB_InOut(Call_Del_MarshalStrB_InOut);
        if (!PInvokeDef.ReverseP_MarshalStrB_InOut(d3, "ă"))
        {
            ReportFailure("Method ReverseP_MarshalStrB_InOut[Managed Side],return value is false");
        }

        Del_MarshalStrB_Out d4 = new Del_MarshalStrB_Out(Call_Del_MarshalStrB_Out);
        if (!PInvokeDef.ReverseP_MarshalStrB_Out(d4))
        {
            ReportFailure("Method ReverseP_MarshalStrB_Out[Managed Side],return value is false");
        }

        #endregion
        
        int length = 10;
        StringBuilder nullTerminatorBuilder = new StringBuilder(length);
        if (!PInvokeDef.Verify_NullTerminators_PastEnd(nullTerminatorBuilder, length))
        {
            ReportFailure("Null terminators for StringBuilder not set for [In] semantics");
        }
        if (!PInvokeDef.Verify_NullTerminators_PastEnd_Out(nullTerminatorBuilder, length))
        {
            ReportFailure("Null terminators for StringBuilder not set for [Out] semantics");
        }

        return ExitTest();
     }
}
