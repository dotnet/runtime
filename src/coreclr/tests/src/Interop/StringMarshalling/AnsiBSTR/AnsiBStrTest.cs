// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;
using NativeDefs;
using System.Diagnostics;

class Test
{

    #region "Report Failure"
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

    public static int Main(string[] args)
    {
        string strManaged = " \0Managed\0String\0 ";
        string strRet = "a";
        string strNative = "Native String";

        //since the out attributes doesnt work for string, so i dont check the out value.
        string strPara2 = strManaged;
        string strRet2 = AnsiBStrTestNative.Marshal_InOut(strPara2);
        if (!strRet2.Equals(strRet))
        {
            ReportFailure("Method AnsiBStrTestNative.Marshal_InOut[Managed Side],The Return string is wrong", strRet, strRet2);
        }
        if (!strPara2.Equals(strManaged))
        {
            ReportFailure("Method AnsiBStrTestNative.Marshal_InOut[Managed Side],The Parameter string is Changed", strManaged, strPara2);
        }

        //TestMethod3
        string strPara3 = strManaged;
        string strRet3 = AnsiBStrTestNative.Marshal_Out(strPara3);
        if (!strRet.Equals(strRet3))
        {
            ReportFailure("Method AnsiBStrTestNative.Marshal_Out[Managed Side],The Return string is wrong", strRet, strRet3);
        }
        if (!strPara3.Equals(strManaged))
        {
            ReportFailure("Method AnsiBStrTestNative.Marshal_Out[Managed Side],The Parameter string is not Changed", strManaged, strPara3);
        }

        //TestMethod5
        string strPara5 = strManaged;
        string strRet5 = AnsiBStrTestNative.MarshalPointer_InOut(ref strPara5);

        if (!strRet5.Equals(strRet))
        {
            ReportFailure("Method AnsiBStrTestNative.MarshalPointer_InOut[Managed Side],The Return string is wrong", strRet, strRet5);
        }
        if (!strPara5.Equals(strNative))
        {
            ReportFailure("Method AnsiBStrTestNative.MarshalPointer_InOut[Managed Side],The Passed string is wrong", strNative, strPara5);
        }

        //TestMethod6
        string strPara6 = strManaged;
        string strRet6 = AnsiBStrTestNative.MarshalPointer_Out(out strPara6);
        if (!strRet6.Equals(strRet))
        {
            ReportFailure("Method AnsiBStrTestNative.MarshalPointer_Out[Managed Side],The Return string is wrong", strRet, strRet6);
        }
        if (!strPara6.Equals(strNative))
        {
            ReportFailure("Method AnsiBStrTestNative.MarshalPointer_Out[Managed Side],The Passed string is wrong", strNative, strPara6);
        }

        return ExitTest();
    }
}
