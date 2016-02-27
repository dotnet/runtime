// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Next(System.Int32,System.Int32)
/// </summary>
public class RandomNext3
{
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
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Next when the seed is default value");

        try
        {
            int maxValue = 0;
            do
            {
                maxValue = TestLibrary.Generator.GetInt32(-55);
            } while (maxValue == Int32.MaxValue);

            int minValue = 0;
            do
            {
                minValue = TestLibrary.Generator.GetInt32(-55);
            } while (minValue >= maxValue);

            retVal = VerificationHelper(new Random(), minValue, maxValue, "001.1") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call Next when the seed is Int32.MaxValue");

        try
        {
            int maxValue = 0;
            do
            {
                maxValue = TestLibrary.Generator.GetInt32(-55);
            } while (maxValue == Int32.MaxValue);

            int minValue = 0;
            do
            {
                minValue = TestLibrary.Generator.GetInt32(-55);
            } while (minValue >= maxValue);

            retVal = VerificationHelper(new Random(Int32.MaxValue), minValue, maxValue, "002.1") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call Next when the seed is 0");

        try
        {
            int maxValue = 0;
            do
            {
                maxValue = TestLibrary.Generator.GetInt32(-55);
            } while (maxValue == Int32.MaxValue);

            int minValue = 0;
            do
            {
                minValue = TestLibrary.Generator.GetInt32(-55);
            } while (minValue >= maxValue);

            Random random = new Random(0);

            retVal = VerificationHelper(new Random(maxValue), minValue, maxValue, "003.1") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call Next when the seed is a negative value");

        try
        {
            int randValue = 0;
            do
            {
                randValue = TestLibrary.Generator.GetInt32(-55);
            } while ((randValue == Int32.MinValue) || (randValue == 0));

            if (randValue > 0)
            {
                randValue *= -1;
            }

            int maxValue = 0;
            do
            {
                maxValue = TestLibrary.Generator.GetInt32(-55);
            } while (maxValue == Int32.MaxValue);

            int minValue = 0;
            do
            {
                minValue = TestLibrary.Generator.GetInt32(-55);
            } while (minValue >= maxValue);

            Random random = new Random(randValue);

            retVal = VerificationHelper(new Random(maxValue), minValue, maxValue, "004.1") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Call Next when the seed is maxValue");

        try
        {
            int maxValue = 0;
            do
            {
                maxValue = TestLibrary.Generator.GetInt32(-55);
            } while (maxValue == Int32.MaxValue);

            int minValue = 0;
            do
            {
                minValue = TestLibrary.Generator.GetInt32(-55);
            } while (minValue >= maxValue);

            Random random = new Random(maxValue);

            retVal = VerificationHelper(new Random(maxValue), minValue, maxValue, "005.1") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Call Next when seed, minvalue or maxvalue is boundary");

        try
        {
            retVal = VerificationHelper(new Random(0), 0, 0, "006.1") && retVal;
            retVal = VerificationHelper(new Random(0), Int32.MaxValue, Int32.MaxValue, "006.2") && retVal;
            retVal = VerificationHelper(new Random(0), Int32.MinValue, Int32.MaxValue, "006.3") && retVal;
            retVal = VerificationHelper(new Random(0), Int32.MaxValue - 1, Int32.MaxValue, "006.4") && retVal;
            retVal = VerificationHelper(new Random(Int32.MaxValue), Int32.MaxValue - 1, Int32.MaxValue, "006.5") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest7: Call Next when the seed is minValue");

        try
        {
            int maxValue = 0;
            do
            {
                maxValue = TestLibrary.Generator.GetInt32(-55);
            } while (maxValue == Int32.MaxValue);

            int minValue = 0;
            do
            {
                minValue = TestLibrary.Generator.GetInt32(-55);
            } while (minValue >= maxValue);

            Random random = new Random(minValue);

            retVal = VerificationHelper(new Random(maxValue), minValue, maxValue, "005.1") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentOutOfRangeException should be thrown when minValue is greater than maxValue");

        try
        {
            Random instance = new Random(-55);

            int value = instance.Next(1, 0);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentOutOfRangeException is not thrown when maxValue is less than minValue.");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] value = " + value);
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        RandomNext3 test = new RandomNext3();

        TestLibrary.TestFramework.BeginTestCase("RandomNext3");

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
    private bool VerificationHelper(Random instance, int minValue, int maxValue, string errorno)
    {
        bool retVal = true;

        int value = instance.Next(minValue, maxValue);

        //                                  If minValue equals maxValue, minValue is returned
        if ((value < minValue) || ((value >= maxValue) && (minValue != maxValue)) )
        {
            TestLibrary.TestFramework.LogError(errorno, "Next returns a value less than minValue, or equal to maxValue");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] value = " + value + ", maxValue = " + maxValue + ", minValue = " + minValue);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
