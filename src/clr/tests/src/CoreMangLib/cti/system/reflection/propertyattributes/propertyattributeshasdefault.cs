// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;

/// <summary>
/// PropertyAttributes.HasDefault(v-yaduoj)
/// </summary>
public class PropertyAttributesTest
{
    private enum MyPropertyAttributes
    {
        None = 0x0000,
        SpecialName = 0x0200,    
        ReservedMask = 0xf400,
        RTSpecialName = 0x0400,   
        HasDefault = 0x1000,     
        Reserved2 = 0x2000,     
        Reserved3 = 0x4000,     
        Reserved4 = 0x8000  
    }

    public static int Main()
    {
        PropertyAttributesTest testObj = new PropertyAttributesTest();

        TestLibrary.TestFramework.BeginTestCase("for Enumeration: PropertyAttributes.HasDefault");
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
        const string c_TEST_DESC = "PosTest1: Property attribute is HasDefault";
        string errorDesc;

        int expectedValue;
        int actualValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedValue = (int)MyPropertyAttributes.HasDefault;
            actualValue = (int)PropertyAttributes.HasDefault;
            if (actualValue != expectedValue)
            {
                errorDesc = "HasDefault value of PropertyAttributes is not the value " + expectedValue +
                            "as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
