// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// ToString(System.Int32)
/// </summary>

public class VersionToString2
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method ToString(int)");

        try
        {
            int intMajor = TestLibrary.Generator.GetInt32(-55);
            int intMinor = TestLibrary.Generator.GetInt32(-55);
            int intBuild = TestLibrary.Generator.GetInt32(-55);
            int intRevision = TestLibrary.Generator.GetInt32(-55);
            Version  version= new Version(intMajor, intMinor, intBuild, intRevision);

            if (   version.ToString(0) != ""
                || version.ToString(1) != intMajor.ToString()
                || version.ToString(2) != intMajor.ToString() + "." + intMinor.ToString()
                || version.ToString(3) != intMajor.ToString() + "." + intMinor.ToString() + "." + intBuild.ToString()
                || version.ToString(4) != intMajor.ToString() + "." + intMinor.ToString() + "." + intBuild.ToString() + "." + intRevision.ToString())
            {
                Console.WriteLine("excepted value is :" + version.ToString(0) + " "
                                                        + version.ToString(1) + " "
                                                        + version.ToString(2) + " "
                                                        + version.ToString(3) + " "
                                                        + version.ToString(4));

                Console.WriteLine("actual value is :" + intMajor + " " + intMinor + " "
                                                      + intBuild + " " + intRevision);

                TestLibrary.TestFramework.LogError("001.1", "ToString() failed!");
                retVal = false;
                return retVal;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentException is not thrown.");

        try
        {
            Version version = new Version(1, 3, 5);
            string str = version.ToString(4);

            TestLibrary.TestFramework.LogError("101.1", " ArgumentException is not thrown.");
            retVal = false;
        }
        catch (ArgumentException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentException is not thrown.");

        try
        {
            Version version = new Version(1, 3, 5);
            string str = version.ToString(-1);

            TestLibrary.TestFramework.LogError("102.1", " ArgumentException is not thrown.");
            retVal = false;
        }
        catch (ArgumentException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        VersionToString2 test = new VersionToString2();

        TestLibrary.TestFramework.BeginTestCase("VersionToString2");

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
