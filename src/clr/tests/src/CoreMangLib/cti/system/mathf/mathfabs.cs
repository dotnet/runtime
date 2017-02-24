// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.MathF.Abs(System.Single)
/// </summary>
public class MathFAbs
{
    public static int Main(string[] args)
    {
        MathFAbs abs = new MathFAbs();
        TestLibrary.TestFramework.BeginTestCase("Testing System.MathF.Abs(System.Single)...");

        if (abs.RunTests())
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


        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the max value of float should be equal to its Abs value");

        try
        {
            float max = float.MaxValue;
            if (MathF.Abs(max) != max)
            {
                TestLibrary.TestFramework.LogError("001", "The Abs of max value should be equal to itself!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the min value of float should be equal to its Abs value");

        try
        {
            float min = float.MinValue;
            if (MathF.Abs(min) != -min)
            {
                TestLibrary.TestFramework.LogError("003", "The Abs of max value should be equal to it's contrary!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify the NegativeInfinity of float should be equal to its contrary...");

        try
        {
            float negInfinit = float.NegativeInfinity;
            if (MathF.Abs(negInfinit) != -negInfinit)
            {
                TestLibrary.TestFramework.LogError("005", "The Abs of max value should be equal to it's contrary!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify the PositiveInfinity of float should be equal to its Abs value");

        try
        {
            float posInfinit = float.PositiveInfinity;
            if (MathF.Abs(posInfinit) != posInfinit)
            {
                TestLibrary.TestFramework.LogError("007", "The Abs of max value should be equal to itself!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify Abs value of zero should be equal to both itself and its contrary value...");

        try
        {
            float zero = 0;
            if (MathF.Abs(zero) != zero || MathF.Abs(zero) != -zero)
            {
                TestLibrary.TestFramework.LogError("009", "Abs value of zero should be equal to both itself and its contrary value!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest6: Verify Abs value of Epsilon ");

        try
        {
            float epsilon = float.Epsilon;
            if (MathF.Abs(epsilon) != epsilon)
            {
                TestLibrary.TestFramework.LogError("011", "Abs value of epsilon should be equal to itself...");
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
}
