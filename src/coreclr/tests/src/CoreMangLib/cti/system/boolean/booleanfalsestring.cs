// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class BooleanFalseString
{
    const string FALSE_STRING = "False";

    public static int Main()
    {
        BooleanFalseString testCase = new BooleanFalseString();

        TestLibrary.TestFramework.BeginTestCase("Boolean.FalseString");
        if (testCase.RunTests())
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
        try
        {
            if (Boolean.FalseString != FALSE_STRING)
            {
                TestLibrary.TestFramework.LogError("001", "expect Boolean.FalseString == \"False\" ");
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
