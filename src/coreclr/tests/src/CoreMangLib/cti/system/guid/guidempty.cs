// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Empty
/// </summary>
public class GuidEmpty
{
    #region Private Fields
    private const string c_EMPTY_GUID = "00000000-0000-0000-0000-000000000000";
    #endregion

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: The values of Guid.Empty should be all zero");

        try
        {
            string actual = Guid.Empty.ToString();

            if (actual != c_EMPTY_GUID)
            {
                TestLibrary.TestFramework.LogError("001.1", "The values of Guid.Empty are not all zero");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] actual = " + actual);
                retVal = false;
            }
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
        GuidEmpty test = new GuidEmpty();

        TestLibrary.TestFramework.BeginTestCase("GuidEmpty");

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
