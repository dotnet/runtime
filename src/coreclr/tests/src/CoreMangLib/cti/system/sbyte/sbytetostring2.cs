using System;
using System.Globalization;
using TestLibrary;

/// <summary>
/// SByte.ToString(IFormatProvider)
/// </summary>
public class SByteToString2
{
    public static int Main()
    {
        SByteToString2 sbyteTS2 = new SByteToString2();
        TestLibrary.TestFramework.BeginTestCase("SByteToString2");
        if (sbyteTS2.RunTests())
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
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:The SByte MaxValue ToString 1");
        try
        {
            sbyte SByteVal = sbyte.MaxValue;
            IFormatProvider provider = new CultureInfo("en-us");
            string desStr = SByteVal.ToString(provider);
            if (desStr != GlobLocHelper.OSSByteToString(SByteVal, new CultureInfo("en-US")))
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
            IFormatProvider provider = null;
            string desStr = SByteVal.ToString(provider);
            if (desStr != GlobLocHelper.OSSByteToString(SByteVal, (CultureInfo)null))
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:The SByte MinValue ToString 1");
        try
        {
            sbyte SByteVal = sbyte.MinValue;
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numinfo = culture.NumberFormat;
            numinfo.NumberDecimalDigits = 0;
            numinfo.NumberNegativePattern = 0;
            numinfo.NegativeSign = "-";
            string desStr = SByteVal.ToString(numinfo);
            if (desStr != "-128")
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:The SByte MinValue ToString 2");
        try
        {
            sbyte SByteVal = sbyte.MinValue;
            IFormatProvider provider = new CultureInfo("en-us");
            string desStr = SByteVal.ToString(provider);
            if (desStr != GlobLocHelper.OSSByteToString(SByteVal, new CultureInfo("en-us")))
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
        TestLibrary.TestFramework.BeginScenario("PosTest5:The SByte MinValue ToString 3");
        try
        {
            sbyte SByteVal = sbyte.MinValue;
            IFormatProvider provider = null;
            string desStr = SByteVal.ToString(provider);
            if (desStr != GlobLocHelper.OSSByteToString(SByteVal, (CultureInfo)null))
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
            sbyte SByteVal = -99;
            IFormatProvider provider = new CultureInfo("en-us");
            string desStr = SByteVal.ToString(provider);
            if (desStr != GlobLocHelper.OSSByteToString(SByteVal, new CultureInfo("en-us")))
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
            sbyte SByteVal = 123;
            IFormatProvider provider = null;
            string desStr = SByteVal.ToString(provider);
            if (desStr != GlobLocHelper.OSSByteToString(SByteVal, (CultureInfo)null))
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
}