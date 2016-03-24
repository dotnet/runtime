// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;

/// <summary>
///ctor(System.String)
/// </summary>
public class IOExceptionctor2
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Create a new IOExection instance.");

        try
        {
            string expectString = "this is a error";
            //Create the application domain setup information.
            IOException myIOExection = new IOException(expectString);
            if (myIOExection.Message != expectString)
            {
                TestLibrary.TestFramework.LogError("001.1", "the IOExection ctor error occurred. ");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Create a new IOExection instance,string is empty.");

        try
        {
            string expectString = string.Empty;
            //Create the application domain setup information.
            IOException myIOExection = new IOException(expectString);
            if (myIOExection.Message != expectString)
            {
                TestLibrary.TestFramework.LogError("002.1", "the IOExection ctor error occurred. ");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Create a new IOExection instance,string is null,then its inner exception set to a null reference .");

        try
        {
            string expectString =null;
            //Create the application domain setup information.
            IOException myIOExection = new IOException(expectString);
            if (myIOExection== null)
            {
                TestLibrary.TestFramework.LogError("003.1", "the IOExection ctor error occurred. ");
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
        IOExceptionctor2 test = new IOExceptionctor2();

        TestLibrary.TestFramework.BeginTestCase("IOExceptionctor2");

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
