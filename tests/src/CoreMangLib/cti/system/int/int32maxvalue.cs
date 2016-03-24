// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class Int32MaxValue
{
    private const int c_Int32MaxValue = 2147483647;

    public static int Main()
    {
        Int32MaxValue testCase = new Int32MaxValue();
        
        TestLibrary.TestFramework.BeginTestCase("Int32MaxValue");

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Test Int32MaxValue");
#pragma warning disable 0162
        if (Int32.MaxValue != c_Int32MaxValue)
        {
            TestLibrary.TestFramework.LogError("001", "expected Int32MaxValue is not equal to 2147483647 ");
            retVal = false;
        }
#pragma warning restore  
        return retVal;
    }
}

