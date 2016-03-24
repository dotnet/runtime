// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;

/// <summary>
///ctor(System.String)
/// </summary>
public class PathTooLongExceptionctor2
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Create a new PathTooLongException instance.");

        try
        {
            string expectString = "This is an error";
            PathTooLongException myException = new PathTooLongException(expectString);
            if (myException.Message != expectString)
            {
                TestLibrary.TestFramework.LogError("001.1", "the PathTooLongException ctor error occurred. ");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Create a new PathTooLongException instance,string is empty.");

        try
        {
            string expectString = string.Empty;
            PathTooLongException myException = new PathTooLongException(expectString);
            if (myException.Message != expectString)
            {
                TestLibrary.TestFramework.LogError("002.1", "the PathTooLongException ctor error occurred. ");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Create a new PathTooLongException instance,string is null.");

        try
        {
            string expectString = null;
            PathTooLongException myException = new PathTooLongException(expectString);
            if (myException == null)
            {
                TestLibrary.TestFramework.LogError("003.1", "the PathTooLongException ctor error occurred. ");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #endregion

    public static int Main()
    {
        PathTooLongExceptionctor2 test = new PathTooLongExceptionctor2();

        TestLibrary.TestFramework.BeginTestCase("PathTooLongExceptionctor2");

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
