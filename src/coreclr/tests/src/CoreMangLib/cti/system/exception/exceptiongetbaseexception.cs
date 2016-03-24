// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class TestException : Exception
{
    public TestException() { }

    public TestException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

public class TestException1 : Exception
{
    public TestException1() { }

    public TestException1(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>
/// GetBaseException
/// </summary>
public class ExceptionGetBaseException
{
    #region Private Fields
    private const int c_MIN_STRING_LENGTH = 1;
    private const int c_MAX_STRING_LENGTH = 256;
    #endregion

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call GetBaseException when InnerException property is null reference");

        try
        {
            Exception desired = new Exception();
            Exception actual = desired.GetBaseException();

            if (!desired.Equals(actual))
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling GetBaseException when InnerException property is null reference does not return current exception instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] desired = " + desired + ", actual = " + actual);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call GetBaseException when InnerException property is not null reference");

        try
        {
            TestException desired = new TestException();
            Exception ex = new Exception(
                TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH),
                desired);
            Exception actual = ex.GetBaseException();

            if (!desired.Equals(actual))
            {
                TestLibrary.TestFramework.LogError("002.1", "Calling GetBaseException when InnerException property is not null reference does not return current exception instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] desired = " + desired + ", actual = " + actual);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call GetBaseException when InnerException property is a chain of Exception");

        try
        {
            TestException desired = new TestException();
            Exception ex = new Exception(
                TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH),
                new TestException1(
                    TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH),
                    desired));
            Exception actual = ex.GetBaseException();

            if (!desired.Equals(actual))
            {
                TestLibrary.TestFramework.LogError("003.1", "Calling GetBaseException when InnerException property is a chain of Exception does not return current exception instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] desired = " + desired + ", actual = " + actual);
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
        ExceptionGetBaseException test = new ExceptionGetBaseException();

        TestLibrary.TestFramework.BeginTestCase("ExceptionGetBaseException");

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
