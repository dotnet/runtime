// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// ctor(System.Byte[])
/// </summary>
public class GuidCtor1
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;
        string byteValue = null;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ctor with rand byte array");

        try
        {
            byte[] bytes = new byte[c_GUID_BYTE_ARRAY_SIZE];
            TestLibrary.Generator.GetBytes(-55, bytes);

            for (int i = 0; i < bytes.Length; ++i)
            {
                byteValue += bytes[i] + ", ";
            }

            Guid guid = new Guid(bytes);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] byteValue = " + byteValue);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call ctor with valid empty byte array");

        try
        {
            byte[] bytes = new byte[c_GUID_BYTE_ARRAY_SIZE];

            Guid guid = new Guid(bytes);
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

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException should be thrown when b is a null reference");

        try
        {
            Guid guid = new Guid(null as byte[]);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentNullException is not thrown when b is a null reference");
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

    public bool NegTest2()
    {
        bool retVal = true;
        int arraySize = 0;

        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentException should be thrown when b is not 16 bytes long.");

        try
        {
            do
            {
                arraySize = TestLibrary.Generator.GetByte(-55);
            } while (arraySize == c_GUID_BYTE_ARRAY_SIZE);
            byte[] bytes = new byte[arraySize];

            Guid guid = new Guid(bytes);

            TestLibrary.TestFramework.LogError("102.1", "ArgumentException is not thrown when b is not 16 bytes long.");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        GuidCtor1 test = new GuidCtor1();

        TestLibrary.TestFramework.BeginTestCase("GuidCtor1");

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
