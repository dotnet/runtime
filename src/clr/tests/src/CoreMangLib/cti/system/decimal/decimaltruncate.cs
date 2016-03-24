// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// Truncate(System.Decimal)
/// </summary>
public class DecimalTruncate
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
       
        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Calling Truncate method.");

        try
        {
            Decimal m1 =new decimal(TestLibrary.Generator.GetSingle(-55));
            Decimal expectValue = 0m;
            Decimal actualValue = Decimal.Truncate(m1);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.1", "Truncate should  return " + expectValue);
                retVal = false;
            }


            m1 = 123.456789m;
            expectValue = 123m;
            actualValue = Decimal.Truncate(m1);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.2", "Truncate should  return " + expectValue);
                retVal = false;
            }
      


            m1 = -123.456789m;
            expectValue = -123m;
            actualValue = Decimal.Truncate(m1);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.3", "Truncate should  return " + expectValue);
                retVal = false;
            }

            m1 = -9999999999.9999999999m;
            expectValue = -9999999999m;
            actualValue = Decimal.Truncate(m1);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.4", "Truncate should  return " + expectValue);
                retVal = false;
            }
            m1 = Decimal.MaxValue;
            expectValue = Decimal.MaxValue;
            actualValue = Decimal.Truncate(m1);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.5", "Truncate should  return " + expectValue);
                retVal = false;
            }

            m1 = Decimal.MinValue;
            expectValue = Decimal.MinValue;
            actualValue = Decimal.Truncate(m1);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.6", "Truncate should  return " + expectValue);
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
        DecimalTruncate test = new DecimalTruncate();

        TestLibrary.TestFramework.BeginTestCase("DecimalTruncate");

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
