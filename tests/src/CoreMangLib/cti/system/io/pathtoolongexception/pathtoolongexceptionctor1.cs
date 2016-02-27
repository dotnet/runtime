// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;

/// <summary>
///ctor
/// </summary>
public class PathTooLongExceptionctor1
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Create a new PathTooLongException instance.");

        try
        {
           PathTooLongException myException = new PathTooLongException();
           if (myException==null)
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
  
    #endregion

    #endregion

    public static int Main()
    {
        PathTooLongExceptionctor1 test = new PathTooLongExceptionctor1();

        TestLibrary.TestFramework.BeginTestCase("PathTooLongExceptionctor1");

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
