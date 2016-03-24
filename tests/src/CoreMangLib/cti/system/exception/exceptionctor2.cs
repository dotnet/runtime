// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// ctor(System.String)
/// </summary>
public class ExceptionCtor2
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
        string randValue = null;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ctor to construct a new Exception instance");

        try
        {
            randValue = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            Exception ex = new Exception(randValue);

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
            Exception ex = new Exception(null);

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
            Exception ex = new Exception(String.Empty);

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
    #endregion
    #endregion

    public static int Main()
    {
        ExceptionCtor2 test = new ExceptionCtor2();

        TestLibrary.TestFramework.BeginTestCase("ExceptionCtor2");

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
