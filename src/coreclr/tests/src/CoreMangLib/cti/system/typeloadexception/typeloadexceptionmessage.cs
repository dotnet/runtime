// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

///<summary>
///System.TypeLoadException.Message [v-zuolan]
///</summary>

public class TypeLoadExceptionCtor
{

    public static int Main()
    {
        TypeLoadExceptionCtor testObj = new TypeLoadExceptionCtor();
        TestLibrary.TestFramework.BeginTestCase("for property of System.TypeLoadException");
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
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        return retVal;
    }


    #region Positive Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        String errMessage = TestLibrary.Generator.GetString(-55, false, 1, 255);
        TypeLoadException tLE = new TypeLoadException(errMessage);

        String expectedValue = errMessage;
        String actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Get Message when the message is a random string");
        try
        {
            actualValue = tLE.Message;

            if (!actualValue.Equals(expectedValue))
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

        String errMessage = null;
        TypeLoadException tLE = new TypeLoadException(errMessage);

        String expectedValue = "Failure has occurred while loading a type.";
        //when message is null,it will return the default message.

        String actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2:Get Message when the message is a null string");
        try
        {
            actualValue = tLE.Message;

            if ((actualValue != expectedValue) &
                !(actualValue.Contains("[Arg_TypeLoadException]")))
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


    public bool PosTest3()
    {
        bool retVal = true;

        String errMessage = "";
        TypeLoadException tLE = new TypeLoadException(errMessage);

        String expectedValue = errMessage;
        String actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest3:Get Message when the message is a empty string");
        try
        {
            actualValue = tLE.Message;

            if (!actualValue.Equals(expectedValue))
            {
                TestLibrary.TestFramework.LogError("005", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }


    public bool PosTest4()
    {
        bool retVal = true;

        String errMessage = TestLibrary.Generator.GetString(-55, false, 256, 512);
        TypeLoadException tLE = new TypeLoadException(errMessage);

        String expectedValue = errMessage;
        String actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest4:Get Message when the message is a long random string");
        try
        {
            actualValue = tLE.Message;

            if (!actualValue.Equals(expectedValue))
            {
                TestLibrary.TestFramework.LogError("007", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    #endregion
}
