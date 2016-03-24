// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Decimal.Compare(Decimal,Decimal)
/// </summary>
public class DecimalCompare
{

    public static int Main()
    {
        DecimalCompare dCompare = new DecimalCompare();
        TestLibrary.TestFramework.BeginTestCase("for Method:System.Decimal.DecimalCompare(Decimal,Decimal)");

        if (dCompare.RunTests())
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
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;

        return retVal;

    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1:Verify two decimal both are plus...";
        const string c_TEST_ID = "P001";

        Decimal dec1 = 12345.678m;
        Decimal dec2 = 87654.321m;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int result = Decimal.Compare(dec1, dec2);
            if (result>0)
            {
                string errorDesc = "Value is not  less than zero  as expected: Actual (" + result.ToString()+")";
                errorDesc += GetDataString(dec1, dec2);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(dec1, dec2));
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2:Verify two  decimal are  equal...";
        const string c_TEST_ID = "P002";

        Decimal dec1 = 623512345.678m;
        Decimal dec2 = 623512345.678m;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int result = Decimal.Compare(dec1, dec2); ;
            if (result != 0)
            {
                string errorDesc = "Value is not zero  as expected: Actual (" + result.ToString()+")";
                errorDesc += GetDataString(dec1, dec2);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(dec1, dec2));
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest3:Verify a  decimal is negative...";
        const string c_TEST_ID = "P003";

        Decimal dec1 = 87654.321234m;
        Decimal dec2 = -12345.678321m;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int result = Decimal.Compare(dec1, dec2); ;
            if (result < 0)
            {
                string errorDesc = "Value is not  greater than zero  as expected: Actual (" + result.ToString() + ")";
                errorDesc += GetDataString(dec1, dec2);
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(dec1, dec2));
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest4:Verify two decimal both are negative...";
        const string c_TEST_ID = "P004";

        Decimal dec1 = -87654.321234m;
        Decimal dec2 = -12345.678321m;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int result = Decimal.Compare(dec1, dec2); ;
            if (result >0)
            {
                string errorDesc = "Value is not less than zero  as expected: Actual (" + result.ToString() + ")";
                errorDesc += GetDataString(dec1, dec2);
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(dec1, dec2));
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest5:Verify a decimal is zero...";
        const string c_TEST_ID = "P005";

        Decimal dec1 = -87654.321234m;
        Decimal dec2 = 0m;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int result = Decimal.Compare(dec1, dec2);
            if (result > 0)
            {
                string errorDesc = "Value is not less than zero  as expected: Actual (" + result.ToString() + ")";
                errorDesc += GetDataString(dec1, dec2);
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(dec1, dec2));
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest6:Verify a decimal is Decimal.MinValue...";
        const string c_TEST_ID = "P006";

        Decimal dec1 = -87654.321234m;
        Decimal dec2 = Decimal.MinValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int result = Decimal.Compare(dec1, dec2);
            if (result < 0)
            {
                string errorDesc = "Value is not greater than zero  as expected: Actual (" + result.ToString() + ")";
                errorDesc += GetDataString(dec1, dec2);
                TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(dec1, dec2));
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest7:Verify a decimal is Decimal.MaxValue...";
        const string c_TEST_ID = "P007";

        Decimal dec1 = 87654.321234m;
        Decimal dec2 = Decimal.MaxValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int result = Decimal.Compare(dec1, dec2);
            if (result > 0)
            {
                string errorDesc = "Value is not less than zero  as expected: Actual (" + result.ToString() + ")";
                errorDesc += GetDataString(dec1, dec2);
                TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(dec1, dec2));
            retVal = false;
        }


        return retVal;
    }
    #endregion

    #region Helper methords for testing
    private string GetDataString(Decimal dec1, Decimal dec2)
    {
        string str;
        str = string.Format("\n[decimal1 value]\n  {0}", dec1.ToString());
        str += string.Format("\n[decimal2 value]\n {0}", dec2.ToString());

        return str;
    }
    #endregion
}