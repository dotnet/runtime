// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// CompareTo(System.Version)
/// </summary>

public class VersionCompareTo2
{
    public static int Main()
    {
        VersionCompareTo2 test = new VersionCompareTo2();

        TestLibrary.TestFramework.BeginTestCase("VersionCompareTo2");

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
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Ensure CompareTo(System.Version) successful when the current Version object is a version before version.");

        try
        {
            Version TestVersion1 = new Version("2.3.4.5");
            Version TestVersion2 = new Version("2.3.4.0");
            if (TestVersion2.CompareTo(TestVersion1) >= 0)
            {
                TestLibrary.TestFramework.LogError("P01.1", "CompareTo(System.Version) failed when the current Version object is a version before version!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Ensure CompareTo(System.Version) successful when the current Version object is the same version as version.");

        try
        {
            Version TestVersion1 = new Version("2.3.4.5");
            Version TestVersion2 = new Version("2.3.4.5");
            if (TestVersion2.CompareTo(TestVersion1) != 0)
            {
                TestLibrary.TestFramework.LogError("P02.1", "CompareTo(System.Version) failed when the current Version object is the same version as version!");
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

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Ensure CompareTo(System.Version) successful when the current Version object is a version subsequent to version.");

        try
        {
            Version TestVersion1 = new Version("2.3.4.5");
            Version TestVersion2 = new Version("2.3.4.8");
            if (TestVersion2.CompareTo(TestVersion1) <= 0)
            {
                TestLibrary.TestFramework.LogError("P03.1", "CompareTo(System.Version) failed when the current Version object is a version subsequent to version!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P03.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: Ensure CompareTo(System.Version) successful when version is a null reference.");

        try
        {
            Version TestVersion2 = new Version("2.3.4.8");
            if (TestVersion2.CompareTo(null) <= 0)
            {
                TestLibrary.TestFramework.LogError("P04.1", "CompareTo(System.Version) failed when version is a null reference!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P04.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
