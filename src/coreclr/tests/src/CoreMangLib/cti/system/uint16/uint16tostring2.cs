using System;
using System.Globalization; //for NumberFormatInfo
using TestLibrary;

/// <summary>
/// UInt16.System.IConvertible.ToString(IFormatProvider)
/// Converts the numeric value of this instance to its equivalent string representation 
/// using the specified culture-specific format information. 
/// </summary>
public class UInt16ToString
{
    public static int Main()
    {
        UInt16ToString testObj = new UInt16ToString();

        TestLibrary.TestFramework.BeginTestCase("for method: UInt16.System.ToString(IFormatProvider)");
        if(testObj.RunTests())
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
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        UInt16 uintA;
        uintA = (UInt16)(TestLibrary.Generator.GetInt32(-55) % (UInt16.MaxValue + 1));
        CultureInfo culture = CultureInfo.CurrentCulture;

        return this.DoPosTest("PosTest1: Value is random UInt16 integer, culture is current culture.",
                                       "001", "002", uintA, culture);
    }

    public bool PosTest2()
    {
        UInt16 uintA;
        uintA = UInt16.MaxValue;
        CultureInfo culture = CultureInfo.CurrentCulture;

        return this.DoPosTest("PosTest2: Value is UInt16.MaxValue, culture is current culture.",
                                       "003", "004", uintA, culture);
    }

    public bool PosTest3()
    {

        UInt16 uintA;
        uintA = UInt16.MinValue;
        CultureInfo culture = CultureInfo.CurrentCulture;

        return this.DoPosTest("PosTest3: Value is UInt16.MinValue, culture is current culture.",
                                       "005", "006", uintA, culture);
    }

    public bool PosTest4()
    {
        UInt16 uintA;
        uintA = (UInt16)(TestLibrary.Generator.GetInt32(-55) % (UInt16.MaxValue + 1));
        CultureInfo culture = new CultureInfo("en-US");

        return this.DoPosTest("PosTest6: Value is random UInt16 integer, culture is en-US ",
                                       "007", "008", uintA, culture);
    }

    public bool PosTest5()
    {
        UInt16 uintA;
        uintA = UInt16.MaxValue;
        CultureInfo culture = new CultureInfo("en-US");

        return this.DoPosTest("PosTest6: Value is UInt16.MaxValue, culture is en-US ",
                                       "009", "010", uintA, culture);
    }

    public bool PosTest6()
    {
        UInt16 uintA;
        uintA = UInt16.MinValue;
        CultureInfo culture = new CultureInfo("en-US");

        return this.DoPosTest("PosTest6: Value is UInt16.MinValue, culture is en-US ",
                                       "011", "012", uintA, culture);
    }

    public bool PosTest7()
    {
        UInt16 uintA;
        uintA = (UInt16)(TestLibrary.Generator.GetInt32(-55) % (UInt16.MaxValue + 1));
        CultureInfo culture = new CultureInfo("fr-FR");

        return this.DoPosTest("PosTest7: Value is random UInt16 integer, culture is fr-FR.",
                                       "013", "014", uintA, culture);
    }

    public bool PosTest8()
    {
        UInt16 uintA;
        uintA = UInt16.MaxValue;
        CultureInfo culture = new CultureInfo("fr-FR");

        return this.DoPosTest("PosTest8: Value is UInt16.MaxValue, culture is fr-FR.",
                                       "015", "016", uintA, culture);
    }

    public bool PosTest9()
    {
        UInt16 uintA;
        uintA = UInt16.MinValue;
        CultureInfo culture = new CultureInfo("fr-FR");

        return this.DoPosTest("PosTest9: Value is UInt16.MinValue, culture is fr-FR.",
                                       "017", "018", uintA, culture);
    }
    #endregion

    #region Helper method for tests

    public bool DoPosTest(string testDesc,
                                    string errorNum1,
                                    string errorNum2,
                                    UInt16 uintA,
                                    CultureInfo culture)
    {
        bool retVal = true;
        string errorDesc;

        IFormatProvider provider;
        string expectedValue;
        string actualValue;

        TestLibrary.TestFramework.BeginScenario(testDesc);
        try
        {
            provider = (IFormatProvider)culture.NumberFormat;

            actualValue = uintA.ToString(provider);
            expectedValue = GlobLocHelper.OSUInt16ToString(uintA, culture);

            if (actualValue != expectedValue)
            {
                errorDesc =
                    string.Format("The string representation of {0} is not the value {1} as expected: actual({2})",
                    uintA, expectedValue, actualValue);
                errorDesc += "\nThe format info is " + culture.Name + " culture speicifed.";
                TestLibrary.TestFramework.LogError("005", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            TestLibrary.TestFramework.LogError("006", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    #endregion
}

