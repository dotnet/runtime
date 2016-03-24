// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// Parse(System.String,System.Globalization.NumberStyles)
/// </summary>
public class DecimalParse2
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        TestLibrary.TestFramework.LogInformation("[Negitive]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        retVal = NegTest7() && retVal;
        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Calling Parse method.");

        try
        {
            Decimal m1 = new decimal(TestLibrary.Generator.GetDouble(-55));
            CultureInfo myCulture = CultureInfo.CurrentCulture;
            string m1ToString = m1.ToString(myCulture);
            Decimal expectValue = m1;
            retVal = VerifyHelper(m1, m1ToString, NumberStyles.AllowDecimalPoint, "001.1") & retVal;
            retVal = VerifyHelper(m1, m1ToString, NumberStyles.Any, "001.2") & retVal;
            retVal = VerifyHelper(m1, m1ToString, NumberStyles.Currency, "001.3") & retVal;
            retVal = VerifyHelper(m1, m1ToString, NumberStyles.Float, "001.4") & retVal;
            retVal = VerifyHelper(m1, m1ToString, NumberStyles.Number, "001.5") & retVal;

           

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Calling Parse method and the decimal is MaxValue and MinValue.");

        try
        {
            Decimal m1 = Decimal.MaxValue;
            CultureInfo myCulture = CultureInfo.CurrentCulture;
            string m1ToString = m1.ToString(myCulture);
            Decimal expectValue = m1;
            Decimal actualValue = Decimal.Parse(m1ToString, NumberStyles.Any);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("002.1", "Parse method should  return " + expectValue);
                retVal = false;
            }

            m1 = Decimal.MinValue;
            m1ToString = m1.ToString(myCulture);
            expectValue = m1;
            actualValue = Decimal.Parse(m1ToString,NumberStyles.Any);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("002.2", "Parse method should  return " + expectValue);
                retVal = false;
            }

        

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Calling Parse method and the decimal is Especial value.");

        try
        {
            Decimal m1 = -9876543210.9876543210m;
            CultureInfo myCulture = CultureInfo.CurrentCulture;
            string m1ToString = m1.ToString(myCulture);
            Decimal expectValue = m1;
            Decimal actualValue = Decimal.Parse(m1ToString, NumberStyles.Any);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("003.1", "Parse method should  return " + expectValue);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #region Negitive test
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: s is a null reference.");

        try
        {
             string m1ToString = null;

             Decimal actualValue = Decimal.Parse(m1ToString, NumberStyles.Any);
            TestLibrary.TestFramework.LogError("101.1", "ArgumentNullException should be caught." );
            retVal = false;
        }
        catch (ArgumentNullException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: s is not in the correct format.");

        try
        {
            string m1ToString = "ADAAAW";

            Decimal actualValue = Decimal.Parse(m1ToString, NumberStyles.Any);
            TestLibrary.TestFramework.LogError("102.1", "FormatException should be caught.");
            retVal = false;
        }
        catch (FormatException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: s represents a number  greater than MaxValue.");

        try
        {
          
            Decimal myDecimal = decimal.MaxValue;
            CultureInfo myCulture = CultureInfo.CurrentCulture;
            string m1ToString = myDecimal.ToString(myCulture);
            m1ToString = m1ToString + m1ToString;
            Decimal actualValue = Decimal.Parse(m1ToString, NumberStyles.Any);
            TestLibrary.TestFramework.LogError("103.1", "OverflowException should be caught.");
            retVal = false;
        }
        catch (OverflowException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("103.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: s represents a number less than MinValue.");

        try
        {

            Decimal myDecimal = decimal.MinValue;
            CultureInfo myCulture = CultureInfo.CurrentCulture;
            string m1ToString = myDecimal.ToString(myCulture);
            m1ToString = m1ToString + Decimal.Negate(myDecimal).ToString(myCulture);
            Decimal actualValue = Decimal.Parse(m1ToString, NumberStyles.Any);
            TestLibrary.TestFramework.LogError("104.1", "OverflowException should be caught.");
            retVal = false;
        }
        catch (OverflowException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool NegTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest5: not support NumberStyles .");

        try
        {
            Decimal m1 = new decimal(TestLibrary.Generator.GetDouble(-55));
            CultureInfo myCulture = CultureInfo.CurrentCulture;
            string m1ToString = m1.ToString(myCulture);
            Decimal expectValue = m1;
            retVal = VerifyNegHelper(m1, m1ToString, NumberStyles.AllowCurrencySymbol, "105.1") & retVal;
            retVal = VerifyNegHelper(m1, m1ToString, NumberStyles.AllowExponent, "105.2") & retVal;
            retVal = VerifyNegHelper(m1, m1ToString, NumberStyles.AllowLeadingSign, "105.4") & retVal;
            retVal = VerifyNegHelper(m1, m1ToString, NumberStyles.AllowLeadingWhite, "105.5") & retVal;
            retVal = VerifyNegHelper(m1, m1ToString, NumberStyles.AllowParentheses, "105.6") & retVal;
            retVal = VerifyNegHelper(m1, m1ToString, NumberStyles.AllowThousands, "105.7") & retVal;
            retVal = VerifyNegHelper(m1, m1ToString, NumberStyles.AllowTrailingSign, "105.8") & retVal;

            retVal = VerifyNegHelper(m1, m1ToString, NumberStyles.AllowTrailingWhite, "105.9") & retVal;
            retVal = VerifyNegHelper(m1, m1ToString, NumberStyles.Integer, "105.11") & retVal;
            retVal = VerifyNegHelper(m1, m1ToString, NumberStyles.None, "105.12") & retVal;
        }

        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("105.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest6: style is the AllowHexSpecifier value.");

        try
        {
            Decimal m1 = new decimal(TestLibrary.Generator.GetDouble(-55));
            CultureInfo myCulture = CultureInfo.CurrentCulture;
            string m1ToString = m1.ToString(myCulture);
            Decimal expectValue = m1;
            retVal = VerifyNegHexHelper(m1, m1ToString, NumberStyles.AllowHexSpecifier, "106.1") & retVal;
            retVal = VerifyNegHexHelper(m1, m1ToString, NumberStyles.HexNumber, "106.2") & retVal;

        }

        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106.0", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest6: style is not a NumberStyles value.");

        try
        {
            Decimal m1 = new decimal(TestLibrary.Generator.GetDouble(-55));
            CultureInfo myCulture = CultureInfo.CurrentCulture;
            string m1ToString = m1.ToString(myCulture);
            Decimal expectValue = m1;
            retVal = VerifyNegHexHelper(m1, m1ToString,(NumberStyles)9999, "107.1") & retVal;
      
        }

        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("107.0", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DecimalParse2 test = new DecimalParse2();

        TestLibrary.TestFramework.BeginTestCase("DecimalParse2");

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
    #region private method
    private bool VerifyHelper(Decimal expectValue, string testValue, NumberStyles myNumberStyles, string errorno)
    {
        bool retVal = true;
        try
        {
            Decimal actualValue = Decimal.Parse(testValue, myNumberStyles);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError(errorno, "Parse should  return " + expectValue);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(errorno + ".0", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    private bool VerifyNegHelper(Decimal expectValue, string testValue, NumberStyles myNumberStyles, string errorno)
    {
        bool retVal = true;
        try
        {
            Decimal actualValue = Decimal.Parse(testValue, myNumberStyles);
           
            TestLibrary.TestFramework.LogError(errorno, "FormatException should be caught.");
            retVal = false;
         
        }
        catch (FormatException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(errorno + ".0", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
     private bool VerifyNegHexHelper(Decimal expectValue, string testValue, NumberStyles myNumberStyles, string errorno)
    {
        bool retVal = true;
        try
        {
            Decimal actualValue = Decimal.Parse(testValue, myNumberStyles);
           
            TestLibrary.TestFramework.LogError(errorno, "ArgumentException should be caught.");
            retVal = false;
         
        }
        catch (ArgumentException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(errorno + ".0", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
