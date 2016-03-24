// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime;

/// <summary>
/// System.Runtime.CompilerServices.AccessedThroughPropertyAttribute.PropertyName
/// </summary>
public class AccessedThroughPropertyAttributePropertyName
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    public static int Main()
    {
        AccessedThroughPropertyAttributePropertyName atpaPropertyName = new AccessedThroughPropertyAttributePropertyName();
        TestLibrary.TestFramework.BeginTestCase("Testing for Method: System.Runtime.CompilerServices.AccessedThroughPropertyAttribute.Ctor()...");

        if (atpaPropertyName.RunTests())
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

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest1: Verfify property PropertyName ... ";
        string c_TEST_ID = "P001";
        string c_PROPERTYNAME = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        System.Runtime.CompilerServices.AccessedThroughPropertyAttribute atpa = new System.Runtime.CompilerServices.AccessedThroughPropertyAttribute(c_PROPERTYNAME);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (atpa.PropertyName != c_PROPERTYNAME)
            {
                string errorDesc = "value is not " + c_PROPERTYNAME + " as expected: Actual is " + atpa.PropertyName;
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
            

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest2: Verfify the PropertyName is empty ... ";
        string c_TEST_ID = "P002";
        string c_PROPERTYNAME = "";

        System.Runtime.CompilerServices.AccessedThroughPropertyAttribute atpa = new System.Runtime.CompilerServices.AccessedThroughPropertyAttribute(c_PROPERTYNAME);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (atpa.PropertyName != c_PROPERTYNAME)
            {
                string errorDesc = "value is not " + c_PROPERTYNAME + " as expected: Actual is empty"; 
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest3: Verfify the PropertyName is a null reference ... ";
        string c_TEST_ID = "P003";
        string c_PROPERTYNAME = null;

        System.Runtime.CompilerServices.AccessedThroughPropertyAttribute atpa = new System.Runtime.CompilerServices.AccessedThroughPropertyAttribute(c_PROPERTYNAME);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (atpa.PropertyName != c_PROPERTYNAME)
            {
                string errorDesc = "value is not " + c_PROPERTYNAME + " as expected: Actual is null"; 
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexcpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    #endregion
}
