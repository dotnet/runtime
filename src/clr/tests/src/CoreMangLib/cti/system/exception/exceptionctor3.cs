// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class TestException : Exception
{
}

/// <summary>
/// ctor(System.String,System.Exception)
/// </summary>
public class ExceptionCtor3
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
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;
        string randValue = null;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ctor to construct a new Exception instance");

        try
        {
            randValue = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            Exception ex = new Exception(randValue, null);

            if (ex == null)
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling ctor to construct a new Exception instance returns a null reference");
                retVal = false;
            }

            if (ex.InnerException != null)
            {
                TestLibrary.TestFramework.LogError("001.2", "Calling ctor to construct a new Exception instance returns an instance with InnerException is not a null reference");
                retVal = false;
            }

            if (!ex.Message.Equals(randValue))
            {
                TestLibrary.TestFramework.LogError("001.3", "Calling ctor to construct a new Exception instance returns an instance with Message is wrong");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] ex.Message = " + ex.Message + ", randValue = " + randValue);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] randValue = " + randValue);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call ctor to construct a new Exception instance with message is null reference");

        try
        {
            Exception ex = new Exception(null, null);

            if (ex == null)
            {
                TestLibrary.TestFramework.LogError("002.1", "Calling ctor to construct a new Exception instance with message is null reference returns a null reference");
                retVal = false;
            }

            if (ex.InnerException != null)
            {
                TestLibrary.TestFramework.LogError("002.2", "Calling ctor to construct a new Exception instance with message is null reference returns an instance with InnerException is not a null reference");
                retVal = false;
            }

            if (ex.Message == null)
            {
                TestLibrary.TestFramework.LogError("002.3", "Calling ctor to construct a new Exception instance with message is null reference returns an instance with Message is not default message");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call ctor to construct a new Exception instance with message is String.Empty");

        try
        {
            Exception ex = new Exception(String.Empty, null);

            if (ex == null)
            {
                TestLibrary.TestFramework.LogError("003.1", "Calling ctor to construct a new Exception instance with message is String.Empty returns a null reference");
                retVal = false;
            }

            if (ex.InnerException != null)
            {
                TestLibrary.TestFramework.LogError("003.2", "Calling ctor to construct a new Exception instance with message is String.Empty returns an instance with InnerException is not a null reference");
                retVal = false;
            }

            if (!ex.Message.Equals(String.Empty))
            {
                TestLibrary.TestFramework.LogError("003.3", "Calling ctor to construct a new Exception instance with message is String.Empty returns an instance with Message is wrong");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] ex.Message = " + ex.Message);
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

    public bool PosTest4()
    {
        bool retVal = true;
        string randValue = null;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call ctor to construct a new Exception instance with InnerException is set");

        try
        {
            randValue = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            TestException innerException = new TestException();
            Exception ex = new Exception(randValue, innerException);

            if (ex == null)
            {
                TestLibrary.TestFramework.LogError("004.1", "Calling ctor to construct a new Exception instance returns a null reference");
                retVal = false;
            }

            if (!ex.InnerException.Equals(innerException))
            {
                TestLibrary.TestFramework.LogError("004.2", "Calling ctor to construct a new Exception instance returns an instance with InnerException is not a instance of TestException");
                retVal = false;
            }

            if (!ex.Message.Equals(randValue))
            {
                TestLibrary.TestFramework.LogError("004.3", "Calling ctor to construct a new Exception instance returns an instance with Message is wrong");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] ex.Message = " + ex.Message + ", randValue = " + randValue);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] randValue = " + randValue);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        string randValue = null;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Call ctor to construct a new Exception instance with InnerException is set to Exception's instance");

        try
        {
            randValue = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            Exception innerException = new Exception();
            Exception ex = new Exception(randValue, innerException);

            if (ex == null)
            {
                TestLibrary.TestFramework.LogError("005.1", "Calling ctor to construct a new Exception instance returns a null reference");
                retVal = false;
            }

            if (!ex.InnerException.Equals(innerException))
            {
                TestLibrary.TestFramework.LogError("005.2", "Calling ctor to construct a new Exception instance returns an instance with InnerException is not a instance of Exception");
                retVal = false;
            }

            if (!ex.Message.Equals(randValue))
            {
                TestLibrary.TestFramework.LogError("005.3", "Calling ctor to construct a new Exception instance returns an instance with Message is wrong");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] ex.Message = " + ex.Message + ", randValue = " + randValue);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] randValue = " + randValue);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ExceptionCtor3 test = new ExceptionCtor3();

        TestLibrary.TestFramework.BeginTestCase("ExceptionCtor3");

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
