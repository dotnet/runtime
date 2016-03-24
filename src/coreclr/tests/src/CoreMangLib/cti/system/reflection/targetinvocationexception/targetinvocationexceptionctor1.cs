// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;

/// <summary>
/// TargetInvocationException constructor() [v-yaduoj]
/// </summary>
public class TargetInvocationExceptionCtor
{
    public static int Main()
    {
        TargetInvocationExceptionCtor testObj = new TargetInvocationExceptionCtor();

        TestLibrary.TestFramework.BeginTestCase("for TargetInvocationException()");
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

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        string c_TEST_DESC = "PosTest1: initialize an instance of type TargetInvocationException via default constructor";
        string errorDesc;

        Exception innerException = new ArgumentNullException();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            TargetInvocationException ex = new TargetInvocationException(innerException);
            if (null == ex)
            {
                errorDesc = "Failed to initialize an instance of type TargetInvocationException via default constructor.";
                errorDesc += "\nInner exception is " + innerException;
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += "\nInner exception is " + innerException;
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    #endregion
}
