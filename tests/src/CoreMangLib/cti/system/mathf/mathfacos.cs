// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.MathF.Acos(System.Single)
/// </summary>
public class MathFAcos
{
    public static int Main(string[] args)
    {
        MathFAcos Acos = new MathFAcos();
        TestLibrary.TestFramework.BeginTestCase("Testing System.MathF.Acos(System.Single)...");

        if (Acos.RunTests())
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

        //TestLibrary.TestFramework.LogInformation("[Negtive]");
        //retVal = NegTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the value of Arccos(-1) is MathF.PI...");

        try
        {
            float angle = MathF.Acos(-1);
            if (!MathFTestLib.SingleIsWithinEpsilon(angle, MathF.PI))
            {
                TestLibrary.TestFramework.LogError("001", "Expected MathF.Acos(-1) = " + MathF.PI.ToString() + ", received " +
            angle.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the value of Arccos(0) is MathF.PI/2...");

        try
        {
            float angle = MathF.Acos(0);
            if (!MathFTestLib.SingleIsWithinEpsilon(angle, MathF.PI / 2))
            {
                TestLibrary.TestFramework.LogError("003", "Expected pi/2, got " + angle.ToString());
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

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify the value of Arccos(1) is 0...");

        try
        {
            float angle = MathF.Acos(1);
            if (!MathFTestLib.SingleIsWithinEpsilon(angle, 0))
            {
                TestLibrary.TestFramework.LogError("005", "Expected 1, got " + angle.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify the value of Arccos(0.5) is MathF.PI/3...");

        try
        {
            float angle = MathF.Acos(0.5f);
            if (!MathFTestLib.SingleIsWithinEpsilon(angle, 1.04719755f))
            {
                TestLibrary.TestFramework.LogError("007", "Expected 1.04719755, got " + angle.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify the value of Arccos(-0.5) is 2*MathF.PI/3...");

        try
        {
            float angle = MathF.Acos(-0.5f);
            if (!MathFTestLib.SingleIsWithinEpsilon(angle, 2.09439510f))
            {
                TestLibrary.TestFramework.LogError("009", "Expected: 2.09439510, actual: " + angle.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest6: Verify the value of Arccos(MathF.Sqrt(3)/2) is MathF.PI/6...");

        try
        {
            float angle = MathF.Acos(MathF.Sqrt(3) / 2);
            if (!MathFTestLib.SingleIsWithinEpsilon(angle, 0.523598776f))
            {
                TestLibrary.TestFramework.LogError("011", "Expected: 0.523598776, actual: " + angle.ToString());
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

    public bool PosTest7()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest7: Verify the value of Arccos(-MathF.Sqrt(3)/2) is 5*MathF.PI/6...");

        try
        {
            float angle = MathF.Acos(-MathF.Sqrt(3) / 2);
            if (!MathFTestLib.SingleIsWithinEpsilon(angle, 2.61799388f))
            {
                TestLibrary.TestFramework.LogError("013", "Expected: 2.61799388, actual: " + angle.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
