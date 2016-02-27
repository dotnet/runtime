// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// Convert.ToDouble(System.String,System.IFormatProvider)
/// </summary>
public class ConvertToDouble14
{
    public static int Main()
    {
        ConvertToDouble14 testObj = new ConvertToDouble14();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToDouble(System.String,System.IFormatProvider)");
        if (testObj.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;

        return retVal;
    }
    
    private CultureInfo CurrentCulture = CultureInfo.CurrentCulture;
    private CultureInfo customCulture = null;
    private CultureInfo CustomCulture
    {
        get
        {
            if (null == customCulture)
            {
                customCulture = new CultureInfo(CultureInfo.CurrentCulture.Name);
                NumberFormatInfo customNFI = new NumberFormatInfo();

                customNFI.NumberDecimalSeparator = ".";
                customNFI.NumberGroupSeparator = ",";
                customNFI.NumberGroupSizes = new int[] { 3 };

                customCulture.NumberFormat = customNFI;
            }
            return customCulture;
        }
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string errorDesc;

        Double actualValue;
        string strDouble = "3,265.12358";
        Double expVal = Double.Parse(strDouble, CustomCulture.NumberFormat);

        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify  the string value is valid Double value");

        try
        {
            actualValue = Convert.ToDouble(strDouble, CustomCulture.NumberFormat);

            if (actualValue != expVal)
            {
                errorDesc = "The Double value " + strDouble + " is not the value " +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("001", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\n The string  is " + strDouble;
            TestLibrary.TestFramework.LogError("002", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string errorDesc;
        // if provider null then currentculture is used
        TestLibrary.Utilities.CurrentCulture = CustomCulture;
        NumberFormatInfo provider = null;

        Double actualValue;
        string strDouble = "3265.12358";
        Double expVal = Double.Parse(strDouble);

        TestLibrary.TestFramework.BeginScenario("PosTest2:Verify the IFormatProvider is a null reference");

        try
        {
            actualValue = Convert.ToDouble(strDouble, provider);

            if (actualValue != expVal)
            {
                errorDesc = "The Double value " + strDouble + " is not the value " +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("003", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\n The string  is " + strDouble;
            TestLibrary.TestFramework.LogError("004", errorDesc);
            retVal = false;
        }
        TestLibrary.Utilities.CurrentCulture = CurrentCulture;
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string errorDesc;

        Double actualValue;
        string strDouble = "3265.";
        Double expVal = Double.Parse(strDouble, CustomCulture.NumberFormat);

        TestLibrary.TestFramework.BeginScenario("PosTest3:Verify  the string end with radix point");

        try
        {
            actualValue = Convert.ToDouble(strDouble, CustomCulture.NumberFormat);

            if (actualValue != expVal)
            {
                errorDesc = "The Double value " + strDouble + " is not the value " +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("005", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\n The string  is " + strDouble;
            TestLibrary.TestFramework.LogError("006", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        string errorDesc;

        Double actualValue;
        string strDouble = ".3265";
        Double expVal = Double.Parse(strDouble, CustomCulture.NumberFormat);

        TestLibrary.TestFramework.BeginScenario("PosTest4:Verify  the string start with radix point");

        try
        {
            actualValue = Convert.ToDouble(strDouble, CustomCulture.NumberFormat);

            if (actualValue != expVal)
            {
                errorDesc = "The Double value " + strDouble + " is not the value " +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("007", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\n The string  is " + strDouble;
            TestLibrary.TestFramework.LogError("008", errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion

    #region NegativeTesting
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:Verify the parameter is a random char...");

        string strDouble = "3,265.123c58";

        try
        {
            Convert.ToDouble(strDouble, CustomCulture.NumberFormat);
            TestLibrary.TestFramework.LogError("009", "FormatException is not thrown as expected ." + "\n string is " + strDouble);
            retVal = false;
        }
        catch (FormatException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2:Verify the parameter  represents a number less than MinValue...");

        string strDouble = "-1.7976931348623159E+308";

        try
        {
            Convert.ToDouble(strDouble, CustomCulture.NumberFormat);
            TestLibrary.TestFramework.LogError("011", "OverflowException is not thrown as expected ." + "\n string is " + strDouble);
            retVal = false;
        }
        catch (OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest3:Verify the parameter  represents a number greater than MaxValue...");

        string strDouble = "1.7976931348623159E+308";

        try
        {
            Convert.ToDouble(strDouble, CustomCulture.NumberFormat);
            TestLibrary.TestFramework.LogError("013", "OverflowException is not thrown as expected ." + "\n string is " + strDouble);
            retVal = false;
        }
        catch (OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest4:Verify the parameter does not supply IFormatProvider...");

        string strDouble = "1.485.355.146,79769";

        try
        {
            Convert.ToDouble(strDouble, CustomCulture.NumberFormat);
            TestLibrary.TestFramework.LogError("015", "FormatException is not thrown as expected ." + "\n string is " + strDouble);
            retVal = false;
        }
        catch (FormatException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest5:Verify the string is empty");

        string strDouble = "";

        try
        {
            Convert.ToDouble(strDouble, CustomCulture.NumberFormat);
            TestLibrary.TestFramework.LogError("017", "FormatException is not thrown as expected ." + "\n string is empty ");
            retVal = false;
        }
        catch (FormatException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

}