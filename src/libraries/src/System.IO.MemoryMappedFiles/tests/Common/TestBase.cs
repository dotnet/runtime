// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

public class TestBase
{
    public String strLoc = "baseLoc";
    public int iCountTestcases = 0;
    public int iCountErrors = 0;

    public bool Eval(bool exp)
    {
        return Eval(exp, null);
    }
    public bool Eval(bool exp, String errorMsg)
    {
        if (!exp)
        {
            iCountErrors++;
            String err = errorMsg;
            if (err == null)
                err = "Test Failed at location: " + strLoc;
            Console.WriteLine(err);
        }
        return exp;
    }

    public bool Eval(bool exp, String format, params object[] arg)
    {
        if (!exp)
        {
            return Eval(exp, String.Format(format, arg));
        }
        return true;
    }

    public bool Eval<T>(T expected, T actual, String errorMsg)
    {
        bool retValue = expected == null ? actual == null : expected.Equals(actual);

        if (!retValue)
            return Eval(retValue, errorMsg +
                "   Expected:" + (null == expected ? "<null>" : expected.ToString()) +
                "   Actual  :" + (null == actual ? "<null>" : actual.ToString()));
        return true;
    }

    public bool Eval<T>(T expected, T actual, String format, params object[] arg)
    {
        bool retValue = expected == null ? actual == null : expected.Equals(actual);

        if (!retValue)
            return Eval(retValue, String.Format(format, arg) +
                "   Expected:" + (null == expected ? "<null>" : expected.ToString()) +
                "   Actual:  " + (null == actual ? "<null>" : actual.ToString()));

        return true;
    }
}
