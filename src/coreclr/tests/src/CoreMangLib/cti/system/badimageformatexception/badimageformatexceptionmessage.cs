// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Message
/// </summary>
public class BadImageFormatExceptionMessage
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Message should not return null reference for empty instance");

        try
        {
            BadImageFormatException ex = new BadImageFormatException();
            if (ex.Message == null)
            {
                TestLibrary.TestFramework.LogError("001.1", "Message returns null reference for empty instance");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Message should return correct value for instance with message is set");

        try
        {
            string expected = TestLibrary.Generator.GetString(-55, false, 1, 256);
            BadImageFormatException ex = new BadImageFormatException(expected);
            string actual = ex.Message;
            if (expected != actual)
            {
                TestLibrary.TestFramework.LogError("001.1", "Message returns wrong value for instance with message is set");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] expected = " + expected + ", actual = " + actual);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Message should return correct value for instance with message and inner is set");

        try
        {
            string expected = TestLibrary.Generator.GetString(-55, false, 1, 256);
            BadImageFormatException ex = new BadImageFormatException(expected, new Exception());
            string actual = ex.Message;
            if (expected != actual)
            {
                TestLibrary.TestFramework.LogError("001.1", "Message returns wrong value for instance with message and inner is set");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] expected = " + expected + ", actual = " + actual);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        BadImageFormatExceptionMessage test = new BadImageFormatExceptionMessage();

        TestLibrary.TestFramework.BeginTestCase("BadImageFormatExceptionMessage");

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
