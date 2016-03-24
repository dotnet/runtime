// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// Equals(System.Version)
/// </summary>

public class VersionEquals2
{
    public static int Main()
    {
        VersionEquals2 test = new VersionEquals2();

        TestLibrary.TestFramework.BeginTestCase("VersionEquals2");

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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Ensure Equals(System.Version) return true when every component of the current Version object matches the corresponding component of the obj parameter.");

        try
        {
            Version TestVersion1 = new Version("2.3.4.5");
            Version TestVersion2 = new Version("2.3.4.5");
            if (!TestVersion2.Equals(TestVersion1))
            {
                TestLibrary.TestFramework.LogError("P01.1", "Equals(System.Version) failed when every component of the current Version object matches the corresponding component of the obj parameter!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P01.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Ensure Equals(System.Version) return false when not every component of the current Version object matches the corresponding component of the obj parameter.");

        try
        {
            Version TestVersion1 = new Version("2.3.4.5");
            Version TestVersion2 = new Version("2.3.4.6");
            if (TestVersion2.Equals(TestVersion1))
            {
                TestLibrary.TestFramework.LogError("P02.1", "Equals(System.Version) failed when not every component of the current Version object matches the corresponding component of the obj parameter!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P02.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
