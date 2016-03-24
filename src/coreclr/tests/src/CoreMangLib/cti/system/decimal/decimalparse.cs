// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// Parse(System.String)
/// </summary>
public class DecimalParse
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
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
            Decimal actualValue = Decimal.Parse(m1ToString);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.1", "Parse method should  return " + expectValue);
                retVal = false;
            }


            m1 = new decimal(TestLibrary.Generator.GetInt32(-55));
            m1ToString = m1.ToString(myCulture);
            expectValue = m1;
            actualValue = Decimal.Parse(m1ToString);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.2", "Parse method should  return " + expectValue);
                retVal = false;
            }

            m1 = new decimal(TestLibrary.Generator.GetSingle(-55));
            m1ToString = m1.ToString(myCulture);
            expectValue = m1;
            actualValue = Decimal.Parse(m1ToString);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.3", "Parse method should  return " + expectValue);
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
            Decimal actualValue = Decimal.Parse(m1ToString);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("002.1", "Parse method should  return " + expectValue);
                retVal = false;
            }

            m1 = Decimal.MinValue;
            m1ToString = m1.ToString(myCulture);
            expectValue = m1;
            actualValue = Decimal.Parse(m1ToString);
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
            Decimal actualValue = Decimal.Parse(m1ToString);
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
   
    #endregion

    public static int Main()
    {
        DecimalParse test = new DecimalParse();

        TestLibrary.TestFramework.BeginTestCase("DecimalParse");

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
    public TypeCode GetExpectValue(Decimal myValue)
    {
        return TypeCode.Decimal;
    }
    #endregion
}
