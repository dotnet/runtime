// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// NextBytes(System.Byte[])
/// </summary>
public class RandomNextBytes
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call NextBytes to fill a byte array");

        try
        {
            byte[] bytes = new byte[1024];
            retVal = VerificationHelper(new Random(-55), bytes, "001.1") && retVal;
            retVal = VerificationHelper(new Random(0), bytes, "001.2") && retVal;
            retVal = VerificationHelper(new Random(Int32.MaxValue), bytes, "001.3") && retVal;
            retVal = VerificationHelper(new Random(-1), bytes, "001.4") && retVal;
            retVal = VerificationHelper(new Random(Byte.MaxValue), bytes, "001.5") && retVal;
            retVal = VerificationHelper(new Random(Byte.MinValue), bytes, "001.6") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call NextBytes to fill a 1 byte array");

        try
        {
            byte[] bytes = new byte[1];
            retVal = VerificationHelper(new Random(-55), bytes, "002.1") && retVal;
            retVal = VerificationHelper(new Random(0), bytes, "002.2") && retVal;
            retVal = VerificationHelper(new Random(Int32.MaxValue), bytes, "002.3") && retVal;
            retVal = VerificationHelper(new Random(-1), bytes, "002.4") && retVal;
            retVal = VerificationHelper(new Random(Byte.MaxValue), bytes, "002.5") && retVal;
            retVal = VerificationHelper(new Random(Byte.MinValue), bytes, "002.6") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException should be thrown when buffer is a null reference");

        try
        {
            Random instance = new Random(-55);

            instance.NextBytes(null);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentNullException is not thrown when buffer is a null reference");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        RandomNextBytes test = new RandomNextBytes();

        TestLibrary.TestFramework.BeginTestCase("RandomNextBytes");

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
    private bool VerificationHelper(Random instance, byte[] bytes, string errorno)
    {
        bool retVal = true;

        instance.NextBytes(bytes);

        for (int i = 0; i < bytes.Length; ++i)
        {
            byte b = bytes[i];
            if (b < 0)
            {
                TestLibrary.TestFramework.LogError(errorno, "NextBytes returns a value less than 0");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] b = " + b + ", i = " + i);
                retVal = false;
            }
        }

        return retVal;
    }
    #endregion
}
