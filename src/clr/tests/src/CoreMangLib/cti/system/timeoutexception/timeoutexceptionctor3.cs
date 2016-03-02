// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class TestException : Exception
{
}

/// <summary>
/// ctor(System.String,System.Exception) [v-yishi]
/// </summary>
public class TimeoutExceptionCtor3
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ctor with null");

        try
        {
            TimeoutException ex = new TimeoutException(null, null);
            if (null == ex)
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling ctor will null returns null reference");
                retVal = false;
            }

            string message = ex.Message;
            // using default message
            if (message == null)
            {
                TestLibrary.TestFramework.LogError("001.2", "Calling ctor will null returns null message");
                retVal = false;
            }

            Exception innerException = ex.InnerException;
            // using default message
            if (innerException != null)
            {
                TestLibrary.TestFramework.LogError("001.3", "Calling ctor will null returns not null innerException");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call ctor with string.Empty");

        try
        {
            TimeoutException ex = new TimeoutException(string.Empty, null);
            if (null == ex)
            {
                TestLibrary.TestFramework.LogError("002.1", "Calling ctor will string.Empty returns null reference");
                retVal = false;
            }

            string message = ex.Message;
            // using default message
            if (message == null)
            {
                TestLibrary.TestFramework.LogError("002.2", "Calling ctor will string.Empty returns null message");
                retVal = false;
            }

            Exception innerException = ex.InnerException;
            // using default message
            if (innerException != null)
            {
                TestLibrary.TestFramework.LogError("002.3", "Calling ctor will string.Empty returns not null innerException");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call ctor with rand message");

        try
        {
            string expected = TestLibrary.Generator.GetString(-55, false, 1, 256);
            Exception expectedException = new TestException();
            TimeoutException ex = new TimeoutException(expected, expectedException);
            if (null == ex)
            {
                TestLibrary.TestFramework.LogError("003.1", "Calling ctor will rand message returns null reference");
                retVal = false;
            }

            string message = ex.Message;
            // using default message
            if (message != expected)
            {
                TestLibrary.TestFramework.LogError("003.2", "Calling ctor will rand message returns null message");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] message = " + message + ", expected = " + expected);
                retVal = false;
            }

            Exception innerException = ex.InnerException;
            // using default message
            if (innerException != expectedException)
            {
                TestLibrary.TestFramework.LogError("003.3", "Calling ctor will string.Empty returns wrong inner exception");
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
        TimeoutExceptionCtor3 test = new TimeoutExceptionCtor3();

        TestLibrary.TestFramework.BeginTestCase("TimeoutExceptionCtor3");

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
