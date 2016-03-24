// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Equals(System.Object)
/// </summary>
public class GuidEquals2
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
        retVal = PosTest5() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Equals with self instance");

        try
        {
            Guid guid = Guid.Empty;

            if (!guid.Equals((object)guid))
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling Equals with self instance returns false");
                retVal = false;
            }

            // double check
            if (!guid.Equals((object)guid))
            {
                TestLibrary.TestFramework.LogError("001.2", "Calling Equals with self instance returns false");
                retVal = false;
            }

            byte[] bytes = new byte[c_GUID_BYTE_ARRAY_SIZE];
            TestLibrary.Generator.GetBytes(-55, bytes);
            guid = new Guid(bytes);

            if (!guid.Equals((object)guid))
            {
                TestLibrary.TestFramework.LogError("001.3", "Calling Equals with self instance returns false");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] guid = " + guid);
                retVal = false;
            }

            // double check
            if (!guid.Equals((object)guid))
            {
                TestLibrary.TestFramework.LogError("001.4", "Calling Equals with self instance returns false");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] guid = " + guid);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call Equals with equal instance");

        try
        {
            Guid guid1 = Guid.Empty;
            Guid guid2 = new Guid("00000000-0000-0000-0000-000000000000");

            if (!guid1.Equals((object)guid2))
            {
                TestLibrary.TestFramework.LogError("002.1", "Calling Equals with equal instance returns false");
                retVal = false;
            }

            // double check
            if (!guid1.Equals((object)guid2))
            {
                TestLibrary.TestFramework.LogError("002.2", "Calling Equals with equal instance returns false");
                retVal = false;
            }

            byte[] bytes = new byte[c_GUID_BYTE_ARRAY_SIZE];
            TestLibrary.Generator.GetBytes(-55, bytes);
            guid1 = new Guid(bytes);
            guid2 = new Guid(bytes);

            if (!guid1.Equals((object)guid2))
            {
                TestLibrary.TestFramework.LogError("002.3", "Calling Equals with equal instance returns false");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] guid1 = " + guid1 + ", guid2 = " + guid2);
                retVal = false;
            }

            // double check
            if (!guid1.Equals((object)guid2))
            {
                TestLibrary.TestFramework.LogError("002.4", "Calling Equals with equal instance returns false");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] guid1 = " + guid1 + ", guid2 = " + guid2);
                retVal = false;
            }
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call Equals with not equal instance");

        try
        {
            retVal = VerificationHelper(Guid.Empty, new Guid("00000000-0000-0000-0000-000000000001"), false, "003.1") && retVal;
            retVal = VerificationHelper(Guid.Empty, new Guid("00000000-0000-0000-0000-000000000100"), false, "003.2") && retVal;
            retVal = VerificationHelper(Guid.Empty, new Guid("00000000-0000-0000-0000-000000010000"), false, "003.3") && retVal;
            retVal = VerificationHelper(Guid.Empty, new Guid("00000000-0000-0000-0000-000001000000"), false, "003.4") && retVal;
            retVal = VerificationHelper(Guid.Empty, new Guid("00000000-0000-0000-0000-000100000000"), false, "003.5") && retVal;
            retVal = VerificationHelper(Guid.Empty, new Guid("00000000-0000-0000-0000-010000000000"), false, "003.6") && retVal;
            retVal = VerificationHelper(Guid.Empty, new Guid("00000000-0000-0000-0001-000000000000"), false, "003.7") && retVal;
            retVal = VerificationHelper(Guid.Empty, new Guid("00000000-0000-0000-0100-000000000000"), false, "003.8") && retVal;
            retVal = VerificationHelper(Guid.Empty, new Guid("00000000-0000-0001-0000-000000000000"), false, "003.9") && retVal;
            retVal = VerificationHelper(Guid.Empty, new Guid("00000000-0000-0100-0000-000000000000"), false, "003.10") && retVal;
            retVal = VerificationHelper(Guid.Empty, new Guid("00000000-0001-0000-0000-000000000000"), false, "003.11") && retVal;
            retVal = VerificationHelper(Guid.Empty, new Guid("00000000-0100-0000-0000-000000000000"), false, "003.12") && retVal;
            retVal = VerificationHelper(Guid.Empty, new Guid("00000001-0000-0000-0000-000000000000"), false, "003.13") && retVal;
            retVal = VerificationHelper(Guid.Empty, new Guid("00000100-0000-0000-0000-000000000000"), false, "003.14") && retVal;
            retVal = VerificationHelper(Guid.Empty, new Guid("00010000-0000-0000-0000-000000000000"), false, "003.15") && retVal;
            retVal = VerificationHelper(Guid.Empty, new Guid("01000000-0000-0000-0000-000000000000"), false, "003.16") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call Equals with null reference");

        try
        {
            Guid guid = Guid.Empty;

            if (guid.Equals(null))
            {
                TestLibrary.TestFramework.LogError("004.1", "Calling Equals with null reference returns true");
                retVal = false;
            }

            // double check
            if (guid.Equals(null))
            {
                TestLibrary.TestFramework.LogError("004.2", "Calling Equals with null reference returns true");
                retVal = false;
            }

            byte[] bytes = new byte[c_GUID_BYTE_ARRAY_SIZE];
            TestLibrary.Generator.GetBytes(-55, bytes);
            guid = new Guid(bytes);

            if (guid.Equals(null))
            {
                TestLibrary.TestFramework.LogError("004.3", "Calling Equals with null reference returns true");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] guid = " + guid);
                retVal = false;
            }

            // double check
            if (guid.Equals(null))
            {
                TestLibrary.TestFramework.LogError("004.4", "Calling Equals with null reference returns true");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] guid = " + guid);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Call Equals with not a Guid instance");

        try
        {
            Guid guid = Guid.Empty;
            Object obj = new Object();

            if (guid.Equals(obj))
            {
                TestLibrary.TestFramework.LogError("005.1", "Calling Equals with not a Guid instance returns true");
                retVal = false;
            }

            // double check
            if (guid.Equals(obj))
            {
                TestLibrary.TestFramework.LogError("005.2", "Calling Equals with not a Guid instance returns true");
                retVal = false;
            }

            byte[] bytes = new byte[c_GUID_BYTE_ARRAY_SIZE];
            TestLibrary.Generator.GetBytes(-55, bytes);
            guid = new Guid(bytes);

            if (guid.Equals(obj))
            {
                TestLibrary.TestFramework.LogError("005.3", "Calling Equals with not a Guid instance returns true");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] guid = " + guid);
                retVal = false;
            }

            // double check
            if (guid.Equals(obj))
            {
                TestLibrary.TestFramework.LogError("005.4", "Calling Equals with not a Guid instance returns true");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] guid = " + guid);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        GuidEquals2 test = new GuidEquals2();

        TestLibrary.TestFramework.BeginTestCase("GuidEquals2");

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
    private bool VerificationHelper(Guid guid1, object guid2, bool expected, string errorNo)
    {
        bool retVal = true;

        bool actual = guid1.Equals(guid2);

        if (actual != expected)
        {
            TestLibrary.TestFramework.LogError(errorNo, "Calling Equals returns wrong result");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] guid1 = " + guid1 + ", guid2 = " + guid2 + ", actual = " + actual + ", expected = " + expected);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
