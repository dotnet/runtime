// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// Sign(System.Double)
/// </summary>

public class MathSign2
{
    public static int Main(string[] args)
    {
        MathSign2 test = new MathSign2();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Sign(System.Double).");

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
        retVal = PosTest8() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negtive]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify Double.MaxValue is a positive number.");

        try
        {
            Double d = Double.MaxValue;
            if (Math.Sign(d) != 1)
            {
                TestLibrary.TestFramework.LogError("P01.1", "Double.MaxValue is not a positive number!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P01.2", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify Double.MinValue is a negative number.");

        try
        {
            Double d = Double.MinValue;
            if (Math.Sign(d) != -1)
            {
                TestLibrary.TestFramework.LogError("P02.1", "Double.MinValue is not a negative number!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P02.2", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify Double.PositiveInfinity is a positive number.");

        try
        {
            Double d = Double.PositiveInfinity;
            if (Math.Sign(d) != 1)
            {
                TestLibrary.TestFramework.LogError("P03.1", "Double.PositiveInfinity is not a positive number!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P03.2", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify Double.NegativeInfinity is a negative number.");

        try
        {
            Double d = Double.NegativeInfinity;
            if (Math.Sign(d) != -1)
            {
                TestLibrary.TestFramework.LogError("P04.1", "Double.NegativeInfinity is not a negative number!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P04.2", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify Double.Epsilon is a positive number.");

        try
        {
            Double d = Double.Epsilon;
            if (Math.Sign(d) != 1)
            {
                TestLibrary.TestFramework.LogError("P05.1", "Double.Epsilon is not a positive number!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P05.2", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest6: Verify the return value should be 1 when the Double is positive.");

        try
        {
            Double d = TestLibrary.Generator.GetDouble(-55);
            while (d <= 0)
            {
                d = TestLibrary.Generator.GetDouble(-55);
            }
            
            if (Math.Sign(d) != 1)
            {
                TestLibrary.TestFramework.LogError("P06.1", "The return value is not 1 when the Double is positive!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P06.2", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest7: Verify the return value should be -1 when the Double is negative.");

        try
        {
            Double d = TestLibrary.Generator.GetDouble(-55);
            while (d <= 0)
            {
                d = TestLibrary.Generator.GetDouble(-55);
            }

            if (Math.Sign(-d) != -1)
            {
                TestLibrary.TestFramework.LogError("P07.1", "The return value is not -1 when the Double is negative!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P07.2", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest8: Verify the return value should be 0 when the Double is zero.");

        try
        {
            Double d = 0.0d;

            if (Math.Sign(d) != 0)
            {
                TestLibrary.TestFramework.LogError("P08.1", "The return value is not -1 when the Double is negative!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P08.2", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: ArithmeticException should be thrown when value is equal to NaN.");

        try
        {
            Double d = Double.NaN;
            Math.Sign(d);

            TestLibrary.TestFramework.LogError("N01.1", "ArithmeticException is not thrown when value is equal to NaN!");
            retVal = false;

        }
        catch (ArithmeticException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N01.2", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
