// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Math.Abs(System.Double)
/// </summary>
public class MathAbs2
{
    public static int Main(string[] args)
    {
        MathAbs2 abs2 = new MathAbs2();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Abs(System.Double)...");

        if (abs2.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the max value of Double should be equal to its Abs value...");

        try
        {
            Double doubleParam = Double.MaxValue;
            if (Math.Abs(doubleParam) != doubleParam)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the Abs of min value of Double should be equal to it's contrary value...");

        try
        {
            Double doubleParam = Double.MinValue;
            if (Math.Abs(doubleParam) != -doubleParam)
            {
                TestLibrary.TestFramework.LogError("003", "The Abs of min value should be equal to it's contrary value!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify the Abs of NegativeInfinity should be equal to it's contrary value...");

        try
        {
            Double doubleParam = Double.NegativeInfinity;
            if (Math.Abs(doubleParam) != -doubleParam)
            {
                TestLibrary.TestFramework.LogError("005", "The Abs of NegativeInfinity should be equal to it's contrary value!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify the Abs of PositiveInfinity should be equal to itself...");

        try
        {
            Double doubleParam = Double.PositiveInfinity;
            if (Math.Abs(doubleParam) != doubleParam)
            {
                TestLibrary.TestFramework.LogError("007", "The Abs of PositiveInfinity should be equal to itself!");
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
            Double zero = 0;
            if (Math.Abs(zero) != zero || Math.Abs(zero) != -zero)
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
            Double epsilon = Double.Epsilon;
            if (Math.Abs(epsilon) != epsilon)
            {
                TestLibrary.TestFramework.LogError("011","Abs value of epsilon should be equal to itself...");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
