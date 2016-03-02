// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;

/// <summary>
/// TargetParameterCountException constructor(string) [v-yaduoj]
/// </summary>
public class TargetParameterCountExceptionCtor
{
    public static int Main()
    {
        TargetParameterCountExceptionCtor testObj = new TargetParameterCountExceptionCtor();

        TestLibrary.TestFramework.BeginTestCase("for TargetParameterCountException(string)");
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
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        string c_TEST_DESC = "PosTest1: initialize an instance of type TargetParameterCountException using an emtpy string message";
        string errorDesc;

        string message = string.Empty;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            TargetParameterCountException e = new TargetParameterCountException(message);
            if (null == e || e.Message != message)
            {
                errorDesc = "Failed to initialize an instance of type TargetParameterCountException.";
                errorDesc += "\nInput message is emtpy string";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += "\nInput message is emtpy string";
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        string c_TEST_DESC = "PosTest2: initialize an instance of type TargetParameterCountException using a string containing special character";
        string errorDesc;

        string message = "Not supported exception occurs here \n\r\0\t\v";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            TargetParameterCountException e = new TargetParameterCountException(message);
            if (null == e || e.Message != message)
            {
                errorDesc = "Failed to initialize an instance of type TargetParameterCountException.";
                errorDesc += "\nInput message is \"" + message + "\"";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += "\nInput message is \"" + message + "\"";
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "P003";
        string c_TEST_DESC = "PosTest3: initialize an instance of type TargetParameterCountException using a null reference";
        string errorDesc;

        string message = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            TargetParameterCountException e = new TargetParameterCountException(message);
            if (null == e)
            {
                errorDesc = "Failed to initialize an instance of type TargetParameterCountException.";
                errorDesc += "\nInput message is a null reference.";
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += "\nInput message is a null reference.";
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
