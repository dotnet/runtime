// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class EnvironmentNewLine
{
    public static int Main(string[] args)
    {
        EnvironmentNewLine newLine = new EnvironmentNewLine();
        TestLibrary.TestFramework.BeginScenario("Testing System.Environment.NewLine property...");

        if (newLine.RunTests())
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

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify environment.NewLine contains \r\n...");

        try
        {
            if (TestLibrary.Utilities.IsWindows && !Environment.NewLine.Contains("\r\n"))
            {
                TestLibrary.TestFramework.LogError("001", @"The NewLine does not contain \r\n");
                retVal = false;
            }
            else if (!TestLibrary.Utilities.IsWindows && !Environment.NewLine.Contains("\n"))
            {
                TestLibrary.TestFramework.LogError("001", @"The NewLine does not contain \n");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
