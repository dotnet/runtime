// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// CompareTo(System.Double)
/// </summary>
public class DoubleCompareTo1
{
    public static int Main()
    {
        DoubleCompareTo1 test = new DoubleCompareTo1();
        TestLibrary.TestFramework.BeginTestCase("DoubleCompareTo1");

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
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Ensure the return value of CompareTo(System.Double) is less than zero when the instance is less than System.Double.");

        try
        {
            Random random = new Random(-55);
            Double d1, d2;
            do
            {
                d1 = random.NextDouble();
                d2 = random.NextDouble();
            }
            while (d1 >= d2);
            if (d1.CompareTo(d2) >= 0)
            {
                TestLibrary.TestFramework.LogError("001.1", "The return value of CompareTo(System.Double) is not less than zero when the instance is less than System.Double!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Ensure the return value of CompareTo(System.Double) is less than zero when this instance is not a number (NaN) and System.Double is a number.");

        try
        {
            Random random = new Random(-55);
            Double randomDouble;
            do
            {
                randomDouble = random.NextDouble();
            }
            while (Double.IsNaN(randomDouble) == true);

            if (Double.NaN.CompareTo(randomDouble) >= 0)
            {
                TestLibrary.TestFramework.LogError("002.1", "The return value of CompareTo(System.Double) is not less than zero when this instance is not a number (NaN) and System.Double is a number!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Ensure the return value of CompareTo(System.Double) equal to zero when this instance equals to System.Double.");

        try
        {
            Random random = new Random(-55);
            Double d1, d2;
            d1 = random.NextDouble();
            d2 = d1;

            if (d1.CompareTo(d2) != 0)
            {
                TestLibrary.TestFramework.LogError("003.1", "The return value of CompareTo(System.Double) does not equal to zero when this instance equals to System.Double!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: Ensure the return value of CompareTo(System.Double) equal to zero when both this instance and value are NaN.");

        try
        {
            Double d1 = 0.0;
            long myl1 = 0;
            myl1 = myl1 | 0x7ff0000000000001;
            byte[] mybytes1 = { 0, 0, 0, 0, 0, 0, 0, 0 };
            mybytes1 = BitConverter.GetBytes(myl1);
            d1 = BitConverter.ToDouble(mybytes1, 0);

            Double d2 = 0.0;
            long myl2 = 0;
            myl2 = myl2 | 0x7ff0000000000002;
            byte[] mybytes2 = { 0, 0, 0, 0, 0, 0, 0, 0 };
            mybytes2 = BitConverter.GetBytes(myl2);
            d2 = BitConverter.ToDouble(mybytes2, 0);

            if (d1.CompareTo(d2) != 0)
            {
                TestLibrary.TestFramework.LogError("004.1", "The return value of CompareTo(System.Double) does not equal to zero when both this instance and value are NaN!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5: Ensure the return value of CompareTo(System.Double) equal to zero when both this instance and value are PositiveInfinity.");

        try
        {
            Double zero = 0.0;

            if ((1 / zero).CompareTo(2 / zero) != 0)
            {
                TestLibrary.TestFramework.LogError("005.1", "The return value of CompareTo(System.Double) does not equal to zero when both this instance and value are PositiveInfinity!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest6: Ensure the return value of CompareTo(System.Double) is greater than zero when the instance is greater than System.Double.");

        try
        {
            Random random = new Random(-55);
            Double d1, d2;
            do
            {
                d1 = random.NextDouble();
                d2 = random.NextDouble();
            }
            while (d1 <= d2);
            if (d1.CompareTo(d2) <= 0)
            {
                TestLibrary.TestFramework.LogError("006.1", "The return value of CompareTo(System.Double) is not greater than zero when the instance is greater than System.Double!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest7: Ensure the return value of CompareTo(System.Double) is greater than zero when this instance is a number and System.Double is not a number.");

        try
        {
            Random random = new Random(-55);
            Double randomDouble;
            do
            {
                randomDouble = random.NextDouble();
            }
            while (Double.IsNaN(randomDouble) == true);

            if (randomDouble.CompareTo(Double.NaN) <= 0)
            {
                TestLibrary.TestFramework.LogError("007.1", "The return value of CompareTo(System.Double) is not greater than zero when this instance is a number and System.Double is not a number!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
