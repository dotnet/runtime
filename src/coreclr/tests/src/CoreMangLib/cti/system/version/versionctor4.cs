// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// ctor(System.String)
/// </summary>

public class VersionCtor4
{
    public static int Main()
    {
        VersionCtor4 test = new VersionCtor4();

        TestLibrary.TestFramework.BeginTestCase("VersionCtor4");

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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        retVal = NegTest7() && retVal;
        retVal = NegTest8() && retVal;
        retVal = NegTest9() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Ensure ctor(System.String) successful.");

        try
        {
            Version TestVersion = new Version("2.3.4.5");
            if (TestVersion.Major != 2 || TestVersion.Minor != 3 || TestVersion.Build != 4 || TestVersion.Revision != 5)
            {
                TestLibrary.TestFramework.LogError("P01.1", "ctor(System.String) failed!");
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

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentOutOfRangeException should be thrown if major is less than zero.");

        try
        {
            Version TestVersion = new Version("-2.3.4.5");
            TestLibrary.TestFramework.LogError("N01.1", "ArgumentOutOfRangeException is not thrown when major is less than zero!");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N01.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentOutOfRangeException should be thrown if minor is less than zero.");

        try
        {
            Version TestVersion = new Version("2.-3.4.5");
            TestLibrary.TestFramework.LogError("N02.1", "ArgumentOutOfRangeException is not thrown when minor is less than zero!");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N02.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest3: ArgumentOutOfRangeException should be thrown if build is less than zero.");

        try
        {
            Version TestVersion = new Version("2.3.-4.5");
            TestLibrary.TestFramework.LogError("N03.1", "ArgumentOutOfRangeException is not thrown when build is less than zero!");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N03.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest4: ArgumentOutOfRangeException should be thrown if revision is less than zero.");

        try
        {
            Version TestVersion = new Version("2.3.4.-5");
            TestLibrary.TestFramework.LogError("N04.1", "ArgumentOutOfRangeException is not thrown when revision is less than zero!");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N04.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest5: ArgumentException should be thrown if version has fewer than two components.");

        try
        {
            Version TestVersion = new Version("2");
            TestLibrary.TestFramework.LogError("N05.1", "ArgumentException is not thrown when version has fewer than two components!");
            retVal = false;
        }
        catch (ArgumentException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N05.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest6: ArgumentException should be thrown if version has more than four components.");

        try
        {
            Version TestVersion = new Version("2.3.4.5.6");
            TestLibrary.TestFramework.LogError("N06.1", "ArgumentException is not thrown when version has more than four components!");
            retVal = false;
        }
        catch (ArgumentException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N06.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest7()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest7: ArgumentNullException should be thrown if version is a null reference.");

        try
        {
            Version TestVersion = new Version(null);
            TestLibrary.TestFramework.LogError("N07.1", "ArgumentNullException is not thrown when version is a null reference!");
            retVal = false;
        }
        catch (ArgumentNullException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N07.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest8()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest8: FormatException should be thrown if one component of version does not parse to an integer.");

        try
        {
            Version TestVersion = new Version("2.w.4.5");
            TestLibrary.TestFramework.LogError("N08.1", "FormatException is not thrown when one component of version does not parse to an integer!");
            retVal = false;
        }
        catch (FormatException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N08.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest9()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest9: OverflowException should be thrown if one component of version represents a number greater than MaxValue.");

        try
        {
            unchecked
            {
                Int64 testInt = Int64.MaxValue;
                String testString = testInt.ToString();
                String resultString = "2.";
                resultString = String.Concat(resultString, testString);
                resultString = String.Concat(resultString, ".4.5");
                Version TestVersion = new Version(resultString);
            }
            TestLibrary.TestFramework.LogError("N09.1", "OverflowException is not thrown when one component of version does not parse to an integer!");
            retVal = false;
        }
        catch (OverflowException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N09.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
