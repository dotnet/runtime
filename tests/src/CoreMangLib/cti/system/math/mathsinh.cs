// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// Sinh(System.Double)
/// </summary>

public class MathSinh
{
    public static int Main(string[] args)
    {
        MathSinh test = new MathSinh();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Sinh(System.Double).");

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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the result when radian is 0.");

        try
        {
            Double d = Math.Sinh(0);
            if (!MathTestLib.DoubleIsWithinEpsilon(d, 0))
            {
                TestLibrary.TestFramework.LogError("P01.1", "The result is error when radian is 0!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the result when radian is Math.PI/2.");

        try
        {
            Double d = Math.Sinh(Math.PI / 2);
            if (!MathTestLib.DoubleIsWithinEpsilon(d ,2.3012989023072947))
            {
                TestLibrary.TestFramework.LogError("P02.1", "The result is error when radian is Math.PI/2!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify the result when radian is -Math.PI/2.");

        try
        {
            Double d = Math.Sinh(-Math.PI / 2);
            if (!MathTestLib.DoubleIsWithinEpsilon(d ,-2.3012989023072947))
            {
                TestLibrary.TestFramework.LogError("P03.1", "The result is error when radian is -Math.PI/2!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify the result when radian is Math.PI/6.");

        try
        {
            Double d = Math.Sinh(Math.PI / 6);
            if (!MathTestLib.DoubleIsWithinEpsilon(d,0.54785347388803973))
            {
                TestLibrary.TestFramework.LogError("P04.1", "The result is error when radian is Math.PI/6!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify the result when radian is -Math.PI/6.");

        try
        {
            Double d = Math.Sinh(-Math.PI / 6);
            if (!MathTestLib.DoubleIsWithinEpsilon(d ,-0.54785347388803973))
            {
                TestLibrary.TestFramework.LogError("P05.1", "The result is error when radian is -Math.PI/6!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest6: Verify the result is PositiveInfinity when radian is PositiveInfinity.");

        try
        {
            Double d = Math.Sinh(Double.PositiveInfinity);
            if (d.CompareTo(Double.PositiveInfinity) != 0)
            {
                TestLibrary.TestFramework.LogError("P06.1", "The result is error when radian is PositiveInfinity!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest7: Verify the result is NegativeInfinity when radian is NegativeInfinity.");

        try
        {
            Double d = Math.Sinh(Double.NegativeInfinity);
            if (d.CompareTo(Double.NegativeInfinity) != 0)
            {
                TestLibrary.TestFramework.LogError("P07.1", "The result is error when radian is NegativeInfinity!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest8: Verify the result is NaN when radian is NaN.");

        try
        {
            Double d = Math.Sinh(Double.NaN);
            if (d.CompareTo(Double.NaN) != 0)
            {
                TestLibrary.TestFramework.LogError("P08.1", "The result is error when radian is NaN!");
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
}
