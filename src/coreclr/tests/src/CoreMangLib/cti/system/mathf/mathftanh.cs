// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.MathF.Tanh(bouble)
/// </summary>
public class MathFTanh
{
    public static int Main()
    {
        MathFTanh mathTanh = new MathFTanh();
        TestLibrary.TestFramework.BeginTestCase("MathFTanh");
        if (mathTanh.RunTests())
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
        return retVal;
    }

    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Caculate the tanh of 0 degrees");
        try
        {
            float sourceA = 0;
            float desA = MathF.Tanh(sourceA);
            if (!MathFTestLib.SingleIsWithinEpsilon(desA, sourceA))
            {
                TestLibrary.TestFramework.LogError("001", "Expected: 0, actual: " + desA.ToString() + "; diff > epsilon = " +
                    MathFTestLib.Epsilon.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Calculate the tanh of 90 degrees");
        try
        {
            float sourceA = 90;
            float desA = MathF.Tanh(sourceA * (MathF.PI / 180));
            if (!MathFTestLib.SingleIsWithinEpsilon(desA, 0.917152336f))
            {
                TestLibrary.TestFramework.LogError("003", "Expected: 0.917152336, actual: " + desA.ToString() + "; diff > epsilon = " +
                    MathFTestLib.Epsilon.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Calculate the tanh of 180 degrees");
        try
        {
            float sourceA = 180;
            float desA = MathF.Tan(sourceA * (MathF.PI / 180));
            if (!MathFTestLib.SingleIsWithinEpsilon(desA, -0.0f))
            {
                TestLibrary.TestFramework.LogError("005", "Expected: -0.0, actual: " + desA.ToString() + "; diff > epsilon = " +
                    MathFTestLib.Epsilon.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: Calculate the tanh of 45 degrees");
        try
        {
            float sourceA = 45.0f;
            float desA = MathF.Tanh(sourceA * (MathF.PI / 180));
            if (!MathFTestLib.SingleIsWithinEpsilon(desA, 0.655794203f))
            {
                TestLibrary.TestFramework.LogError("007", "Expected: 0.655794203, actual: " + desA.ToString() + "; diff > epsilon = " +
                    MathFTestLib.Epsilon.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5: tanh(NaN)=NaN");
        try
        {
            float sourceA = float.NaN;
            float desA = MathF.Tanh(sourceA);
            if (!float.IsNaN(desA))
            {
                TestLibrary.TestFramework.LogError("009", "Expected: NaN, actual: " + desA.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest6: tanh(-inf)=-1");
        try
        {
            float sourceA = float.NegativeInfinity;
            float desA = MathF.Tanh(sourceA);
            if (!MathFTestLib.SingleIsWithinEpsilon(desA, -1))
            {
                TestLibrary.TestFramework.LogError("011", "Expected: -1, actual: " + desA.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest7: tanh(+inf) = 1");
        try
        {
            float sourceA = float.PositiveInfinity;
            float desA = MathF.Tanh(sourceA);
            if (!MathFTestLib.SingleIsWithinEpsilon(desA, 1))
            {
                TestLibrary.TestFramework.LogError("013", "Expected 1, actual: " + desA.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
