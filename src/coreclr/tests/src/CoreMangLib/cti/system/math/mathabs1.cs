// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Math.Abs(System.Decimal)
/// </summary>
public class MathAbs1
{
    public static int Main(string[] args)
    {
        MathAbs1 abs1 = new MathAbs1();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Abs(System.Decimal)...");

        if (abs1.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the max value of Decimal is equal to its Abs value...");

        try
        {
            Decimal dec = Decimal.MaxValue;
            if (Math.Abs(dec) != dec)
            {
                TestLibrary.TestFramework.LogError("001","The Abs of max value should be equal to itself!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the Abs of min value should be equal to it's contrary value...");

        try
        {
            Decimal dec = Decimal.MinValue;
            if (Math.Abs(dec) != -dec)
            {
                TestLibrary.TestFramework.LogError("003","The Abs of min value should be equal to it's contrary value!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify the Abs of MinusOne should be equal to it's contrary value...");

        try
        {
            Decimal dec = Decimal.MinusOne;
            if (Math.Abs(dec) != -dec)
            {
                TestLibrary.TestFramework.LogError("005", "The Abs of MinusOne should be equal to it's contrary value!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify Abs value of zero should be equal to both itself and its contrary value...");

        try
        {
            Decimal zero = 0;
            if (Math.Abs(zero) != zero || Math.Abs(zero) != -zero)
            {
                TestLibrary.TestFramework.LogError("007", "Abs value of zero should be equal to both itself and its contrary value!");
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
}
