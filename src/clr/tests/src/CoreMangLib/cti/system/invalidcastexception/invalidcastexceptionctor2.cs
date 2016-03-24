// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// System.InvalidCastException.ctor(String) [v-minch]
/// </summary>
public class InvalidCastExceptionctor2
{
    public static int Main()
    {
        InvalidCastExceptionctor2 invalidCastExceptionctor2 = new InvalidCastExceptionctor2();
        TestLibrary.TestFramework.BeginTestCase("InvalidCastExceptionctor2");
        if (invalidCastExceptionctor2.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Initialize the InvalidCastException instance with message 1");
        try
        {
            string message = "HelloWorld";
            InvalidCastException myException = new InvalidCastException(message);
            if (myException == null || myException.Message != message)
            {
                TestLibrary.TestFramework.LogError("001", "the InvalidCastException with message instance creating failed");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2:Initialize the InvalidCastException instance with message 2");
        try
        {
            string message = null;
            InvalidCastException myException = new InvalidCastException(message);
            if (myException == null || myException.Message == null)
            {
                TestLibrary.TestFramework.LogError("003", "Initialize the InvalidCastException instance with null message not create");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
