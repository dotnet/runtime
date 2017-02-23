// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.MathF.IEEERemainder(System.Single, System.Single)
/// </summary>
public class MathFIEEERemainder
{
    public static int Main(string[] args)
    {
        MathFIEEERemainder ieeeRemainder = new MathFIEEERemainder();
        TestLibrary.TestFramework.BeginTestCase("Testing System.MathF.IEEERemainder(System.Single,System.Single)...");

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
            float x = 5;
            float y = 2;
            float remainder = MathF.IEEERemainder(x, y);

            // When x=5,y=2,the quotient should be equal to the even nember 2, therefore the IEEERemainder is positive
            if (remainder != x - MathF.Floor(x / y) * y)
            {
                TestLibrary.TestFramework.LogError("001", "The remainder should be x - (y*quotient)!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify x/y fails halfway between two integer,the add integer is less than even integer...");

        try
        {
            float x = 7;
            float y = 2;
            float remainder = MathF.IEEERemainder(x, y);

            //When x=7,y=2,the quotient should be equal to the even number 4,therefore the IEEERemainder is negative
            if (remainder != x - MathF.Ceiling(x / y) * y)
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
            float x = TestLibrary.Generator.GetSingle(-55);
            float y = 0;
            float remainder = MathF.IEEERemainder(x, y);

            if (!float.IsNaN(remainder))
            {
                TestLibrary.TestFramework.LogError("005", "The value should be float.NaN!");
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
            float x = 6;
            float y = 2;
            float remainder = MathF.IEEERemainder(x, y);

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
