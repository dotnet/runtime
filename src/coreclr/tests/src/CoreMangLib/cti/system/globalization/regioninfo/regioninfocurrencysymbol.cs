using System;
using System.Globalization;
/// <summary>
/// RegionInfo.CurrencySymbol
/// </summary>
public class RegionInfoCurrencySymbol
{
    public static int Main()
    {
        RegionInfoCurrencySymbol regInfoCurrencySymbol = new RegionInfoCurrencySymbol();
        TestLibrary.TestFramework.BeginTestCase("RegionInfoCurrencySymbol");
        if (regInfoCurrencySymbol.RunTests())
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
		return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the CurrencySymbol property in RegionInfo object 1");
        try
        {
            CultureInfo myCulture = new CultureInfo("en-US");
            RegionInfo regionInfo = new RegionInfo(myCulture.Name);
            string strCurrencySymbol = regionInfo.CurrencySymbol;            
            if (strCurrencySymbol != "$")
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is $ but the ActualResult is " + strCurrencySymbol);
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Return the CurrencySymbol property in RegionInfo object 2");
        try
        {
            RegionInfo regionInfo = new RegionInfo("zh-CN");
            string strCurrencySymbol = regionInfo.CurrencySymbol;
            if (strCurrencySymbol != (TestLibrary.Utilities.IsVistaOrLater ? "\u00A5" : "\uFFE5"))
            {
				TestLibrary.TestFramework.LogError("003", "the ExpectResult is "+ (TestLibrary.Utilities.IsVista ? "\u00A5" : "\uFFE5") + " but the ActualResult is " + strCurrencySymbol);
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
    #endregion
}