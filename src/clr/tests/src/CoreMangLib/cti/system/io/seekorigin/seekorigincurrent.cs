// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
/// <summary>
/// System.IO.SeekOrigin.Current
/// </summary>
public class SeekOriginCurrent
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check the value of SeekOriginCurrent");

        try
        {
            SeekOrigin seekOrigin = (SeekOrigin)1;
            if (SeekOrigin.Current != seekOrigin)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,The value is: " + (Int32)SeekOrigin.Current);
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
        SeekOriginCurrent test = new SeekOriginCurrent();

        TestLibrary.TestFramework.BeginTestCase("SeekOriginCurrent");

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
