// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;

public class SecurityExceptionCtor1
{
    public static int Main()
    {
        SecurityExceptionCtor1 ac = new SecurityExceptionCtor1();

        TestLibrary.TestFramework.BeginTestCase("SecurityExceptionCtor1");

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: SecurityException.Ctor");

        try
        {
            sec = new SecurityException();

            if (null == sec.Message)
            {
                TestLibrary.TestFramework.LogError("000", "Message is null!");
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

