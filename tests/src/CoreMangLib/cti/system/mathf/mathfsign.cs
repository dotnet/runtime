// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.MathF.Sign(System.Single)
/// </summary>
public class MathFSign
{
    public static int Main(string[] args)
    {
        MathFSign test = new MathFSign();
        TestLibrary.TestFramework.BeginTestCase("Testing System.MathF.Sign(System.Single).");

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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify float.MaxValue is a positive number.");

        try
        {
            float s = float.MaxValue;
            if (MathF.Sign(s) != 1)
            {
                TestLibrary.TestFramework.LogError("P01.1", "float.MaxValue is not a positive number!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify float.MinValue is a negative number.");

        try
        {
            float s = float.MinValue;
            if (MathF.Sign(s) != -1)
            {
                TestLibrary.TestFramework.LogError("P02.1", "float.MinValue is not a negative number!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify float.PositiveInfinity is a positive number.");

        try
        {
            float s = float.PositiveInfinity;
            if (MathF.Sign(s) != 1)
            {
                TestLibrary.TestFramework.LogError("P03.1", "float.PositiveInfinity is not a positive number!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify float.NegativeInfinity is a negative number.");

        try
        {
            float s = float.NegativeInfinity;
            if (MathF.Sign(s) != -1)
            {
                TestLibrary.TestFramework.LogError("P04.1", "float.NegativeInfinity is not a negative number!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify float.Epsilon is a positive number.");

        try
        {
            float s = float.Epsilon;
            if (MathF.Sign(s) != 1)
            {
                TestLibrary.TestFramework.LogError("P05.1", "float.Epsilon is not a positive number!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest6: Verify the return value should be 1 when the float is positive.");

        try
        {
            float s = TestLibrary.Generator.GetSingle(-55);
            while (s <= 0)
            {
                s = TestLibrary.Generator.GetSingle(-55);
            }

            if (MathF.Sign(s) != 1)
            {
                TestLibrary.TestFramework.LogError("P06.1", "The return value is not 1 when the float is positive!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest7: Verify the return value should be -1 when the float is negative.");

        try
        {
            float s = TestLibrary.Generator.GetSingle(-55);
            while (s <= 0)
            {
                s = TestLibrary.Generator.GetSingle(-55);
            }

            if (MathF.Sign(-s) != -1)
            {
                TestLibrary.TestFramework.LogError("P07.1", "The return value is not -1 when the float is negative!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest8: Verify the return value should be 0 when the float is zero.");

        try
        {
            float s = 0.0f;

            if (MathF.Sign(s) != 0)
            {
                TestLibrary.TestFramework.LogError("P08.1", "The return value is not -1 when the float is negative!");
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
            float s = float.NaN;
            MathF.Sign(s);

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
