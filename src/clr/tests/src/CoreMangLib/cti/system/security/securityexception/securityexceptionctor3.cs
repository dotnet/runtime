// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;

public class SecurityExceptionCtor3
{
    private const int c_MINLEN = 10;
    private const int c_MAXLEN = 1024;

    public static int Main()
    {
        SecurityExceptionCtor3 ac = new SecurityExceptionCtor3();

        TestLibrary.TestFramework.BeginTestCase("SecurityExceptionCtor3");

        if (ac.RunTests())
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("FAIL");
            return 0;
        }
    }

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        SecurityException sec;
        Exception eh;
        string msg1;
        string msg2;

        TestLibrary.TestFramework.BeginScenario("PosTest1: SecurityException.Ctor(string, exception)");

        try
        {
            msg1 = TestLibrary.Generator.GetString(-55, false, c_MINLEN, c_MAXLEN);
            msg2 = TestLibrary.Generator.GetString(-55, false, c_MINLEN, c_MAXLEN);
            eh   = new Exception(msg2);
            sec  = new SecurityException(msg1, eh);

            if (!sec.Message.Equals(msg1))
            {
                TestLibrary.TestFramework.LogError("000", "Message mismatch: Expected("+msg1+") Actual("+sec.Message+")");
                retVal = false;
            }

            if (null == sec.InnerException)
            {
                TestLibrary.TestFramework.LogError("001", "InnerException should not be null");
                retVal = false;
            }

            if (null != sec.InnerException && !sec.InnerException.Message.Equals(msg2))
            {
                TestLibrary.TestFramework.LogError("002", "InnerException Message mismatch: Expected("+msg2+") Actual("+sec.InnerException.Message+")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}

