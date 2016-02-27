// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// Sign(System.Single)
/// </summary>

public class MathSign7
{
    public static int Main(string[] args)
    {
        MathSign7 test = new MathSign7();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Sign(System.Single).");

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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify Single.MaxValue is a positive number.");

        try
        {
            Single s = Single.MaxValue;
            if (Math.Sign(s) != 1)
            {
                TestLibrary.TestFramework.LogError("P01.1", "Single.MaxValue is not a positive number!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify Single.MinValue is a negative number.");

        try
        {
            Single s = Single.MinValue;
            if (Math.Sign(s) != -1)
            {
                TestLibrary.TestFramework.LogError("P02.1", "Single.MinValue is not a negative number!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify Single.PositiveInfinity is a positive number.");

        try
        {
            Single s = Single.PositiveInfinity;
            if (Math.Sign(s) != 1)
            {
                TestLibrary.TestFramework.LogError("P03.1", "Single.PositiveInfinity is not a positive number!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify Single.NegativeInfinity is a negative number.");

        try
        {
            Single s = Single.NegativeInfinity;
            if (Math.Sign(s) != -1)
            {
                TestLibrary.TestFramework.LogError("P04.1", "Single.NegativeInfinity is not a negative number!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify Single.Epsilon is a positive number.");

        try
        {
            Single s = Single.Epsilon;
            if (Math.Sign(s) != 1)
            {
                TestLibrary.TestFramework.LogError("P05.1", "Single.Epsilon is not a positive number!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest6: Verify the return value should be 1 when the Single is positive.");

        try
        {
            Single s = TestLibrary.Generator.GetSingle(-55);
            while (s <= 0)
            {
                s = TestLibrary.Generator.GetSingle(-55);
            }

            if (Math.Sign(s) != 1)
            {
                TestLibrary.TestFramework.LogError("P06.1", "The return value is not 1 when the Single is positive!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest7: Verify the return value should be -1 when the Single is negative.");

        try
        {
            Single s = TestLibrary.Generator.GetSingle(-55);
            while (s <= 0)
            {
                s = TestLibrary.Generator.GetSingle(-55);
            }

            if (Math.Sign(-s) != -1)
            {
                TestLibrary.TestFramework.LogError("P07.1", "The return value is not -1 when the Single is negative!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest8: Verify the return value should be 0 when the Single is zero.");

        try
        {
            Single s = 0.0f;

            if (Math.Sign(s) != 0)
            {
                TestLibrary.TestFramework.LogError("P08.1", "The return value is not -1 when the Single is negative!");
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
            Single s = Single.NaN;
            Math.Sign(s);

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
