// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// RegionInfo.ISOCurrencySymbol [v-minch]
/// </summary>
public class RegionInfoISOCurrencySymbol
{
    public static int Main()
    {
        RegionInfoISOCurrencySymbol regInfoISOCurrencySymbol = new RegionInfoISOCurrencySymbol();
        TestLibrary.TestFramework.BeginTestCase("RegionInfoISOCurrencySymbol");
        if (regInfoISOCurrencySymbol.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the property ISOCurrencySymbol in RegionInfo object 1");
        try
        {
            RegionInfo regionInfo = new RegionInfo("en-US");
            string strISOCurrency = regionInfo.ISOCurrencySymbol;
            if (strISOCurrency != "USD")
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is USD but the ActualResult is (" + strISOCurrency +")");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Return the property ISOCurrencySymbol in RegionInfo object 2");
        try
        {
            RegionInfo regionInfo = new RegionInfo("zh-CN");
            string strISOCurrency = regionInfo.ISOCurrencySymbol;
            if (strISOCurrency != "CNY")
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is CNY but the ActualResult is (" + strISOCurrency +")");
                retVal = false;
            }
        }
		catch (ArgumentException)
		{
			TestLibrary.TestFramework.LogInformation("The East Asian Languages are not installed. Skipping test(s)");
			retVal = true;
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Return the property ISOCurrencySymbol in RegionInfo object 3");
        try
        {
            RegionInfo regionInfo = new RegionInfo("de-DE");
            string strISOCurrency = regionInfo.ISOCurrencySymbol;
            // On an older OS which does not have the right uptodate information, we can report DEM as the currency for German
            if (TestLibrary.Utilities.IsVistaOrLater)
            {
                if (strISOCurrency != "EUR")
                {
                    TestLibrary.TestFramework.LogError("005", "the ExpectResult is EUR but the ActualResult is (" + strISOCurrency + ")");
                    retVal = false;
                }
            }
            else
            {
                if (strISOCurrency != "EUR" && strISOCurrency != "DEM")
                {
                    TestLibrary.TestFramework.LogError("005", "the ExpectResult is EUR or DEM but the ActualResult is (" + strISOCurrency + ")");
                    retVal = false;
                }
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:Return the property ISOCurrencySymbol in RegionInfo object 4");
        try
        {
            RegionInfo regionInfo = new RegionInfo("it-IT");
            string strISOCurrency = regionInfo.ISOCurrencySymbol;
            // On an older OS which does not have the right uptodate information, we can report ITL as the currency for Italian
            if (TestLibrary.Utilities.IsVistaOrLater)
            {
                if (strISOCurrency != "EUR")
                {
                    TestLibrary.TestFramework.LogError("007", "the ExpectResult is EUR but the ActualResult is (" + strISOCurrency + ")");
                    retVal = false;
                }
            }
            else
            {
                if (strISOCurrency != "EUR" && strISOCurrency != "ITL")
                {
                    TestLibrary.TestFramework.LogError("007", "the ExpectResult is EUR or ITL but the ActualResult is (" + strISOCurrency + ")");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}