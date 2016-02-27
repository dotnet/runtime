// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// System.Math.Ceiling(System.Double)
/// </summary>
public class MathCeiling
{
    public static int Main(string[] args)
    {
        MathCeiling ceiling = new MathCeiling();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Ceiling(System.Double)...");

        if (ceiling.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify Ceiling number should be equal to the integer part of negative number...");

        try
        {
            double number = TestLibrary.Generator.GetDouble(-55);
            while (number >= 0)
            {
                number = (-TestLibrary.Generator.GetDouble(-55))*100;
            }

            double ceilingNumber = Math.Ceiling(number);
            if (ceilingNumber < number || ceilingNumber > number + 1)
            {
                TestLibrary.TestFramework.LogError("001","The Ceiling number should be equal to the integer part of negative number!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify Ceiling number should be equal to itself when number is negative integer");

        try
        {
            double number = TestLibrary.Generator.GetDouble(-55);
            while (number >= 0)
            {
                number = (-TestLibrary.Generator.GetDouble(-55)) * 100;
            }

            double ceilingNumber = Math.Ceiling(number);
            if (ceilingNumber != Math.Ceiling(ceilingNumber))
            {
                TestLibrary.TestFramework.LogError("003","The Ceiling number should be equal to itself when number is negative integer...");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify Ceiling number should be equal to the integer part plus one of positive number...");

        try
        {
            double number = TestLibrary.Generator.GetDouble(-55);
            while (number <= 0)
            {
                number = (TestLibrary.Generator.GetDouble(-55)) * 100;
            }

            double ceilingNumber = Math.Ceiling(number);
            if (ceilingNumber < number || ceilingNumber > number + 1)
            {
                TestLibrary.TestFramework.LogError("005", "The Ceiling number should be equal to the integer part plus one of positive number!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify Ceiling number should be equal to itself when number is positive integer");

        try
        {
            double number = TestLibrary.Generator.GetDouble(-55);
            while (number <= 0)
            {
                number = (TestLibrary.Generator.GetDouble(-55)) * 100;
            }

            double ceilingNumber = Math.Ceiling(number);
            if (ceilingNumber != Math.Ceiling(ceilingNumber))
            {
                TestLibrary.TestFramework.LogError("007", "The Ceiling number should be equal to itself when number is positive integer...");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify Ceiling number should be equal to itself when number is maxvalue...");

        try
        {
            double ceilingMax = Math.Ceiling(double.MaxValue);
            if (ceilingMax != double.MaxValue)
            {
                TestLibrary.TestFramework.LogError("009", "The Ceiling number should be equal to itself when number is maxvalue!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest6: Verify Ceiling number should be equal to itself when number is minvalue...");

        try
        {
            double ceilingMax = Math.Ceiling(double.MinValue);
            if (ceilingMax != double.MinValue)
            {
                TestLibrary.TestFramework.LogError("011", "The Ceiling number should be equal to itself when number is minvalue!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
