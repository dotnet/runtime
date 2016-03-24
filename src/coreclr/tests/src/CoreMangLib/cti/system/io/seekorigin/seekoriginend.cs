// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
/// <summary>
/// System.IO.SeekOrigin.End
/// </summary>
public class SeekOriginEnd
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check the value of SeekOriginEnd");

        try
        {
            SeekOrigin seekOrigin = (SeekOrigin)2;
            if (SeekOrigin.End != seekOrigin)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,The value is: " + (Int32)SeekOrigin.End);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #region Nagetive Test Cases
    #endregion
    #endregion

    public static int Main()
    {
        SeekOriginEnd test = new SeekOriginEnd();

        TestLibrary.TestFramework.BeginTestCase("SeekOriginEnd");

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
