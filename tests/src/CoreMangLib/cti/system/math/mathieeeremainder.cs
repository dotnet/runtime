// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Math.IEEERemainder(System.Double,System.Double)
/// </summary>
public class MathIEEERemainder
{
    public static int Main(string[] args)
    {
        MathIEEERemainder ieeeRemainder = new MathIEEERemainder();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.IEEERemainder(System.Double,System.Double)...");

        if (ieeeRemainder.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify  x / y fails halfway between two integer,the even integer is less than odd integer...");

        try
        {
            Double x = 5;
            Double y = 2;
            Double remainder = Math.IEEERemainder(x,y);

            // When x=5,y=2,the quotient should be equal to the even nember 2, therefore the IEEERemainder is positive
            if (remainder != x - Math.Floor(x / y) * y) 
            {
                TestLibrary.TestFramework.LogError("001", "The remainder should be x - (y*quotient)!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify x/y fails halfway between two integer,the add integer is less than even integer...");

        try
        {
            Double x = 7;
            Double y = 2;
            Double remainder = Math.IEEERemainder(x,y);

            //When x=7,y=2,the quotient should be equal to the even number 4,therefore the IEEERemainder is negative
            if (remainder != x - Math.Ceiling(x / y) * y)
            {
                TestLibrary.TestFramework.LogError("003", "The remainder should be x - (y*quotient)!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify divisor is zero...");

        try
        {
            Double x = TestLibrary.Generator.GetDouble(-55);
            Double y = 0;
            Double remainder = Math.IEEERemainder(x,y);

            if (!Double.IsNaN(remainder))
            {
                TestLibrary.TestFramework.LogError("005","The value should be Double.NaN!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify the value 0 is returned when x - (y*quotient) is zero...");

        try
        {
            Double x = 6;
            Double y = 2;
            Double remainder = Math.IEEERemainder(x,y);

            if (remainder != 0)
            {
                TestLibrary.TestFramework.LogError("007", "The value should be zero!");
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
}
