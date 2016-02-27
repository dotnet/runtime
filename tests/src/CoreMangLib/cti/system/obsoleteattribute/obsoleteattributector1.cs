// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.ObsoleteAttribute.Ctor()[v-juwa]
/// </summary>
public class ObsoleteAttributeCtor1
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

        const string c_TEST_DESC = "PosTest1: Verify created the object is not null";
        const string c_TEST_ID = "P001";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ObsoleteAttribute oa = new ObsoleteAttribute();
            if (oa == null)
            {
                string errorDesc = "created ObsoleteAttribute should not be null";
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

        const string c_TEST_DESC = "PosTest2: Verify createed ObsoleteAttribute object's Message is  null";
        const string c_TEST_ID = "P002";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ObsoleteAttribute oa = new ObsoleteAttribute();
            if (oa.Message != null)
            {
                string errorDesc = "created ObsoleteAttribute's Message should be null";
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

        const string c_TEST_DESC = "PosTest3: Verify create the ObsoleteAttribute object's IsError property is false";
        const string c_TEST_ID = "P003";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            ObsoleteAttribute oa = new ObsoleteAttribute();
            if (oa.IsError)
            {
                string errorDesc = "created ObsoleteAttribute IsError property should  be false";
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

    #region Nagetive Test Cases
    //public bool NegTest1()
    //{
    //    bool retVal = true;

    //    TestLibrary.TestFramework.BeginScenario("NegTest1: ");

    //    try
    //    {
    //          //
    //          // Add your test logic here
    //          //
    //    }
    //    catch (Exception e)
    //    {
    //        TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
    //        TestLibrary.TestFramework.LogInformation(e.StackTrace);
    //        retVal = false;
    //    }

    //    return retVal;
    //}
    #endregion
    #endregion

    public static int Main()
    {
        ObsoleteAttributeCtor1 test = new ObsoleteAttributeCtor1();

        TestLibrary.TestFramework.BeginTestCase("For method:System.ObsoleteAttribute.Ctor()");

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
