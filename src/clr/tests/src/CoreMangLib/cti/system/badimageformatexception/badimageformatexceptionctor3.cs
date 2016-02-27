// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// ctor(System.String,System.Exception)
/// </summary>
public class BadImageFormatExceptionCtor3
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }
    
    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ctor to create a new instance");

        try
        {
            Exception inner = new Exception();
            BadImageFormatException ex = new BadImageFormatException(TestLibrary.Generator.GetString(-55, false, 1, 256), inner);
            if (ex == null)
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling ctor returns null reference");
                retVal = false;
            }

            if (!(ex is BadImageFormatException))
            {
                TestLibrary.TestFramework.LogError("001.2", "Calling ctor returns non BadImageFormatException instance");
                retVal = false;
            }

            if (ex.InnerException != inner)
            {
                TestLibrary.TestFramework.LogError("001.3", "Calling ctor can not set InnerException property");
                retVal = false;
            }

            // check we can throw this instance
            throw ex;
        }
        catch (BadImageFormatException)
        {
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call ctor to create a new instance with message and innerException set to null");

        try
        {
            Exception inner = null;
            BadImageFormatException ex = new BadImageFormatException(null, inner);
            if (ex == null)
            {
                TestLibrary.TestFramework.LogError("002.1", "Calling ctor returns null reference");
                retVal = false;
            }

            if (!(ex is BadImageFormatException))
            {
                TestLibrary.TestFramework.LogError("002.2", "Calling ctor returns non BadImageFormatException instance");
                retVal = false;
            }

            if (ex.InnerException != inner)
            {
                TestLibrary.TestFramework.LogError("002.3", "Calling ctor can not set InnerException property");
                retVal = false;
            }

            // check we can throw this instance
            throw ex;
        }
        catch (BadImageFormatException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        BadImageFormatExceptionCtor3 test = new BadImageFormatExceptionCtor3();

        TestLibrary.TestFramework.BeginTestCase("BadImageFormatExceptionCtor3");

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
