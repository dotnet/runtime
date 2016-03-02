// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// SByte.GetHashCode()
/// </summary>
public class SByteGetHashCode
{
    private const int midVal1 = (int)sbyte.MaxValue - (int)(sbyte.MinValue) + 2;
    private const int midVal2 = (int)sbyte.MaxValue - (int)(sbyte.MinValue);
    public static int Main()
    {
        SByteGetHashCode sbytehash = new SByteGetHashCode();
        TestLibrary.TestFramework.BeginTestCase("SByteGetHashCode");
        if (sbytehash.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Return the SByte MaxValue hashcode");
        try
        {
            sbyte sourceVal = sbyte.MaxValue;
            int retHashCode = sourceVal.GetHashCode();
            if (retHashCode != midVal1*(int)(sbyte.MaxValue))
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Return the SByte MinValue hashcode");
        try
        {
            sbyte sourceVal = sbyte.MinValue;
            int retHashCode = sourceVal.GetHashCode();
            if (retHashCode != midVal2 * (int)sbyte.MinValue * (-1))
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Return the 0 hashcode");
        try
        {
            sbyte sourceVal = 0;
            int retHashCode = sourceVal.GetHashCode();
            if (retHashCode != 0)
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Return the random sbyte hashcode 1");
        try
        {
            sbyte sourceVal = (sbyte)(this.GetInt32(1, 128));
            int retHashCode = sourceVal.GetHashCode();
            if (retHashCode != midVal1 * (int)(sourceVal))
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: Return the random sbyte hashcode 2");
        try
        {
            sbyte sourceVal = (sbyte)(this.GetInt32(1, 129) * (-1));
            int retHashCode = sourceVal.GetHashCode();
            if (retHashCode != midVal2 * (int)(sourceVal)*(-1))
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
    #endregion
    #region HelpMethod
    private Int32 GetInt32(Int32 minValue, Int32 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + TestLibrary.Generator.GetInt32(-55) % (maxValue - minValue);
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }
    #endregion
}
