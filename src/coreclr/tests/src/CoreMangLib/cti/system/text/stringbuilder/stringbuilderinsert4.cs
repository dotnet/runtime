// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// Insert(System.Int32,System.String,System.Int32)
/// </summary>
public class StringBuilderInsert4
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
        retVal = PosTest5() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;
        string randString = null;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call insert on an empty string builder instance");

        try
        {
            randString = TestLibrary.Generator.GetString(-55, false, c_LENGTH_OF_STRING, c_LENGTH_OF_STRING);
            StringBuilder builder = new StringBuilder();
            StringBuilder newBuilder = builder.Insert(0, randString, 1);

            string actualString = newBuilder.ToString();
            if (!actualString.Equals(randString))
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling insert on an empty string builder instance returns wrong string builder instance");
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
        int randCount = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call insert on a non empty string builder instance");

        try
        {
            randString = TestLibrary.Generator.GetString(-55, false, c_LENGTH_OF_STRING, c_LENGTH_OF_STRING);
            randIndex = TestLibrary.Generator.GetByte(-55);
            randCount = TestLibrary.Generator.GetByte(-55);

            StringBuilder builder = new StringBuilder(randString);
            StringBuilder newBuilder = builder.Insert(randIndex, randString, randCount);
            char[] characters = new char[randString.Length + randCount * randString.Length];
            int index = 0;
            for (int i = 0; i < randIndex; ++i)
            {
                characters[index++] = randString[i];
            }
            for (int c = 0; c < randCount; ++c)
            {
                for (int i = 0; i < randString.Length; ++i)
                {
                    characters[index++] = randString[i];
                }
            }
            for (int i = randIndex; i < randString.Length; ++i)
            {
                characters[index++] = randString[i];
            }

            string desiredString = new string(characters);
            string actualString = newBuilder.ToString();
            if (!desiredString.Equals(actualString))
            {
                TestLibrary.TestFramework.LogError("002.1", "Calling insert on a non empty string builder instance returns wrong string builder instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] randString = " + randString + ", actualString = " + actualString + ", desiredString = " + desiredString + ", randIndex = " + randIndex + ", randCount = " + randCount);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] randString = " + randString + ", randIndex = " + randIndex + ", randCount = " + randCount);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string randString = null;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call insert on an empty string builder instance and with count is 0");

        try
        {
            randString = TestLibrary.Generator.GetString(-55, false, c_LENGTH_OF_STRING, c_LENGTH_OF_STRING);
            StringBuilder builder = new StringBuilder();
            StringBuilder newBuilder = builder.Insert(0, randString, 0);

            string actualString = newBuilder.ToString();
            if (!actualString.Equals(String.Empty))
            {
                TestLibrary.TestFramework.LogError("003.1", "Calling insert on an empty string builder instance and with count is 0 returns wrong string builder instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] randString = " + randString + ", actualString = " + actualString);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] randString = " + randString);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call insert on an empty string builder instance and with value is null reference");

        try
        {
            StringBuilder builder = new StringBuilder();
            StringBuilder newBuilder = builder.Insert(0, null, 1);

            string actualString = newBuilder.ToString();
            if (!actualString.Equals(String.Empty))
            {
                TestLibrary.TestFramework.LogError("004.1", "Calling insert on an empty string builder instance and with value is null reference returns wrong string builder instance");
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

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Call insert on an empty string builder instance and with value is String.Empty");

        try
        {
            StringBuilder builder = new StringBuilder();
            StringBuilder newBuilder = builder.Insert(0, String.Empty, 1);

            string actualString = newBuilder.ToString();
            if (!actualString.Equals(String.Empty))
            {
                TestLibrary.TestFramework.LogError("005.1", "Calling insert on an empty string builder instance and with value is String.Empty returns wrong string builder instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] actualString = " + actualString);
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

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentOutOfRangeException should be thrown when index is less than zero");

        try
        {
            string randString = TestLibrary.Generator.GetString(-55, false, c_LENGTH_OF_STRING, c_LENGTH_OF_STRING);
            StringBuilder builder = new StringBuilder();

            builder.Insert(-1, randString, 1);

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

        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentOutOfRangeException should be thrown when index is greater than the current length of this instance");

        try
        {
            string randString = TestLibrary.Generator.GetString(-55, false, c_LENGTH_OF_STRING, c_LENGTH_OF_STRING);
            StringBuilder builder = new StringBuilder();

            builder.Insert(1, randString, 1);

            TestLibrary.TestFramework.LogError("102.1", "ArgumentOutOfRangeException is not thrown when index is greater than the current length of this instance");
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

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: ArgumentOutOfRangeException should be thrown when count is less than zero. ");

        try
        {
            string randString = TestLibrary.Generator.GetString(-55, false, c_LENGTH_OF_STRING, c_LENGTH_OF_STRING);
            StringBuilder builder = new StringBuilder();

            builder.Insert(0, randString, -1);

            TestLibrary.TestFramework.LogError("103.1", "ArgumentOutOfRangeException is not thrown when count is less than zero. ");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("103.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: OutOfMemoryException should be thrown when The current length of this StringBuilder object plus the length of value times count exceeds MaxCapacity.");

        try
        {
            string randString = TestLibrary.Generator.GetString(-55, false, c_LENGTH_OF_STRING, c_LENGTH_OF_STRING);
            StringBuilder builder = new StringBuilder();

            builder.Insert(0, randString, Int32.MaxValue);

            TestLibrary.TestFramework.LogError("104.1", "OutOfMemoryException is not thrown when The current length of this StringBuilder object plus the length of value times count exceeds MaxCapacity.");
            retVal = false;
        }
        catch (OutOfMemoryException) // StringBuilder new implementation is now throwing OutOfMemoryException
        {
            
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }


        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        StringBuilderInsert4 test = new StringBuilderInsert4();

        TestLibrary.TestFramework.BeginTestCase("StringBuilderInsert4");

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
