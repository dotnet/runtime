// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
///System.IConvertible.ToSingle(System.IFormatProvider)
/// </summary>
public class DecimalToSingle
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;
        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check a  random single.");

        try
        {
            Decimal i1 = new decimal(TestLibrary.Generator.GetSingle(-55));

            CultureInfo myCulture = CultureInfo.InvariantCulture;
            Single actualValue = ((IConvertible)i1).ToSingle(myCulture);
            Single expectValue = (Single)i1;
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.1", "ToSingle should return " + expectValue);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Check a single which is  -9999.999.");

        try
        {
            Decimal i1 = -9999.999m;
            CultureInfo myCulture = CultureInfo.InvariantCulture;
            Single actualValue = ((IConvertible)i1).ToSingle(myCulture);
            Single expectValue = -9999.999f;
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("002.1", "ToSingle should return " + expectValue);
                retVal = false;
            }

        }

        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

   
    #endregion

    #endregion

    public static int Main()
    {
        DecimalToSingle test = new DecimalToSingle();

        TestLibrary.TestFramework.BeginTestCase("DecimalToSingle");

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
