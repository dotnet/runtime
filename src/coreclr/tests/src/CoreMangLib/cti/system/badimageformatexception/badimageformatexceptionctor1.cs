// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// ctor
/// </summary>
public class BadImageFormatExceptionCtor1
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ctor to create a new instance");

        try
        {
            BadImageFormatException ex = new BadImageFormatException();
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
    #endregion
    #endregion

    public static int Main()
    {
        BadImageFormatExceptionCtor1 test = new BadImageFormatExceptionCtor1();

        TestLibrary.TestFramework.BeginTestCase("BadImageFormatExceptionCtor1");

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
