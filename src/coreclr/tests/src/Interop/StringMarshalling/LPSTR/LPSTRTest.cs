// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    #region ReversePInvoke

    public static string Call_DelMarshal_InOut(string s)
    {
        string strRet;
        if (!s.Equals("ň"))
        {
            ReportFailure("Method Call_DelMarshal_InOut[Managed Side],The passed string is wrong", "ň", s);
            strRet = "\0\0\0";
            return strRet;
        }
        s = "Managed";
        strRet = "Return\0Return\0";
        return strRet;
    }

    public static string Call_DelMarshalPointer_Out(out string s)
    {
        s = "Native\0String\0";
        string strRet = "Return\0Return\0";
        return strRet;
    }

    public static StringBuilder Call_Del_MarshalStrB_InOut(StringBuilder r)
    {
        StringBuilder retstr = new StringBuilder("Return\0Native");

        if (!r.ToString().Equals(new StringBuilder("a", 1).ToString()))
        {
            ReportFailure("Method Call_Del_MarshalStrB_InOut[Managed Side] Failure. String is different than expected", "ă", r.ToString());
        }
        r.Replace('a', 'm');
        return retstr;
    }

    public static StringBuilder Call_Del_MarshalStrB_Out(out StringBuilder r)
    {
        StringBuilder retstr = new StringBuilder("Native\0Native");
        r = new StringBuilder("Managed", 7);
        return retstr;
    }

    #endregion

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

    public static int Main(string[] args)
    {
#if BUG750509
        if ((args != null) && (args.Length == 1) && (args[0] == "/mb"))
        {
            MessageBox(new IntPtr(0), "Hello World!", "Hello Dialog", 0);
        }
#endif

#pragma warning disable 0219
        string strManaged = "Managed";
        string strRet = "a";
        StringBuilder strBRet = new StringBuilder("a", 1);
        string strNative = " Native";
        StringBuilder strBNative = new StringBuilder(" Native", 7);
#pragma warning restore 0219

        PInvokeDef.Writeline("Format i=%d c=%c d=%f, s=%d, u=%d", 100, 'A', 3.1415926, 32767, 32768);

        //since the out attributes doesnt work for string, so i dont check the out value.
        string strPara2 = strManaged;
        string strRet2 = PInvokeDef.Marshal_InOut(strPara2);
        if (!strRet2.Equals(strRet))
        {
            ReportFailure("Method PInvokeDef.Marshal_InOut[Managed Side],The Return string is wrong", strRet, strRet2);
        }
        if (!strPara2.Equals(strManaged))
        {
            ReportFailure("Method PInvokeDef.Marshal_InOut[Managed Side],The Parameter string is Changed", strManaged, strPara2);
        }

        //TestMethod3
        string strPara3 = strManaged;
        string strRet3 = PInvokeDef.Marshal_Out(strPara3);
        if (!strRet.Equals(strRet3))
        {
            ReportFailure("Method PInvokeDef.Marshal_Out[Managed Side],The Return string is wrong", strRet, strRet3);
        }
        if (!strPara3.Equals(strManaged))
        {
            ReportFailure("Method PInvokeDef.Marshal_Out[Managed Side],The Parameter string is not Changed", strManaged, strPara3);
        }

        //TestMethod5
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
        string strPara6 = strManaged;
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
        StringBuilder strPara7 = new StringBuilder(strManaged, strManaged.Length);
        StringBuilder strRet7 = PInvokeDef.MarshalStrB_InOut(strPara7);

        if (!strRet7.ToString().Equals(strBRet.ToString()))
        {
            ReportFailure("Method PInvokeDef.MarshalStrB_InOut[Managed Side],The Return string is wrong", strRet, strRet7.ToString());
        }
        if (!strPara7.ToString().Equals(strBNative.ToString()))
        {
            ReportFailure("Method PInvokeDef.MarshalStrB_InOut[Managed Side],The Passed string is wrong", strNative, strPara7.ToString());
        }

        //TestMethod8
        StringBuilder strPara8;// = new StringBuilder(strManaged);
        StringBuilder strRet8 = PInvokeDef.MarshalStrB_Out(out strPara8);
        if (!strRet8.ToString().Equals(strBRet.ToString()))
        {
            ReportFailure("Method PInvokeDef.MarshalStrB_Out[Managed Side],The Return string is wrong", strRet, strRet8.ToString());
        }
        if (!strPara8.ToString().Equals(strBNative.ToString()))
        {
            ReportFailure("Method PInvokeDef.MarshalStrB_Out[Managed Side],The Passed string is wrong", strNative, strPara8.ToString());
        }

        #region ReversePinvoke
        DelMarshal_InOut d1 = new DelMarshal_InOut(Call_DelMarshal_InOut);
        if (!PInvokeDef.RPinvoke_DelMarshal_InOut(d1, "ň"))
        {
            ReportFailure("Method RPinvoke_DelMarshal_InOut[Managed Side],Return value is false");
        }

        DelMarshalPointer_Out d2 = new DelMarshalPointer_Out(Call_DelMarshalPointer_Out);
        if (!PInvokeDef.RPinvoke_DelMarshalPointer_Out(d2))
        {
            ReportFailure("Method RPinvoke_DelMarshal_Out[Managed Side],Return value is false");
        }

        #endregion
        #region DelegatePInvoke

        Del_Marshal_InOut d3 = new Del_Marshal_InOut(PInvokeDef.Marshal_InOut);
        string strPara9 = strManaged;
        string strRet9 = d3(strPara9);
        if (!strRet9.Equals(strRet))
        {
            ReportFailure("Method Del_Marshal_InOut[Managed Side],The Return string is wrong", strRet, strRet9);
        }
        if (!strPara9.Equals(strManaged))
        {
            ReportFailure("Method Del_Marshal_InOut[Managed Side],The Parameter string is Changed", strManaged, strPara9);
        }

        Del_MarshalPointer_Out d4 = new Del_MarshalPointer_Out(PInvokeDef.MarshalPointer_Out);
        string strPara10 = strManaged;
        string strRet10 = d4(out strPara10);
        if (!strRet10.Equals(strRet))
        {
            ReportFailure("Method Del_MarshalPointer_Out[Managed Side],The Return string is wrong", strRet, strRet10);
        }
        if (!strPara10.Equals(strNative))
        {
            ReportFailure("Method Del_MarshalPointer_Out[Managed Side],The Passed string is wrong", strNative, strPara10);
        }

        Del_MarshalStrB_InOut d5 = new Del_MarshalStrB_InOut(Call_Del_MarshalStrB_InOut);

        if (!PInvokeDef.ReverseP_MarshalStrB_InOut(d5, new string('a', 1)))
        {
            ReportFailure("Method ReverseP_MarshalStrB_InOut[Managed Side],return value is false");
        }
        #endregion
        return ExitTest();
    }
}
