// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// FormatException Constructor () 
/// </summary>
public class FormatExceptionCtor
{
    public static int Main()
    {
        FormatExceptionCtor testObj = new FormatExceptionCtor();

        TestLibrary.TestFramework.BeginTestCase("for constructor: FormatException()");
        if (testObj.RunTests())
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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        string testDesc = "PosTest1: Initializes a new instance of FormatException.";
        string errorDesc;

        FormatException formatException;

        TestLibrary.TestFramework.BeginScenario(testDesc);
        try
        {
            formatException = new FormatException();
            if(null == formatException || formatException.InnerException != null)
            {
                errorDesc = "Failed to initialize instance of FormatException";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
