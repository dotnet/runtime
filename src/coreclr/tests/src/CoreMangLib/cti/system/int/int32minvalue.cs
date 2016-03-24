// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

class Int32MinValue
{
    private const int c_Int32MinValue = -2147483648;
   
    public static int Main()
    {
        Int32MinValue testCase = new Int32MinValue();

        TestLibrary.TestFramework.BeginTestCase("Int32MinValue");

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Test Int32MinValue ");
#pragma warning disable 0162
        if (Int32.MinValue != c_Int32MinValue)
        {
            TestLibrary.TestFramework.LogError("001", "expected Int32MaxValue is not equal to -2147483648 ");
            retVal = false;
       }
#pragma warning restore

        return retVal;
    }
}

