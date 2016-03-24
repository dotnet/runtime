// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Next(System.Int32)
/// </summary>
public class RandomNext2
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
            } while ((maxValue <= 0) || (maxValue == Int32.MaxValue));

            Random random = new Random(-55);
            int value = random.Next(maxValue);

            if ((value < 0) || (value >= maxValue))
            {
                TestLibrary.TestFramework.LogError("001.1", "Next returns a value less than 0 or equal to maxValue");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] value = " + value + ", maxValue = " + maxValue);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call Next when the seed is Int32.MaxValue");

        try
        {
            int maxValue = 0;
            do
            {
                maxValue = TestLibrary.Generator.GetInt32(-55);
            } while ((maxValue <= 0) || (maxValue == Int32.MaxValue));

            Random random = new Random(Int32.MaxValue);
            int value = random.Next(maxValue);

            if ((value < 0) || (value >= maxValue))
            {
                TestLibrary.TestFramework.LogError("002.1", "Next returns a value less than 0 or equal to maxValue");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] value = " + value + ", maxValue = " + maxValue);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call Next when the seed is 0");

        try
        {
            int maxValue = 0;
            do
            {
                maxValue = TestLibrary.Generator.GetInt32(-55);
            } while ((maxValue <= 0) || (maxValue == Int32.MaxValue));

            Random random = new Random(0);
            int value = random.Next(maxValue);

            if ((value < 0) || (value >= maxValue))
            {
                TestLibrary.TestFramework.LogError("003.1", "Next returns a value less than 0 or equal to maxValue");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] value = " + value + ", maxValue = " + maxValue);
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
            } while ((maxValue <= 0) || (maxValue == Int32.MaxValue));

            Random random = new Random(randValue);
            int value = random.Next(maxValue);

            if ((value < 0) || (value >= maxValue))
            {
                TestLibrary.TestFramework.LogError("004.1", "Next returns a value less than 0 or equal to maxValue");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] value = " + value + ", maxValue = " + maxValue);
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Call Next when the seed is maxValue");

        try
        {
            int maxValue = 0;
            do
            {
                maxValue = TestLibrary.Generator.GetInt32(-55);
            } while ((maxValue <= 0) || (maxValue == Int32.MaxValue));

            Random random = new Random(maxValue);
            int value = random.Next(maxValue);

            if ((value < 0) || (value >= maxValue))
            {
                TestLibrary.TestFramework.LogError("005.1", "Next returns a value less than 0 or equal to maxValue");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] value = " + value + ", maxValue = " + maxValue);
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

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Call Next when the seed is 0 and maxvalue is 0");

        try
        {
            Random random = new Random(0);
            int value = random.Next(0);

            if (value != 0)
            {
                TestLibrary.TestFramework.LogError("006.1", "Next returns a value less than 0 or equal to maxValue");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] value = " + value);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006.0", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentOutOfRangeException should be thrown when maxValue is less than zero.");

        try
        {
            Random random = new Random(-55);
            int value = random.Next(-1);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentOutOfRangeException is not thrown when maxValue is less than zero.");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] value = " + value);
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
    #endregion
    #endregion

    public static int Main()
    {
        RandomNext2 test = new RandomNext2();

        TestLibrary.TestFramework.BeginTestCase("RandomNext2");

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
