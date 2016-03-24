// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;


/// <summary>
/// System.Math.Acos(System.Double)
/// </summary>
public class MathAcos
{
   

    public static int Main(string[] args)
    {
        MathAcos Acos = new MathAcos();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Acos(System.Double)...");

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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the value of Arccos(-1) is Math.PI...");

        try
        {
            double angle = Math.Acos(-1);
            if (!MathTestLib.DoubleIsWithinEpsilon(angle ,Math.PI))
            {
                TestLibrary.TestFramework.LogError("001","Expected Math.Acos(-1) = " + Math.PI.ToString() +", received "+
			angle.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the value of Arccos(0) is Math.PI/2...");

        try
        {
            double angle = Math.Acos(0);
            if (!MathTestLib.DoubleIsWithinEpsilon(angle, Math.PI/2))
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
            double angle = Math.Acos(1);
            if (!MathTestLib.DoubleIsWithinEpsilon(angle ,0))
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify the value of Arccos(0.5) is Math.PI/3...");

        try
        {
            double angle = Math.Acos(0.5);
            if (!MathTestLib.DoubleIsWithinEpsilon(angle, 1.0471975511965979))
            {
                TestLibrary.TestFramework.LogError("007","Expected 1.0471975511965979, got "+angle.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify the value of Arccos(-0.5) is 2*Math.PI/3...");

        try
        {
            double angle = Math.Acos(-0.5);
            if (!MathTestLib.DoubleIsWithinEpsilon(angle, 2.0943951023931957))
            {
                TestLibrary.TestFramework.LogError("009", "Expected: 2.09439510239319573, actual: " + angle.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest6: Verify the value of Arccos(Math.Sqrt(3)/2) is Math.PI/6...");

        try
        {
            double angle = Math.Acos(Math.Sqrt(3)/2);
            if (!MathTestLib.DoubleIsWithinEpsilon(angle, 0.52359877559829893))
            {
                TestLibrary.TestFramework.LogError("011", "Expected: 0.52359877559829893, actual: " + angle.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest7: Verify the value of Arccos(-Math.Sqrt(3)/2) is 5*Math.PI/6...");

        try
        {
            double angle = Math.Acos(-Math.Sqrt(3) / 2);
            if (!MathTestLib.DoubleIsWithinEpsilon(angle, 2.6179938779914944))
            {
                TestLibrary.TestFramework.LogError("013", "Expected: 2.617993877991494, actual: " + angle.ToString());
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
