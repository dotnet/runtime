using System;
using System.Globalization;
using TestLibrary;

//System.Int32.ToString(System.String,System.IFormatProvider);
public class Int32ToString4
{
    #region Private Methods
    private CultureInfo CurrentCulture = TestLibrary.Utilities.CurrentCulture;
    private CultureInfo customCulture = null;

    private CultureInfo CustomCulture
    {
        get
        {
            if (null == customCulture)
            {
                customCulture = new CultureInfo(CultureInfo.CurrentCulture.Name);
                NumberFormatInfo nfi = customCulture.NumberFormat;
                //For "G"
                // NegativeSign, NumberDecimalSeparator, NumberDecimalDigits, PositiveSign
                nfi.NegativeSign = "@";                 //Default: "-"
                nfi.NumberDecimalSeparator = ",";       //Default: "."
                nfi.NumberDecimalDigits = 3;            //Default: 2
                nfi.PositiveSign = "++";                //Default: "+"

                //For "E"
                // PositiveSign, NegativeSign, and NumberDecimalSeparator. 
                // If precision specifier is omitted, a default of six digits after the decimal point is used.

                //For "R"
                // NegativeSign, NumberDecimalSeparator and PositiveSign

                //For "X", The result string isn't affected by the formatting information of the current NumberFormatInfo

                //For "C"
                // CurrencyPositivePattern, CurrencySymbol, CurrencyDecimalDigits, CurrencyDecimalSeparator, CurrencyGroupSeparator, CurrencyGroupSizes, NegativeSign and CurrencyNegativePattern
                nfi.CurrencyDecimalDigits = 3;          //Default: 2
                nfi.CurrencyDecimalSeparator = ",";     //Default: ","
                nfi.CurrencyGroupSeparator = ".";       //Default: "."
                nfi.CurrencyGroupSizes = new int[] { 2 };  //Default: new int[]{3}
                nfi.CurrencyNegativePattern = 2;        //Default: 0
                nfi.CurrencyPositivePattern = 1;        //Default: 0
                nfi.CurrencySymbol = "USD";             //Default: "$" 

                //For "D"
                // NegativeSign

                //For "E"
                // PositiveSign, NumberDecimalSeparator and NegativeSign. 
                // If precision specifier is omitted, a default of six digits after the decimal point is used.
                nfi.PositiveSign = "++";                //Default: "+"
                nfi.NumberDecimalSeparator = ",";       //Default: "."

                //For "F"
                // NumberDecimalDigits, and NumberDecimalSeparator and NegativeSign.
                nfi.NumberDecimalDigits = 3;            //Default: 2

                //For "N"
                // NumberGroupSizes, NumberGroupSeparator, NumberDecimalSeparator, NumberDecimalDigits, NumberNegativePattern and NegativeSign.
                nfi.NumberGroupSizes = new int[] { 2 }; //Default: 3
                nfi.NumberGroupSeparator = "#";         //Default: ","

                //For "P"
                // PercentPositivePattern, PercentNegativePattern, NegativeSign, PercentSymbol, PercentDecimalDigits, PercentDecimalSeparator, PercentGroupSeparator and PercentGroupSizes 
                nfi.PercentPositivePattern = 1;         //Default: 0
                nfi.PercentNegativePattern = 2;         //Default: 0
                nfi.PercentSymbol = "~";                //Default: "%"
                nfi.PercentDecimalDigits = 4;           //Default: 2
                nfi.PercentDecimalSeparator = ",";      //Default: "."
                nfi.PercentGroupSizes[0] = 2;           //Default: 3
                nfi.PercentGroupSeparator = ",";        //Default: "," in most cultures, "." in DE and TR        

                customCulture.NumberFormat = nfi;
            }
            return customCulture;
        }
    }

    private bool VerifyToString(String id, Int32 myInt, String format, String expected, CultureInfo ci)
    {
        try
        {
            String actual = myInt.ToString(format, ci);
            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError(id + "_001", "Expected: " + expected + " Actual: " + actual);
                return false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(id + "_002", "Unexpected exception occurs: " + e);
            return false;
        }
        return true;
    }
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal &= VerifyToString("PostTest1", Int32.MaxValue, "G", "2147483647", CustomCulture);
        retVal &= VerifyToString("PostTest2", Int32.MinValue, "X", "80000000", new CultureInfo("it-CH"));
        retVal &= VerifyToString("PostTest3", -0, "d", "0", new CultureInfo("pt-PT"));
        retVal &= VerifyToString("PostTest4", -1, "d", "@1", CustomCulture);
        retVal &= VerifyToString("PostTest5", -00000765, "f", "@765,000", CustomCulture);
        retVal &= VerifyToString("PostTest6", 18650000, "e", TestLibrary.Utilities.IsWindows ? "1,865000e++007" : "1,865000e7", CustomCulture);
        retVal &= VerifyToString("PostTest7", 18650000, "n", "18#65#00#00,000", CustomCulture);
        retVal &= VerifyToString("PostTest8", 56781100, "p", "5,678,110,000,0000~", CustomCulture);
        retVal &= VerifyToString("PostTest9", -76500, "c", "USD@7.65.00,000", CustomCulture);

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal &= NegTest1();
        if (Utilities.IsWindows) //Mac has no notion of invalid culture
            retVal = NegTest2() && retVal;
        return retVal;
    }
    #endregion
        
    #region Negative Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Input an invalid argument\"K\"");

        try
        {
            Int32 i1 = TestLibrary.Generator.GetInt32();
            string s1 = i1.ToString("k", null);
            TestLibrary.TestFramework.LogError("101", "The FormatException was not thrown as expected ");
            retVal = false;
        }
        catch (System.FormatException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Input a not supported cultureinfo\"PR\"");

        try
        {
            Int32 i1 = TestLibrary.Generator.GetInt32();
            string s1 = i1.ToString("d", new CultureInfo("PRafasfdasda=-asdf"));
            TestLibrary.TestFramework.LogError("103", "The ArgumentException was not thrown as expected ");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

   
    #endregion
    
    public static int Main()
    {
        Int32ToString4 test = new Int32ToString4();

        TestLibrary.TestFramework.BeginTestCase("Int32ToString4");

        if (test.RunTests())
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
}
