// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

//System.Int32.Parse(System.String,System.Globalization.NumberStyles)
public class Int32Parse2
{
    #region Privates
    private CultureInfo CurrentCulture = TestLibrary.Utilities.CurrentCulture;
    private CultureInfo CustomCulture = null;
    private CultureInfo CustomCultureForNumberFormat
    {
        get
        {
            if (null == CustomCulture)
            {
                CustomCulture = new CultureInfo(CurrentCulture.Name);
                NumberFormatInfo nfi = CustomCulture.NumberFormat;
                //default is "," in most of the cultures, but in some cultures like DE, TR it is "."
                //nfi.NumberGroupSeparator;     
                nfi.NumberGroupSizes = new int[] { 3 };
                nfi.NegativeSign = "-";         //for NegTest5
                nfi.NumberNegativePattern = 1;  //for NegTest5

                CustomCulture.NumberFormat = nfi;
            }
            return CustomCulture;
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;

        return retVal;
    }
    
    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1:Test the parameter NumberStyles.AllowThousands ");
                
        try
        {
            TestLibrary.Utilities.CurrentCulture = CustomCultureForNumberFormat;
            String groupSeparator = CustomCultureForNumberFormat.NumberFormat.NumberGroupSeparator;
            
            string s1 = "2" + groupSeparator + "147" + groupSeparator + "483" + groupSeparator + "647";
            Int32 i1 = Int32.Parse(s1, System.Globalization.NumberStyles.AllowThousands);
            if (i1 != Int32.MaxValue)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected ");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }
        finally
        {
            TestLibrary.Utilities.CurrentCulture = CurrentCulture;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2:Test the parameters NumberStyles.AllowParentheses ");

        try
        {
            string s1 = "(105)";
            Int32 i1 = Int32.Parse(s1, System.Globalization.NumberStyles.AllowParentheses);
            if (i1 != -105)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected ");
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

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest3:Test the parameters with white space in both the beginning and the end ");

        try
        {
            string s1 = "   8765   ";
            Int32 i1 = Int32.Parse(s1, System.Globalization.NumberStyles.AllowLeadingWhite | System.Globalization.NumberStyles.AllowTrailingWhite);
            if (i1 != 8765)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected ");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest4:Test the parameters NumberStyles.AllowTrailingSign");

        try
        {
            string s1 = "8765-";
            Int32 i1 = Int32.Parse(s1, System.Globalization.NumberStyles.AllowTrailingSign);
            if (i1 != -8765)
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected ");
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

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest5:Test the parameters NumberStyles.HexNumber");

        try
        {
            string s1 = " 6a9fe7 ";
            Int32 i1 = Int32.Parse(s1, System.Globalization.NumberStyles.HexNumber);
            if (i1 != 6987751)
            {
                TestLibrary.TestFramework.LogError("009", "The result is not the value as expected ");
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
    #endregion

    #region Negative Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: null reference parameter");

        try
        {
            string s1 = null;
            Int32 i1 = Int32.Parse(s1, System.Globalization.NumberStyles.AllowLeadingSign);
            TestLibrary.TestFramework.LogError("101", "the Method did not throw a ArgumentNullException");
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: test FormatException1 ");

        string s1 = " (321)677 ";
        try
        {
            Int32 i1 = Int32.Parse(s1, System.Globalization.NumberStyles.AllowParentheses);
            TestLibrary.TestFramework.LogError("103", "the Method did not throw a FormatException,patameter is: " + s1);
            retVal = false;
        }
        catch (System.FormatException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: test ArgumentException1");

        try
        {
            string s1 = "43dce6a-";
            Int32 i1 = Int32.Parse(s1, System.Globalization.NumberStyles.HexNumber | System.Globalization.NumberStyles.AllowTrailingSign);
            TestLibrary.TestFramework.LogError("105", "the Method did not throw a FormatException,patameter is: " + s1);
            retVal = false;
        }
        catch (System.ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: test ArgumentException2");

        try
        {
            string s1 = "4346";
            int i1 = 54543;
            System.Globalization.NumberStyles Ns = (System.Globalization.NumberStyles)i1;
            Int32 i2 = Int32.Parse(s1, Ns);
            TestLibrary.TestFramework.LogError("107", "the Method did not throw a FormatException,patameter is: " + s1);
            retVal = false;
        }
        catch (System.ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("108", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest5: test OverflowException ");

        try
        {
            TestLibrary.Utilities.CurrentCulture = CustomCultureForNumberFormat;
            String groupSeparator = CustomCultureForNumberFormat.NumberFormat.NumberGroupSeparator;

            string s1 = "-2" + groupSeparator + "147" + groupSeparator + "483" + groupSeparator + "650";
            Int32 i1 = Int32.Parse(s1, System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowLeadingSign);
            TestLibrary.TestFramework.LogError("109", "the Method did not throw a FormatException,patameter is: " + s1);
            retVal = false;
        }
        catch (System.OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("110", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        finally
        {
            TestLibrary.Utilities.CurrentCulture = CurrentCulture;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        Int32Parse2 test = new Int32Parse2();

        TestLibrary.TestFramework.BeginTestCase("Int32Parse2");

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
