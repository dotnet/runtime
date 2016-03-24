// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

//Parse(System.String,System.Globalization.NumberStyles,System.IFormatProvider)
public class Int32Parse3
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
                NumberFormatInfo nfi = CustomCulture.NumberFormat;
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
//        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;

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
            int i1 = Int32.Parse(s1, System.Globalization.NumberStyles.AllowThousands, CustomCulture);
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
            // if NumberFormatInfo is null, created without any argument, Parse uses CurrrentCulture 
            TestLibrary.Utilities.CurrentCulture = CustomCulture;
            string s1 = "  3,000  ";
            int i1 = Int32.Parse(s1, System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowLeadingWhite | System.Globalization.NumberStyles.AllowTrailingWhite, new NumberFormatInfo());
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
            string s1 = "-56,466";
            int i1 = Int32.Parse(s1, System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowLeadingSign, new DateTimeFormatInfo());
            if (i1 != -56466)
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Test special string \"-3.456.789\" using the cultureinfo parameter");

        try
        {
            string s1 = "-3.456.789";
            int i1 = Int32.Parse(s1, System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowLeadingSign, new CultureInfo("pt-BR"));
            if (i1 != -3456789)
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
/*
    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Test special string \"  3 456 78\" using the cultureinfo parameter");

        try
        {
            string s1 = "  3 456 78";
            int i1 = Int32.Parse(s1, System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowLeadingWhite, new CultureInfo("sma-SE"));
            if (i1 != 345678)
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
*/
    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Using the null reference as the third parameter IFormatProvider");

        try
        {
            string s1 = "  345678";
            int i1 = Int32.Parse(s1, System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowLeadingWhite, null);
            if (i1 != 345678)
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

        TestLibrary.TestFramework.BeginScenario("NegTest1:Set the parameter string to null reference ");

        try
        {
            String s1 = null;
            int i1 = Int32.Parse(s1, System.Globalization.NumberStyles.AllowThousands, new CultureInfo("en-US"));
            TestLibrary.TestFramework.LogError("101", "The ArgumentNullException was not thrown as expected ");
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

        TestLibrary.TestFramework.BeginScenario("NegTest2:Test the ArgumentException");

        try
        {
            String s1 = "1000";
            int i1 = 54543;
            System.Globalization.NumberStyles Ns = (System.Globalization.NumberStyles)i1;
            Int32 i2 = Int32.Parse(s1, Ns, new CultureInfo("en-US"));
            TestLibrary.TestFramework.LogError("103", "the Method did not throw a ArgumentException,patameter is: " + s1);
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

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3:Using improper cultureinfo");

        try
        {
            String s1 = "1 000";
            Int32 i2 = Int32.Parse(s1, System.Globalization.NumberStyles.AllowThousands, CustomCulture);
            TestLibrary.TestFramework.LogError("105", "the Method did not throw a FormatException,patameter is: " + s1);
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

        TestLibrary.TestFramework.BeginScenario("NegTest4:Test the OverflowException");

        try
        {
            String s1 = "2,147,483,650";
            Int32 i2 = Int32.Parse(s1, System.Globalization.NumberStyles.AllowThousands, CustomCulture);
            TestLibrary.TestFramework.LogError("107", "the Method did not throw an OverflowException,patameter is: " + s1);
            retVal = false;
        }
        catch (System.OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("108", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        Int32Parse3 test = new Int32Parse3();

        TestLibrary.TestFramework.BeginTestCase("Int32Parse3");

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