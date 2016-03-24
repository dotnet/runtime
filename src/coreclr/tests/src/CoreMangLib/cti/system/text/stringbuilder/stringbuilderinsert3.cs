// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// Insert(System.Int32,System.String)
/// </summary>
public class StringBuilderInsert3
{
    #region Private Fields
    private const int c_LENGTH_OF_STRING = 256;
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;
        string randString = null;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call insert on empty string builder instance");

        try
        {
            randString = TestLibrary.Generator.GetString(-55, false, c_LENGTH_OF_STRING, c_LENGTH_OF_STRING);

            StringBuilder builder = new StringBuilder();
            StringBuilder newBuilder = builder.Insert(0, randString);

            string actualString = newBuilder.ToString();
            if (!randString.Equals(actualString))
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling insert on empty string builder instance returns wrong string builder instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] randString = " + randString + ", actualString = " + actualString);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] randString = " + randString);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string randString = null;
        int randIndex = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call insert on a non empty string builder instance");

        try
        {
            randString = TestLibrary.Generator.GetString(-55, false, c_LENGTH_OF_STRING, c_LENGTH_OF_STRING);
            randIndex = TestLibrary.Generator.GetByte(-55);

            StringBuilder builder = new StringBuilder(randString);
            StringBuilder newBuilder = builder.Insert(randIndex, randString);

            string actualString = newBuilder.ToString();
            char[] characters = new char[randString.Length + randString.Length];
            int index = 0;
            for (int i = 0; i < randIndex; ++i)
            {
                characters[index++] = randString[i];
            }
            for (int i = 0; i < randString.Length; ++i)
            {
                characters[index++] = randString[i];
            }
            for (int i = randIndex; i < randString.Length; ++i)
            {
                characters[index++] = randString[i];
            }
            string desiredString = new string(characters);

            if (!desiredString.Equals(actualString))
            {
                TestLibrary.TestFramework.LogError("002.1", "Calling insert on a non empty string builder instance returns wrong string builder instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] randString = " + randString + ", actualString = " + actualString + ", desiredString = " + desiredString + ", randIndex = " + randIndex);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] randString = " + randString + ", randIndex = " + randIndex);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call insert on empty string builder instance with value is null reference");

        try
        {
            StringBuilder builder = new StringBuilder();
            StringBuilder newBuilder = builder.Insert(0, null as string);

            string actualString = newBuilder.ToString();
            if (!actualString.Equals(String.Empty))
            {
                TestLibrary.TestFramework.LogError("003.1", "Calling insert on empty string builder instance with value is null reference returns wrong string builder instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] actualString = " + actualString);
                retVal = false;
            }
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call insert on empty string builder instance with value is String.Empty");

        try
        {
            StringBuilder builder = new StringBuilder();
            StringBuilder newBuilder = builder.Insert(0, String.Empty);

            string actualString = newBuilder.ToString();
            if (!actualString.Equals(String.Empty))
            {
                TestLibrary.TestFramework.LogError("004.1", "Calling insert on empty string builder instance with value is String.Empty returns wrong string builder instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] actualString = " + actualString);
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentOutOfRangeException should be thrown when index is less than zero");

        try
        {
            StringBuilder builder = new StringBuilder();
            builder.Insert(-1, String.Empty);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentOutOfRangeException is not thrown when index is less than zero");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentOutOfRangeException should be thrown when index is greater than the current length of this instance.");

        try
        {
            StringBuilder builder = new StringBuilder();
            builder.Insert(1, String.Empty);

            TestLibrary.TestFramework.LogError("102.1", "ArgumentOutOfRangeException is not thrown when index is greater than the current length of this instance.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
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
        StringBuilderInsert3 test = new StringBuilderInsert3();

        TestLibrary.TestFramework.BeginTestCase("StringBuilderInsert3");

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
