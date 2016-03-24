// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// Int16.Parse(String,System.Globalization.NumberStyles)
/// </summary>
public class Int16Parse2
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
                //default is "," in most of the cultures, but in some cultures like DE, TR it is "."
                //nfi.NumberGroupSeparator = ",";     
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
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;
        retVal = PosTest10() && retVal;

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Normally test a random string of int16 ");

        try
        {
            string str = TestLibrary.Generator.GetInt16().ToString();
            NumberStyles numberstyle = NumberStyles.Any;
            Int16 i1 = Int16.Parse(str, numberstyle);
            Int16 i2 = Convert.ToInt16(str);
            if (i1 != i2)
            {
                TestLibrary.TestFramework.LogError("001", "the result is not the value as expected, the string is " + str);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Test the int16.MaxValue");

        try
        {
            string str = "32767";
            NumberStyles numberstyle = NumberStyles.Any;
            Int16 i1 = Int16.Parse(str, numberstyle);
            if (i1 != 32767)
            {
                TestLibrary.TestFramework.LogError("003", "the result is not the value as expected ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Test the int16.MinValue");

        try
        {
            TestLibrary.Utilities.CurrentCulture = CustomCulture; //NegativeSign may differ
            string str = "-32768";
            NumberStyles numberstyle = NumberStyles.Any;
            Int16 i1 = Int16.Parse(str, numberstyle);
            if (i1 != -32768)
            {
                TestLibrary.TestFramework.LogError("005", "the result is not the value as expected ");
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: The argument with white space in both beginning and the end");

        try
        {
            string str2;
            string str = str2 = TestLibrary.Generator.GetInt16().ToString();
            str = "  " + str;
            str = str + "  ";
            NumberStyles numberstyle = NumberStyles.Any;
            Int16 i1 = Int16.Parse(str, numberstyle);
            Int16 i2 = Int16.Parse(str2, numberstyle);
            if (i1 != i2)
            {
                TestLibrary.TestFramework.LogError("007", "the result is not the value as expected,the string is :" + str);
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Test the parameter \"-0\"");

        try
        {
            TestLibrary.Utilities.CurrentCulture = CustomCulture; //NegativeSign may differ
            NumberStyles numberstyle = NumberStyles.Any;
            Int16 i1 = Int16.Parse("-0", numberstyle);
            if (i1 != 0)
            {
                TestLibrary.TestFramework.LogError("009", "the result is not the value as expected ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }
        finally
        {
            TestLibrary.Utilities.CurrentCulture = CurrentCulture;
        }
        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Test the parameter NumberStyles.AllowThousands");

        try
        {
            TestLibrary.Utilities.CurrentCulture = CustomCulture; //NumberGroupSeparator may different than ","
            string str = "32" + CustomCulture.NumberFormat.NumberGroupSeparator + "700";
            NumberStyles numberstyle = NumberStyles.AllowThousands;
            Int16 i1 = Int16.Parse(str, numberstyle);
            if (i1 != 32700)
            {
                TestLibrary.TestFramework.LogError("011", "the result is not the value as expected ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            retVal = false;
        }
        finally
        {
            TestLibrary.Utilities.CurrentCulture = CurrentCulture;
        }
        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest7: Test the parameter NumberStyles.AllowParentheses");

        try
        {
            Int16 int16 = TestLibrary.Generator.GetInt16();
            string str = "(" + int16.ToString() + ")";
            NumberStyles numberstyle = NumberStyles.AllowParentheses;
            Int16 i1 = Int16.Parse(str, numberstyle);
            if (i1 != (-int16))
            {
                TestLibrary.TestFramework.LogError("013", "the result is not the value as expected,the int16 is: " + int16);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest8: The string with white space in both the beginning and the end");

        try
        {
            Int16 int16 = TestLibrary.Generator.GetInt16();
            string str = "   " + int16.ToString() + "   ";
            NumberStyles numberstyle = NumberStyles.AllowLeadingWhite | System.Globalization.NumberStyles.AllowTrailingWhite;
            Int16 i1 = Int16.Parse(str, numberstyle);
            if (i1 != int16)
            {
                TestLibrary.TestFramework.LogError("015", "the result is not the value as expected ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest9()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest9: Test the parameters NumberStyles.AllowTrailingSign");

        try
        {
            TestLibrary.Utilities.CurrentCulture = CustomCulture; //NegativeSign may be different
            string str = "8765-";
            NumberStyles numberstyle = NumberStyles.AllowTrailingSign;
            Int16 i1 = Int16.Parse(str, numberstyle);
            if (i1 != -8765)
            {
                TestLibrary.TestFramework.LogError("017", "the result is not the value as expected ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception: " + e);
            retVal = false;
        }
        finally
        {
            TestLibrary.Utilities.CurrentCulture = CurrentCulture; //NegativeSign may be different
        }
        return retVal;
    }

    public bool PosTest10()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest10: Test the parameters NumberStyles.HexNumber");

        try
        {
            string str = "5e81";
            NumberStyles numberstyle = NumberStyles.HexNumber;
            Int16 i1 = Int16.Parse(str, numberstyle);
            if (i1 != 24193)
            {
                TestLibrary.TestFramework.LogError("019", "the result is not the value as expected ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Unexpected exception: " + e);
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
            Int16 i1 = Int16.Parse(str, numberstyle);
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
            TestLibrary.Utilities.CurrentCulture = CustomCulture; //NegativeSign may be different
            string str = "-123567";
            NumberStyles numberstyle = NumberStyles.AllowTrailingSign;
            Int16 i1 = Int16.Parse(str, numberstyle);
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
        finally
        {
            TestLibrary.Utilities.CurrentCulture = CurrentCulture;
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
            Int16 i1 = Int16.Parse(str, numberstyle);
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
            Int16 i1 = Int16.Parse(str, numberstyle);
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
            Int16 i1 = Int16.Parse(str, numberstyle);
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
            Int16 i1 = Int16.Parse(str, numberstyle);
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
            Int16 i1 = Int16.Parse(str, numberstyle);
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
            Int16 i1 = Int16.Parse(str, numberstyle);
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
        Int16Parse2 test = new Int16Parse2();

        TestLibrary.TestFramework.BeginTestCase("Int16Parse2");

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
