// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;

/// <summary>
///ctor(System.String)
/// </summary>
public class DirectoryNotFoundExceptionctor1
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Create a new DirectoryNotFoundException instance.");

        try
        {
            string expectString = "This is an error";
            DirectoryNotFoundException myException = new DirectoryNotFoundException(expectString);
            if (myException.Message != expectString)
            {
                TestLibrary.TestFramework.LogError("001.1", "the DirectoryNotFoundException ctor error occurred.the message should be " + expectString);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Create a new DirectoryNotFoundException instance,string is empty.");

        try
        {
            string expectString = string.Empty;
            DirectoryNotFoundException myException = new DirectoryNotFoundException(expectString);
            if (myException.Message != expectString)
            {
                TestLibrary.TestFramework.LogError("002.1", "the DirectoryNotFoundException ctor error occurred. the message should be " + expectString);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Create a new DirectoryNotFoundException instance,string is null.");

        try
        {
            string expectString = null;
            DirectoryNotFoundException myException = new DirectoryNotFoundException(expectString);
            if (myException == null)
            {
                TestLibrary.TestFramework.LogError("003.1", "the DirectoryNotFoundException ctor error occurred. the message should be null");
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
        DirectoryNotFoundExceptionctor1 test = new DirectoryNotFoundExceptionctor1();

        TestLibrary.TestFramework.BeginTestCase("DirectoryNotFoundExceptionctor1");

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
