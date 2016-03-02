// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.ObjectDisposedException.ObjectName[v-juwa]
/// </summary>
public class ObjectDisposedExceptionObjectName
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;

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

        const string c_TEST_DESC = "PosTest1: Verify the objectName is random string";
        const string c_TEST_ID = "P001";

        string name = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
        ObjectDisposedException exception = new ObjectDisposedException(name);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (exception.ObjectName != name)
            {
                string errorDesc = "ObjectName is not " + name + " as expected: Actual(" + exception.ObjectName+ ")";
                TestLibrary.TestFramework.LogError("001 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + "TestID_" + c_TEST_ID, "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Verify the objectName is empty";
        const string c_TEST_ID = "P002";

        string name = String.Empty;
        ObjectDisposedException exception = new ObjectDisposedException(name);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (exception.ObjectName != name)
            {
                string errorDesc = "ObjectName is not empty as expected: Actual(" + exception.ObjectName + ")";
                TestLibrary.TestFramework.LogError("003 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest3: Verify the objectName parameter is null";
        const string c_TEST_ID = "P003";

        string name = null;
        ObjectDisposedException exception = new ObjectDisposedException(name);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (exception.ObjectName != String.Empty)
            {
                string errorDesc = "ObjectName is not empty as expected: Actual(" + exception.ObjectName + ")";
                TestLibrary.TestFramework.LogError("005 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ObjectDisposedExceptionObjectName test = new ObjectDisposedExceptionObjectName();

        TestLibrary.TestFramework.BeginTestCase("For property:System.ObjectDisposedException.ObjectName");

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
