// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

///<summary>
///System.Threading.ManualResetEvent.Ctor(bool) [v-zuolan]
///</summary>

public class ManualResetEventCtor
{

    public static int Main()
    {
        ManualResetEventCtor testObj = new ManualResetEventCtor();
        TestLibrary.TestFramework.BeginTestCase("for constructor of System.Threading.ManualResetEvent");
        if (testObj.RunTests())
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
        TestLibrary.TestFramework.LogInformation("Positive");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }


    #region Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        ManualResetEvent expectedValue = new ManualResetEvent(true);
        ManualResetEvent actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Set initialState as true and Create a instance");
        try
        {
            actualValue = (ManualResetEvent)(new ManualResetEvent(true));
            if (expectedValue.Equals(actualValue))
            {
                TestLibrary.TestFramework.LogError("001", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        ManualResetEvent expectedValue = new ManualResetEvent(false);
        ManualResetEvent actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2:Set initialState as false and Create a instance");
        try
        {
            actualValue = (ManualResetEvent)(new ManualResetEvent(false));
            if (expectedValue.Equals(actualValue))
            {
                TestLibrary.TestFramework.LogError("003", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
