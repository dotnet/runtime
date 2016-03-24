// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.NullReferenceException.Ctor()[v-juwa]
/// </summary>
public class NullReferenceExceptionCtor1
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Verify create the exception object is not null";
        const string c_TEST_ID = "P001";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            NullReferenceException exception = new NullReferenceException();
            if (exception == null)
            {
                string errorDesc = "created exception should not be null";
                TestLibrary.TestFramework.LogError("001 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Verify createed exception object's InnerException is not null";
        const string c_TEST_ID = "P002";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            NullReferenceException exception = new NullReferenceException();
            if (exception.InnerException != null)
            {
                string errorDesc = "created exception's InnerException should be null";
                TestLibrary.TestFramework.LogError("003 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

   
    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest3: Verify create the exception object's Message is localized error message";
        const string c_TEST_ID = "P003";

        string message = "Object reference not set to an instance of an object.";
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            NullReferenceException exception = new NullReferenceException();
            if ((exception.Message != message) &
                (!exception.Message.Contains("[Arg_NullReferenceException]")))
            {
                string errorDesc = "Message is not \""+message+"\" as expected :Actual(\""+exception.Message+"\")";
                TestLibrary.TestFramework.LogError("005 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #endregion

    public static int Main()
    {
        NullReferenceExceptionCtor1 test = new NullReferenceExceptionCtor1();

        TestLibrary.TestFramework.BeginTestCase("For Method:System.NullReferenceException.Ctor()");

        if (test.RunTests())
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
}
