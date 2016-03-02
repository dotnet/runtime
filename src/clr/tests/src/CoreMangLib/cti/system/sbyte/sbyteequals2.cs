// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// SByte.Equals(SByte)
/// </summary>
public class SByteEquals2
{
    public static int Main()
    {
        SByteEquals2 sbyteEquals2 = new SByteEquals2();
        TestLibrary.TestFramework.BeginTestCase("SByteEquals2");
        if (sbyteEquals2.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: The sbyte equals SByte maxValue");
        try
        {
            sbyte desSByte = SByte.MaxValue;
            sbyte sourceSByte = (sbyte)(this.GetInt32(0,127) + this.GetInt32(0,128)*(-1));
            bool retbool = sourceSByte.Equals(desSByte);
            if (retbool)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: The sbyte equals SByte minValue");
        try
        {
            sbyte desSByte = SByte.MinValue;
            sbyte sourceSByte = (sbyte)(this.GetInt32(0, 127) + this.GetInt32(0, 128) * (-1));
            bool retbool = sourceSByte.Equals(desSByte);
            if (retbool)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: The sbyte maxValue equals SByte minValue");
        try
        {
            sbyte desSByte = SByte.MinValue;
            sbyte sourceSByte = SByte.MaxValue;
            bool retbool = sourceSByte.Equals(desSByte);
            if (retbool)
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: The sbyte maxValue equals 127");
        try
        {
            sbyte desSByte = 127;
            sbyte sourceSByte = SByte.MaxValue;
            bool retbool = sourceSByte.Equals(desSByte);
            if (!retbool)
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: The sbyte minValue equals -128");
        try
        {
            sbyte desSByte = -128;
            sbyte sourceSByte = SByte.MinValue;
            bool retbool = sourceSByte.Equals(desSByte);
            if (!retbool)
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
