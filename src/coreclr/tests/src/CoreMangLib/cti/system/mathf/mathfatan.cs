// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.MathF.Atan(System.Single)
/// </summary>
public class MathFAtan
{
    public static int Main(string[] args)
    {
        MathFAtan arctan = new MathFAtan();
        TestLibrary.TestFramework.BeginTestCase("Testing System.MathF.Atan(System.Single)...");

        if (arctan.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the value of arctan(NegativeInfinity) is -MathF.PI/2...");

        try
        {
            float angle = MathF.Atan(float.NegativeInfinity);
            if (!MathFTestLib.SingleIsWithinEpsilon(angle, -MathF.PI / 2))
            {
                TestLibrary.TestFramework.LogError("001", "Expected: -pi/2, actual: " +
                    angle.ToString() + " diff>epsilon= " + MathFTestLib.Epsilon.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the value of arctan(PositiveInfinity) is MathF.PI/2...");

        try
        {
            float angle = MathF.Atan(float.PositiveInfinity);
            if (!MathFTestLib.SingleIsWithinEpsilon(angle, MathF.PI / 2))
            {
                TestLibrary.TestFramework.LogError("003", "Expected: pi/2, actual: " +
                    angle.ToString() + " diff>epsilon= " + MathFTestLib.Epsilon.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify the value of arctan(0) is zero...");

        try
        {
            float angle = MathF.Atan(0);
            if (!MathFTestLib.SingleIsWithinEpsilon(angle, 0))
            {
                TestLibrary.TestFramework.LogError("005", "Expected: 0, actual: " +
                    angle.ToString() + " diff>epsilon= " + MathFTestLib.Epsilon.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify the value of atn(inf+1)=atn(inf)");

        try
        {
            float angle = MathF.Atan(float.PositiveInfinity + 1);
            float baseline = MathF.Atan(float.PositiveInfinity);
            if (!MathFTestLib.SingleIsWithinEpsilon(angle, baseline))
            {
                TestLibrary.TestFramework.LogError("007", "Expected: " + baseline.ToString() + ", actual: " +
                    angle.ToString() + " diff>epsilon= " + MathFTestLib.Epsilon.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify atn(-inf -1)=atn(-inf)");

        try
        {
            float angle = MathF.Atan(float.NegativeInfinity - 1);
            float baseline = MathF.Atan(float.NegativeInfinity);
            if (!MathFTestLib.SingleIsWithinEpsilon(angle, baseline))
            {
                TestLibrary.TestFramework.LogError("009", "Expected " + baseline.ToString() + ", actual: " + angle.ToString());
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
}
