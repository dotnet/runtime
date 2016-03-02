// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// Negate(System.Decimal)
/// </summary>
public class DecimalNegate
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Calling Negate method.");

        try
        {
            Decimal m1 = new decimal(TestLibrary.Generator.GetDouble(-55));
            Decimal expectValue = -m1;
            Decimal actualValue = Decimal.Negate(m1);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.1", "Negate should  return " + expectValue);
                retVal = false;
            }


            m1 = new decimal(TestLibrary.Generator.GetInt32(-55));
            expectValue = -m1;
            actualValue = Decimal.Negate(m1);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.2", "Negate should  return " + expectValue);
                retVal = false;
            }

            m1 = new decimal(TestLibrary.Generator.GetSingle(-55));
            expectValue = -m1;
            actualValue = Decimal.Negate(m1);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.3", "Negate should  return " + expectValue);
                retVal = false;
            }

            m1 = -1000m;
            expectValue =1000m;
            actualValue = Decimal.Negate(m1);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.4", "Negate should  return " + expectValue);
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
        DecimalNegate test = new DecimalNegate();

        TestLibrary.TestFramework.BeginTestCase("DecimalNegate");

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
