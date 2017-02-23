// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.MathF.Cos(System.Single)
/// </summary>
public class MathFCos
{
    public static int Main(string[] args)
    {
        MathFCos cos = new MathFCos();
        TestLibrary.TestFramework.BeginTestCase("Testing System.MathF.Cos(System.Single)...");

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
            float value = MathF.Cos(0);
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the value of cosine(MathF.PI/2)...");

        try
        {
            float value = MathF.Cos(MathF.PI / 2);
            if (MathF.Abs(1000000 * value) >= 0.1)
            {
                TestLibrary.TestFramework.LogError("003", "cosine(MathF.PI/2) should be 0!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify the value of cosine(MathF.PI)...");

        try
        {
            float value = MathF.Cos(MathF.PI);
            if (value != -1)
            {
                TestLibrary.TestFramework.LogError("005", "cosine(MathF.PI) should be -1!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify the value of cosine(3*MathF.PI/2)...");

        try
        {
            float value = MathF.Cos(3 * MathF.PI / 2);
            if (MathF.Abs(1000000 * value) >= 0.1)
            {
                TestLibrary.TestFramework.LogError("007", "cosine(3*MathF.PI/2) should be 0!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify the value of cosine(2*MathF.PI) is equal to cosine(0)...");

        try
        {
            float value1 = MathF.Cos(0);
            float value2 = MathF.Cos(2 * MathF.PI);
            if (value1 != value2)
            {
                TestLibrary.TestFramework.LogError("009", "cosine(3*MathF.PI/2) should be 0!");
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
            float coefficient = TestLibrary.Generator.GetSingle(-55);
            if (MathF.Cos(2 * coefficient * MathF.PI) > 1 || MathF.Cos(2 * coefficient * MathF.PI) < -1)
            {
                TestLibrary.TestFramework.LogError("011", "The scale of cosine is wrong!");
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
