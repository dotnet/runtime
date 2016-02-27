// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Math.Cos(System.Double)
/// </summary>
public class MathCos
{
    public static int Main(string[] args)
    {
        MathCos cos = new MathCos();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Cos(System.Double)...");

        if (cos.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the value of cosine(0)...");

        try
        {
            double value = Math.Cos(0);
            if (value != 1)
            {
                TestLibrary.TestFramework.LogError("001", "cosine(0) should be 1!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the value of cosine(Math.PI/2)...");

        try
        {
            double value = Math.Cos(Math.PI/2);
            if (Math.Abs(1000000000*value) >= 0.1)
            {
                TestLibrary.TestFramework.LogError("003", "cosine(Math.PI/2) should be 0!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify the value of cosine(Math.PI)...");

        try
        {
            double value = Math.Cos(Math.PI);
            if (value != -1)
            {
                TestLibrary.TestFramework.LogError("005", "cosine(Math.PI) should be -1!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify the value of cosine(3*Math.PI/2)...");

        try
        {
            double value = Math.Cos(3*Math.PI/2);
            if (Math.Abs(1000000000 * value) >= 0.1)
            {
                TestLibrary.TestFramework.LogError("007", "cosine(3*Math.PI/2) should be 0!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify the value of cosine(2*Math.PI) is equal to cosine(0)...");

        try
        {
            double value1 = Math.Cos(0);
            double value2 = Math.Cos(2*Math.PI);
            if (value1 != value2)
            {
                TestLibrary.TestFramework.LogError("009", "cosine(3*Math.PI/2) should be 0!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest6: Verify the value of cosine(number) is between -1 and 1...");

        try
        {
            double coefficient = TestLibrary.Generator.GetDouble(-55);
            if (Math.Cos(2 * coefficient * Math.PI) > 1 || Math.Cos(2 * coefficient * Math.PI) < -1)
            {
                TestLibrary.TestFramework.LogError("011","The scale of cosine is wrong!");
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
