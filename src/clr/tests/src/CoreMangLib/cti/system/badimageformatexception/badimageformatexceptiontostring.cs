// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// ToString
/// </summary>
public class BadImageFormatExceptionToString
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ToString on an empty exception class");

        try
        {
            BadImageFormatException ex = new BadImageFormatException();
            string expected = "System.BadImageFormatException: Format of the executable (.exe) or library (.dll) is invalid.";
            string actual = ex.ToString();

            if (ex.ToString() == null)
            {
                TestLibrary.TestFramework.LogError("001.1", "ToString returns unexpected value");
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

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call ToString on an exception class with message is set");

        try
        {
            string message = TestLibrary.Generator.GetString(-55, false, 1, 50);
            BadImageFormatException ex = new BadImageFormatException(message);
            string expected = "System.BadImageFormatException: " + message;
            string actual = ex.ToString();

            if (expected != actual)
            {
                TestLibrary.TestFramework.LogError("002.1", "ToString returns unexpected value");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] expected = " + expected + ", actual = " + actual);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call ToString on an exception class with message and inner is set");

        try
        {
            string message = TestLibrary.Generator.GetString(-55, false, 1, 50);
            Exception inner = new Exception();
            BadImageFormatException ex = new BadImageFormatException(message, inner);
            string expected = "System.BadImageFormatException: " + message + " ---> " + inner.ToString();
            string actual = ex.ToString();

            if (expected != actual)
            {
                TestLibrary.TestFramework.LogError("003.1", "ToString returns unexpected value");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] expected = " + expected + ", actual = " + actual);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        BadImageFormatExceptionToString test = new BadImageFormatExceptionToString();

        TestLibrary.TestFramework.BeginTestCase("BadImageFormatExceptionToString");

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
