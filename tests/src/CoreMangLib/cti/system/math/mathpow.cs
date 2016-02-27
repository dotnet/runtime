// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Pow(System.Double, System.Double)
/// </summary>

public class MathPow
{
    #region Public Methods
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
        retVal = PosTest9() && retVal;
        retVal = PosTest10() && retVal;
        retVal = PosTest11() && retVal;
        retVal = PosTest12() && retVal;
        retVal = PosTest13() && retVal;
        retVal = PosTest14() && retVal;
        retVal = PosTest15() && retVal;
        retVal = PosTest16() && retVal;

        
        //
        // TODO: Add your negative test cases here
        //
        // TestLibrary.TestFramework.LogInformation("[Negative]");
        // retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify Pow when one of args is NaN .");

        try
        {
            double d = Math.Pow(double.NaN, TestLibrary.Generator.GetByte(-55));

            if (!double.IsNaN(d))
            {
                TestLibrary.TestFramework.LogError("001.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify Pow when second arg is zero .");

        try
        {
            double d = Math.Pow(TestLibrary.Generator.GetByte(-55),0);

            if (d != 1)
            {
                TestLibrary.TestFramework.LogError("002.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify Pow(NegativeInfinity, < 0) .");

        try
        {
            double d = Math.Pow(double.NegativeInfinity,-TestLibrary.Generator.GetDouble(-55));

            if (d != 0)
            {
                TestLibrary.TestFramework.LogError("003.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify Pow(NegativeInfinity, positive odd int) .");

        try
        {
            double d = Math.Pow(double.NegativeInfinity, TestLibrary.Generator.GetByte(-55) * 2 + 1);

            if (d != double.NegativeInfinity)
            {
                TestLibrary.TestFramework.LogError("004.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify Pow(NegativeInfinity, positive non-odd int) .");

        try
        {
            int e = (TestLibrary.Generator.GetByte(-55) +1)  * 2;
            double d = Math.Pow(double.NegativeInfinity, e);

            if (d != double.PositiveInfinity)
            {
                TestLibrary.TestFramework.LogError("005.1", "Return value is wrong: expected Double.PositiveInfinity, actual: "+
                    d.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest6: Verify Pow(< 0, non-int) .");

        try
        {
            double d = Math.Pow(-TestLibrary.Generator.GetByte(-55) - 1, TestLibrary.Generator.GetDouble() + 0.1);

            if (!double.IsNaN(d))
            {
                TestLibrary.TestFramework.LogError("006.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest7: Verify Pow(-1, NegativeInfinity) .");

        try
        {
            double d = Math.Pow(- 1, double.NegativeInfinity);

            if (!double.IsNaN(d))
            {
                TestLibrary.TestFramework.LogError("007.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest8: Verify Pow(-1<x<1, NegativeInfinity) .");

        try
        {
            double d = Math.Pow(TestLibrary.Generator.GetDouble(-55), double.NegativeInfinity);

            if (!double.IsPositiveInfinity(d))
            {
                TestLibrary.TestFramework.LogError("008.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest9()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest9: Verify Pow(-1<x<1, PositiveInfinity) .");

        try
        {
            double d = Math.Pow(TestLibrary.Generator.GetDouble(-55), double.PositiveInfinity);

            if (d != 0)
            {
                TestLibrary.TestFramework.LogError("009.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest10()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest10: Verify Pow(>1, NegativeInfinity) .");

        try
        {
            double d = Math.Pow(TestLibrary.Generator.GetDouble(-55) + 1, double.NegativeInfinity);

            if (d != 0)
            {
                TestLibrary.TestFramework.LogError("010.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest11()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest11: Verify Pow(>1, PositiveInfinity) .");

        try
        {
            double d = Math.Pow(TestLibrary.Generator.GetDouble(-55) + 1, double.PositiveInfinity);

            if (!double.IsPositiveInfinity(d))
            {
                TestLibrary.TestFramework.LogError("011.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("011.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest12()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest12: Verify Pow( 0, < 0) .");

        try
        {
            double d = Math.Pow(0, -TestLibrary.Generator.GetDouble(-55) - 0.1);

            if (!double.IsPositiveInfinity(d))
            {
                TestLibrary.TestFramework.LogError("012.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest13()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest13: Verify Pow( 0, > 0) .");

        try
        {
            double d = Math.Pow(0, TestLibrary.Generator.GetDouble(-55) + 0.1);

            if (d != 0)
            {
                TestLibrary.TestFramework.LogError("013.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("013.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest14()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest14: Verify Pow( 1, any value) .");

        try
        {
            double d = Math.Pow(1, TestLibrary.Generator.GetDouble(-55));

            if (d != 1)
            {
                TestLibrary.TestFramework.LogError("014.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest15()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest15: Verify Pow( PositiveInfinity, < 0) .");

        try
        {
            double d = Math.Pow(double.PositiveInfinity, -TestLibrary.Generator.GetDouble(-55) - 0.1);

            if (d != 0)
            {
                TestLibrary.TestFramework.LogError("015.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("015.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest16()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest16: Verify Pow( PositiveInfinity, > 0) .");

        try
        {
            double d = Math.Pow(double.PositiveInfinity, TestLibrary.Generator.GetDouble(-55) + 0.1);

            if (!double.IsPositiveInfinity(d))
            {
                TestLibrary.TestFramework.LogError("015.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("015.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    //public bool NegTest1()
    //{
    //    bool retVal = true;

    //    TestLibrary.TestFramework.BeginScenario("NegTest1: ");

    //    try
    //    {
    //          //
    //          // Add your test logic here
    //          //
    //    }
    //    catch (Exception e)
    //    {
    //        TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
    //        TestLibrary.TestFramework.LogInformation(e.StackTrace);
    //        retVal = false;
    //    }

    //    return retVal;
    //}
    #endregion
    #endregion

    public static int Main()
    {
        MathPow test = new MathPow();

        TestLibrary.TestFramework.BeginTestCase("MathPow");

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
}
