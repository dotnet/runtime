// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// Int16.Parse(string,numberstyle,iformatprovider)
/// </summary>
public class Int16Parse3
{
    #region Privates
    private CultureInfo CurrentCulture = TestLibrary.Utilities.CurrentCulture;
    private CultureInfo customCulture = null;
    private CultureInfo CustomCulture
    {
        get
        {
            if (null == customCulture)
            {
                customCulture = new CultureInfo(CurrentCulture.Name);
                NumberFormatInfo nfi = customCulture.NumberFormat;
                nfi.NumberGroupSeparator = ",";     
                nfi.NumberGroupSizes = new int[] { 3 };
                nfi.NegativeSign = "-";
                nfi.NumberNegativePattern = 1;
                customCulture.NumberFormat = nfi;
            }
            return customCulture;
        }
    }
    #endregion

    #region Public Methods
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        retVal = NegTest7() && retVal;
        retVal = NegTest8() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Test the method with the cultureinfo parameter which  implemented the IFormatProvider interface ");

        try
        {
            string s1 = "1,000";
            int i1 = Int16.Parse(s1, System.Globalization.NumberStyles.AllowThousands, CustomCulture);
            if (i1 != 1000)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2:Test the method with the numberformatinfo parameter which  implemented the IFormatProvider interface ");

        try
        {
            TestLibrary.Utilities.CurrentCulture = CustomCulture; //Parse uses CurrentCulture since numberFormatInfo is created without any parameter.
            string s1 = "  3,000  ";
            int i1 = Int16.Parse(s1, System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowLeadingWhite | System.Globalization.NumberStyles.AllowTrailingWhite, new NumberFormatInfo());
            if (i1 != 3000)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }
        finally
        {
            TestLibrary.Utilities.CurrentCulture = CurrentCulture;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3:Test the method with the DateTimeFormatInfo parameter which  implemented the IFormatProvider interface ");

        try
        {
            TestLibrary.Utilities.CurrentCulture = CustomCulture; 
            string s1 = "-16,466";
            int i1 = Int16.Parse(s1, System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowLeadingSign, new DateTimeFormatInfo());
            if (i1 != -16466)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }
        finally
        {
            TestLibrary.Utilities.CurrentCulture = CurrentCulture;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Test special string \"-3.456\" using the cultureinfo parameter");

        try
        {
            string s1 = "-3.456";
            int i1 = Int16.Parse(s1, System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowLeadingSign, new CultureInfo("pt-BR"));
            if (i1 != -3456)
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Test special string \"  3 01 78\" using the cultureinfo parameter");

        try
        {
            string s1 = "  3 01 78";
            NumberFormatInfo nfi = new CultureInfo("pt-BR").NumberFormat;
            nfi.NumberGroupSizes = new int[] { 2 };
            nfi.NumberGroupSeparator = " ";
            int i1 = Int16.Parse(s1, System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowLeadingWhite, nfi);
            if (i1 != 30178)
            {
                TestLibrary.TestFramework.LogError("009", "The result is not the value as expected. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Using the null reference as the third parameter IFormatProvider");

        try
        {
            string s1 = "  3678";
            int i1 = Int16.Parse(s1, System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowLeadingWhite, null);
            if (i1 != 3678)
            {
                TestLibrary.TestFramework.LogError("011", "The result is not the value as expected. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The argument is null reference");

        try
        {
            string str = null;
            NumberStyles numberstyle = NumberStyles.Any;
            Int16 i1 = Int16.Parse(str, numberstyle, new CultureInfo("en-US"));
            TestLibrary.TestFramework.LogError("101", "the Method did not throw an ArgumentNullException");
            retVal = false;
        }
        catch (ArgumentNullException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: Test format exception 1");

        try
        {
            string str = "-123567";
            NumberStyles numberstyle = NumberStyles.AllowTrailingSign;
            Int16 i1 = Int16.Parse(str, numberstyle, CustomCulture);
            TestLibrary.TestFramework.LogError("103", "the Method did not throw a FormatException");
            retVal = false;
        }
        catch (FormatException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: Test format exception 2");

        try
        {
            string str = "98d5t6w7";
            NumberStyles numberstyle = NumberStyles.Currency;
            Int16 i1 = Int16.Parse(str, numberstyle, new CultureInfo("en-US"));
            TestLibrary.TestFramework.LogError("105", "the Method did not throw a FormatException");
            retVal = false;
        }
        catch (FormatException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: Test format exception 3, the string is white space");

        try
        {
            string str = "  ";
            NumberStyles numberstyle = NumberStyles.None;
            Int16 i1 = Int16.Parse(str, numberstyle, new CultureInfo("en-US"));
            TestLibrary.TestFramework.LogError("107", "the Method did not throw a FormatException");
            retVal = false;
        }
        catch (FormatException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("108", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest5: The string represents a number less than int16.minvalue");

        try
        {
            string str = (Int16.MinValue - 1).ToString();
            NumberStyles numberstyle = NumberStyles.Currency;
            Int16 i1 = Int16.Parse(str, numberstyle, new CultureInfo("en-US"));
            TestLibrary.TestFramework.LogError("109", "the Method did not throw a OverflowException");
            retVal = false;
        }
        catch (OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("110", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest6: The string represents a number greater than int16.maxvalue");

        try
        {
            string str = (Int16.MaxValue + 1).ToString();
            NumberStyles numberstyle = NumberStyles.Currency;
            Int16 i1 = Int16.Parse(str, numberstyle, new CultureInfo("en-US"));
            TestLibrary.TestFramework.LogError("111", "the Method did not throw a OverflowException");
            retVal = false;
        }
        catch (OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("112", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest7: style is not a NumberStyles value");

        try
        {
            string str = TestLibrary.Generator.GetInt16().ToString();
            int i2 = 24568;
            NumberStyles numberstyle = (NumberStyles)i2;
            Int16 i1 = Int16.Parse(str, numberstyle, new CultureInfo("en-US"));
            TestLibrary.TestFramework.LogError("113", "the Method did not throw an ArgumentException,the string is:" + str);
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("114", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest8()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest8: style is not a combination of AllowHexSpecifier and HexNumber");

        try
        {
            string str = "1df6";
            NumberStyles numberstyle = NumberStyles.HexNumber | NumberStyles.AllowTrailingSign;
            Int16 i1 = Int16.Parse(str, numberstyle, new CultureInfo("en-US"));
            TestLibrary.TestFramework.LogError("115", "the Method did not throw an ArgumentException,the string is:");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("116", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        Int16Parse3 test = new Int16Parse3();

        TestLibrary.TestFramework.BeginTestCase("Int16Parse3");

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
