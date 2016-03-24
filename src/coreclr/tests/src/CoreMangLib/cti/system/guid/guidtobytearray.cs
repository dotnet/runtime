// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// ToByteArray
/// </summary>
public class GuidToByteArray
{
    #region Private Fields
    private const int c_SIZE_OF_ARRAY = 16;
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ToByteArray on a guid instance");

        try
        {
            do
            {
                byte[] expected = new byte[c_SIZE_OF_ARRAY];
                TestLibrary.Generator.GetBytes(-55, expected);

                Guid guid = new Guid(expected);
                byte[] actual = guid.ToByteArray();

                if (actual == null)
                {
                    TestLibrary.TestFramework.LogError("001.1", "ToByteArray return null reference");
                    retVal = false;
                    break;
                }

                if (actual.Length != expected.Length)
                {
                    TestLibrary.TestFramework.LogError("001.2", "The length of byte array returned by ToByteArray is not the same as original byte array's length");
                    TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual.Length = " + actual.Length + ", expected.Length = " + expected.Length);
                    retVal = false;
                }

                for (int i = 0; i < actual.Length; ++i)
                {
                    if (actual[i] != expected[i])
                    {
                        TestLibrary.TestFramework.LogError("001.3", "The byte array returned by ToByteArray is not the same as original byte array");
                        TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual[i] = " + actual[i] + ", expected[i] = " + expected[i] + ", i = " + i);
                        retVal = false;
                    }
                }
            } while (false);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call ToByteArray on a Guid.Empty");

        try
        {
            do
            {
                byte[] expected = new byte[c_SIZE_OF_ARRAY];
                byte[] actual = Guid.Empty.ToByteArray();

                if (actual == null)
                {
                    TestLibrary.TestFramework.LogError("002.1", "ToByteArray return null reference on a Guid.Empty");
                    retVal = false;
                    break;
                }

                if (actual.Length != expected.Length)
                {
                    TestLibrary.TestFramework.LogError("002.2", "The length of byte array returned by ToByteArray on a Guid.Empty is not the same as original byte array's length");
                    TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual.Length = " + actual.Length + ", expected.Length = " + expected.Length);
                    retVal = false;
                }

                for (int i = 0; i < actual.Length; ++i)
                {
                    if (actual[i] != expected[i])
                    {
                        TestLibrary.TestFramework.LogError("002.3", "The byte array returned by ToByteArray on a Guid.Empty is not the same as original byte array");
                        TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual[i] = " + actual[i] + ", expected[i] = " + expected[i] + ", i = " + i);
                        retVal = false;
                    }
                }
            } while (false);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        GuidToByteArray test = new GuidToByteArray();

        TestLibrary.TestFramework.BeginTestCase("GuidToByteArray");

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
