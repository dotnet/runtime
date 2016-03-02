// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// CompareTo(System.Guid)
/// </summary>
public class GuidCompareTo1
{
    #region Private Fields
    private const int c_GUID_BYTE_ARRAY_SIZE = 16;
    #endregion

    #region Public Methods
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

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call CompareTo to compare with guid itself");

        try
        {
            Guid guid = new Guid();
            int result = guid.CompareTo(guid);
            if ( result != 0)
            {
                TestLibrary.TestFramework.LogError("001.1", "Call CompareTo to compare with guid itself does not return 0");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] guid = " + guid + ", result = " + result);
                retVal = false;
            }

            byte[] bytes = new byte[c_GUID_BYTE_ARRAY_SIZE];
            TestLibrary.Generator.GetBytes(-55, bytes);

            guid = new Guid(bytes);
            result = guid.CompareTo(guid);
            if (result != 0)
            {
                TestLibrary.TestFramework.LogError("001.2", "Call CompareTo to compare with guid itself does not return 0");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] guid = " + guid + ", result = " + result);
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

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call CompareTo to compare with a guid less then it should a positive integer");

        try
        {
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1), true, "002.1") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1), true, "002.2") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1), true, "002.3") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1), true, "002.4") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1), true, "002.5") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1), true, "002.6") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1), true, "002.7") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1), true, "002.8") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1), true, "002.9") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1), true, "002.10") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0), true, "002.11") && retVal;

            // Negative values
            retVal = VerificationHelper(new Guid(-1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(0, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1), true, "002.12") && retVal;
            retVal = VerificationHelper(new Guid(-1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(-1, 0, -1, 1, 1, 1, 1, 1, 1, 1, 1), true, "002.13") && retVal;
            retVal = VerificationHelper(new Guid(-1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(-1, -1, 0, 1, 1, 1, 1, 1, 1, 1, 1), true, "002.14") && retVal;
            retVal = VerificationHelper(new Guid(-1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(-1, -1, -1, 0, 1, 1, 1, 1, 1, 1, 1), true, "002.15") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call CompareTo to compare with a guid less then it should a negative integer");

        try
        {
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1), false, "003.1") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1), false, "003.2") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1), false, "003.3") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1), false, "003.4") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1), false, "003.5") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1), false, "003.6") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1), false, "003.7") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1), false, "003.8") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1), false, "003.9") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1), false, "003.10") && retVal;
            retVal = VerificationHelper(new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2), false, "003.11") && retVal;
            
            // Negative values
            retVal = VerificationHelper(new Guid(2, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(-1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1), false, "003.12") && retVal;
            retVal = VerificationHelper(new Guid(-1, 2, -1, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(-1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1), false, "003.13") && retVal;
            retVal = VerificationHelper(new Guid(-1, -1, 2, 1, 1, 1, 1, 1, 1, 1, 1),
                new Guid(-1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1), false, "003.14") && retVal;
            retVal = VerificationHelper(new Guid(-1, -1, -1, 0, 1, 1, 1, 1, 1, 1, 1),
                new Guid(-1, -1, -1, 1, 1, 1, 1, 1, 1, 1, 1), false, "003.15") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call CompareTo to compare an guid with all field set to 0 and an guid all field set to F");

        try
        {
            retVal = VerificationHelper(new Guid("00000000-0000-0000-0000-000000000000"),
                new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"), false, "004.1") && retVal;
            retVal = VerificationHelper(new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"),
                new Guid("00000000-0000-0000-0000-000000000000"), true, "004.2") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        GuidCompareTo1 test = new GuidCompareTo1();

        TestLibrary.TestFramework.BeginTestCase("GuidCompareTo1");

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

    #region Private Methods
    private bool VerificationHelper(Guid guid1, Guid guid2, bool greaterThanZero, string errorNo)
    {
        bool retVal = true;

        int result = guid1.CompareTo(guid2);
        bool actual = result > 0;

        if (actual != greaterThanZero)
        {
            TestLibrary.TestFramework.LogError(errorNo, "Call CompareTo to compare with guid itself does not return 0");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] guid1 = " + guid1 + ", guid2 = " + guid2 + ", greaterThanZero = " + greaterThanZero + ", result = " + result + ", actual = " + actual);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
