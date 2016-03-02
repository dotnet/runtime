// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;

public class SecurityExceptionCtor2
{
    private const int c_MINLEN = 10;
    private const int c_MAXLEN = 1024;

    public static int Main()
    {
        SecurityExceptionCtor2 ac = new SecurityExceptionCtor2();

        TestLibrary.TestFramework.BeginTestCase("SecurityExceptionCtor2");

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
        string msg;

        TestLibrary.TestFramework.BeginScenario("PosTest1: SecurityException.Ctor(string)");

        try
        {
            msg = TestLibrary.Generator.GetString(-55, false, c_MINLEN, c_MAXLEN);
            sec = new SecurityException(msg);

            if (!sec.Message.Equals(msg))
            {
                TestLibrary.TestFramework.LogError("000", "Message mismatch: Expected("+msg+") Actual("+sec.Message+")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}

