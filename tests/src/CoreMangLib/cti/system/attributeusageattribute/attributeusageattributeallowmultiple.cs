// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

///<summary>
///System.AttributeUsageAttribute.AllowMultiple
///</summary>

public class AttributeUsageAttributeAllowMultiple
{

    public static int Main()
    {
        AttributeUsageAttributeAllowMultiple testObj = new AttributeUsageAttributeAllowMultiple();
        TestLibrary.TestFramework.BeginTestCase("for property of System.AttributeUsageAttribute.AllowMultiple");
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
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        return retVal;
    }


    #region Test Logic
    
    public bool PosTest1()
    {
        bool retVal = true;
        bool expectedValue = true;

        AttributeUsageAttribute aUT = new AttributeUsageAttribute(AttributeTargets.All);

        TestLibrary.TestFramework.BeginScenario("PosTest1:get and set AllowMultiple as true.");
        try
        {
            aUT.AllowMultiple = expectedValue;
            if (expectedValue != aUT.AllowMultiple)
            {
                TestLibrary.TestFramework.LogError("001", "ExpectedValue(" + aUT.AllowMultiple + ") !=ActualValue(" + expectedValue + ")");
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
        bool expectedValue = false;

        AttributeUsageAttribute aUT = new AttributeUsageAttribute(AttributeTargets.All);

        TestLibrary.TestFramework.BeginScenario("PosTest2:get and set AllowMultiple as false.");
        try
        {
            aUT.AllowMultiple = expectedValue;
            if (expectedValue != aUT.AllowMultiple)
            {
                TestLibrary.TestFramework.LogError("003", "ExpectedValue(" + aUT.AllowMultiple + ") !=ActualValue(" + expectedValue + ")");
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
