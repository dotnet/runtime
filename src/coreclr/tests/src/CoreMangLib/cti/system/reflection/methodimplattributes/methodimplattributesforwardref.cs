// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;

/// <summary>
/// System.Reflection.MethodAttributes.CodeTypeMask[v-jisong]
/// </summary>
public class MethodImplAttributesForwardRef
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1:check the MethodImplAttributes.ForwardRef value is 0x0010...";
        const string c_TEST_ID = "P001";
        MethodImplAttributes FLAG_VALUE = (MethodImplAttributes)0x0010;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (MethodImplAttributes.ForwardRef != FLAG_VALUE)
            {
                string errorDesc = "value " + FLAG_VALUE.ToString() + " is not as expected: Actual is " + MethodImplAttributes.ForwardRef.ToString();
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #endregion

    public static int Main()
    {
        MethodImplAttributesForwardRef test = new MethodImplAttributesForwardRef();

        TestLibrary.TestFramework.BeginTestCase("System.Reflection.MethodImplForwardRef");

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
