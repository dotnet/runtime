// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// SByte.Parse(string)
/// </summary>
public class SByteParse1
{
    public static int Main()
    {
        SByteParse1 sbyteParse1 = new SByteParse1();
        TestLibrary.TestFramework.BeginTestCase("SByteParse1");
        if (sbyteParse1.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: the string represents SByte maxValue");
        try
        {
            string sourceStr = SByte.MaxValue.ToString();
            sbyte desSByte = sbyte.Parse(sourceStr);
            if (desSByte != SByte.MaxValue)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: the string represents SByte minValue");
        try
        {
            string sourceStr = SByte.MinValue.ToString();
            sbyte desSByte = sbyte.Parse(sourceStr);
            if (desSByte != SByte.MinValue)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: the string represents random SByte 1");
        try
        {
            SByte SByteVal = -012;
            string sourceStr = SByteVal.ToString();
            sbyte desSByte = sbyte.Parse(sourceStr);
            if (desSByte != -12)
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: the string represents random SByte 2");
        try
        {
            SByte SByteVal = +012;
            string sourceStr = SByteVal.ToString();
            sbyte desSByte = sbyte.Parse(sourceStr);
            if (desSByte != 12)
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: the string represents random SByte 3");
        try
        {
            SByte SByteVal = (sbyte)this.GetInt32(0, 127);
            string sourceStr = "\u0020" + "-" + SByteVal.ToString() + "\u0020";
            sbyte desSByte = sbyte.Parse(sourceStr);
            if (desSByte != SByteVal * (-1))
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
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: the string is null");
        try
        {
            string sourceStr = null;
            sbyte desSByte = sbyte.Parse(sourceStr);
            TestLibrary.TestFramework.LogError("N001", "the string is null but not throw exception");
            retVal = false;
        }
        catch (ArgumentException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2: the string does not consist of an optional sign follow by a sequence of digit");
        try
        {
            string sourceStr = "helloworld";
            sbyte desSByte = sbyte.Parse(sourceStr);
            TestLibrary.TestFramework.LogError("N003", "the string does not consist of an optional sign follow by a sequence of digit but not throw exception");
            retVal = false;
        }
        catch (FormatException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest3: the string represents digit less than SByte MinValue");
        try
        {
            string sourceStr = "-129";
            sbyte desSByte = sbyte.Parse(sourceStr);
            TestLibrary.TestFramework.LogError("N005", "the string represents digit less than SByte MinValue");
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest4: the string represents digit greater than SByte MaxValue");
        try
        {
            string sourceStr = "128";
            sbyte desSByte = sbyte.Parse(sourceStr);
            TestLibrary.TestFramework.LogError("N007", "the string represents digit greater than SByte MaxValue but not throw exception");
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N008", "Unexpect exception:" + e);
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
