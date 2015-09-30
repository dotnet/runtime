using System;
using TestLibrary;

/// <summary>
/// SByte.ToString(String)
/// </summary>
public class SByteToString3
{
    public static int Main()
    {
        SByteToString3 sbyteTS3 = new SByteToString3();
        TestLibrary.TestFramework.BeginTestCase("SByteToString3");
        if (sbyteTS3.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:The SByte MaxValue ToString 1");
        try
        {
            sbyte SByteVal = sbyte.MaxValue;
            string format1 = null;
            string format2 = string.Empty;
            string desStr1 = SByteVal.ToString(format1);
            string desStr2 = SByteVal.ToString(format2);
            if (desStr1 != GlobLocHelper.OSSByteToString(SByteVal, format1) || desStr2 != GlobLocHelper.OSSByteToString(SByteVal, format2)) 
            {
                TestLibrary.TestFramework.LogError("001", "The ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:The SByte MaxValue ToString 2");
        try
        {
            sbyte SByteVal = sbyte.MaxValue;
            string format = "X";
            string desStr = SByteVal.ToString(format);
            if (desStr != "7F")
            {
                TestLibrary.TestFramework.LogError("003", "The ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:The SByte MaxValue ToString 3");
        try
        {
            sbyte SByteVal = sbyte.MaxValue;
            string format = "D";
            string desStr = SByteVal.ToString(format);
            if (desStr != GlobLocHelper.OSSByteToString(SByteVal, format))
            {
                TestLibrary.TestFramework.LogError("005", "The ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:The SByte MinValue ToString 1");
        try
        {
            sbyte SByteVal = sbyte.MinValue;
            string format = "X";
            string desStr = SByteVal.ToString(format);
            if (desStr != "80")
            {
                TestLibrary.TestFramework.LogError("007", "The ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest5:The SByte MinValue ToString 2");
        try
        {
            sbyte SByteVal = sbyte.MinValue;
            string format = "D";
            string desStr = SByteVal.ToString(format);
            if (desStr != GlobLocHelper.OSSByteToString(SByteVal, format))
            {
                TestLibrary.TestFramework.LogError("009", "The ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest6:The random SByte ToString 1");
        try
        {
            sbyte SByteVal = 123;
            string format = "D";
            string desStr = SByteVal.ToString(format);
            if (desStr != GlobLocHelper.OSSByteToString(SByteVal, format))
            {
                TestLibrary.TestFramework.LogError("011", "The ExpectResult is not the ActualResult");
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
    public bool PosTest7()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest7:The random SByte ToString 2");
        try
        {
            sbyte SByteVal = -99;
            string format = "X";
            string desStr = SByteVal.ToString(format);
            if (desStr != "9D")
            {
                TestLibrary.TestFramework.LogError("013", "The ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:the format is Invalid");
        try
        {
            sbyte SByteVal = (sbyte)(this.GetInt32(0, 128) + this.GetInt32(0, 129) * (-1));
            string format = "W";
            string desStr = SByteVal.ToString(format);
            TestLibrary.TestFramework.LogError("N001", "the format is Invalid but not throw exception");
            retVal = false;
        }
        catch (FormatException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpect exception:" + e);
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
