// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// GetHashCode()
/// </summary>

public class VersionGetHashCode
{
    public static int Main()
    {
        VersionGetHashCode test = new VersionGetHashCode();

        TestLibrary.TestFramework.BeginTestCase("VersionGetHashCode");

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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Ensure GetHashCode() successful.");
        Version TestVersion = null;

        try
        {
            for (int i = 0; i < 50; i++)
            {
                int intMajor = TestLibrary.Generator.GetInt32(-55);
                int intMinor = TestLibrary.Generator.GetInt32(-55);
                int intBuild = TestLibrary.Generator.GetInt32(-55);
                int intRevision = TestLibrary.Generator.GetInt32(-55);
                TestVersion = new Version(intMajor, intMinor, intBuild, intRevision);
                int accumulator = 0;
                accumulator |= (TestVersion.Major & 0x0000000F) << 28;
                accumulator |= (TestVersion.Minor & 0x000000FF) << 20;
                accumulator |= (TestVersion.Build & 0x000000FF) << 12;
                accumulator |= (TestVersion.Revision & 0x00000FFF);

                if (TestVersion.GetHashCode() != accumulator)
                {
                    TestLibrary.TestFramework.LogError("P01.1", "GetHashCode() failed!" + TestVersion.ToString());
                    retVal = false;
                    return retVal;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P01.2", "Unexpected exception: " + e + TestVersion.ToString());
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
