// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.MathF.Sqrt(float)
/// </summary>
public class MathFSqrt
{
    public static int Main()
    {
        MathFSqrt mathSqrt = new MathFSqrt();
        TestLibrary.TestFramework.BeginTestCase("MathFSqrt");
        if (mathSqrt.RunTests())
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

    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Calculate the zero sqrt");
        try
        {
            float source = 0;
            float des = MathF.Sqrt(source);
            if (des != 0)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2:Calculate the positive number sqrt");
        try
        {
            float source = 100;
            float des = MathF.Sqrt(source);
            if (des != 10)
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3:Calculate the negative number sqrt");
        try
        {
            float source = -1;
            float des = MathF.Sqrt(source);
            if (!float.IsNaN(des))
            {
                TestLibrary.TestFramework.LogError("005", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4:Calculate the float NaN sqrt");
        try
        {
            float source = float.NaN;
            float des = MathF.Sqrt(source);
            if (!float.IsNaN(des))
            {
                TestLibrary.TestFramework.LogError("007", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5:Calculate the float PositiveInfinity sqrt");
        try
        {
            float source = float.PositiveInfinity;
            float des = MathF.Sqrt(source);
            if (!float.IsPositiveInfinity(des))
            {
                TestLibrary.TestFramework.LogError("009", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest6:Calculate the float NegativeInfinity sqrt");
        try
        {
            float source = float.NegativeInfinity;
            float des = MathF.Sqrt(source);
            if (!float.IsNaN(des))
            {
                TestLibrary.TestFramework.LogError("011", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
