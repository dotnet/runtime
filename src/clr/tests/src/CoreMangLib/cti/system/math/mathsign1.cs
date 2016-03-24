// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// Sign(System.Decimal)
/// </summary>

public class MathSign1
{
    public static int Main(string[] args)
    {
        MathSign1 test = new MathSign1();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Sign(System.Decimal)...");

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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify Decimal.MaxValue is a positive number.");

        try
        {
            Decimal dec = Decimal.MaxValue;
            if (Math.Sign(dec) != 1)
            {
                TestLibrary.TestFramework.LogError("P01.1", "Decimal.MaxValue is not a positive number!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify Decimal.MinValue is a negative number.");

        try
        {
            Decimal dec = Decimal.MinValue;
            if (Math.Sign(dec) != -1)
            {
                TestLibrary.TestFramework.LogError("P02.1", "Decimal.MinValue is not a negative number!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify Decimal.One is a positive number.");

        try
        {
            Decimal dec = Decimal.One;
            if (Math.Sign(dec) != 1)
            {
                TestLibrary.TestFramework.LogError("P03.1", "Decimal.One is not a positive number!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify Decimal.MinusOne is a negative number.");

        try
        {
            Decimal dec = Decimal.MinusOne;
            if (Math.Sign(dec) != -1)
            {
                TestLibrary.TestFramework.LogError("P04.1", "Decimal.MinusOne is not a negative number!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify Decimal.Zero is zero.");

        try
        {
            Decimal zero = 0M;
            if (Math.Sign(zero) != 0)
            {
                TestLibrary.TestFramework.LogError("P05.1", "Decimal.Zero is not zero!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest6: Verify the return value should be 1 when the decimal is positive.");

        try
        {
            Decimal dec = 123.456M;
            if (Math.Sign(dec) != 1)
            {
                TestLibrary.TestFramework.LogError("P06.1", "The return value is not 1 when the decimal is positive!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest7: Verify the return value should be -1 when the decimal is negative.");

        try
        {
            Decimal dec = -123.456M;
            if (Math.Sign(dec) != -1)
            {
                TestLibrary.TestFramework.LogError("P07.1", "The return value is not -1 when the decimal is negative!");
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
}
