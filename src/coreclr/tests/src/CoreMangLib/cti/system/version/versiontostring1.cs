// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// ToString()
/// </summary>

public class VersionToString1
{
    public static int Main()
    {
        VersionToString1 test = new VersionToString1();

        TestLibrary.TestFramework.BeginTestCase("VersionToString1");

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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Ensure ToString() successful.");
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
                String VersionString = null;
                VersionString = String.Concat(VersionString, intMajor.ToString());
                VersionString = String.Concat(VersionString, ".");
                VersionString = String.Concat(VersionString, intMinor.ToString());
                VersionString = String.Concat(VersionString, ".");
                VersionString = String.Concat(VersionString, intBuild.ToString());
                VersionString = String.Concat(VersionString, ".");
                VersionString = String.Concat(VersionString, intRevision.ToString());

                if (TestVersion.ToString() != VersionString)
                {
                    TestLibrary.TestFramework.LogError("P01.1", "ToString() failed!" + TestVersion.ToString());
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
