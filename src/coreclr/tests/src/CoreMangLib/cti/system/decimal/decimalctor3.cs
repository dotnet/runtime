// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Decimal.ctor(Int32,Int32,Int32,Boolean,Byte)
/// </summary>
public class DecimalCtor3
{

    public static int Main()
    {
        DecimalCtor3 dCtor3 = new DecimalCtor3();
        TestLibrary.TestFramework.BeginTestCase("for Constructor:System.Decimal.Ctor(Int32,Int32,Int32,Boolean,Byte)");

        if (dCtor3.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1:Verify the isNagetive is true... ";
        const string c_TEST_ID = "P001";

        int low = TestLibrary.Generator.GetInt32(-55);
        int mid = TestLibrary.Generator.GetInt32(-55);
        int hi = TestLibrary.Generator.GetInt32(-55);
        bool isNagetive = true;
        Byte scale = TestLibrary.Generator.GetByte(-55);

        while (scale > 28)
        {
            scale = TestLibrary.Generator.GetByte(-55);
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(low, mid, hi, isNagetive, scale);
            int[]   arrInt = Decimal.GetBits(decimalValue);

            if (arrInt[0] != low)
            {
                string errorDesc = "lo Value is not " +low.ToString() +" as expected: param is " + arrInt[0].ToString();
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
            if (arrInt[1] != mid)
            {
                string errorDesc = "mid Value is not " + mid.ToString() + " as expected: param is " + arrInt[1].ToString();
                TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
            if (arrInt[2] != hi)
            {
                string errorDesc = "hi Value is not " + hi.ToString() + " as expected: param is " + arrInt[2].ToString();
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (isNagetive)
            {
                if (decimalValue > 0m)
                {
                    string errorDesc = "created decimal object should  is less than 0 ";
                    TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
                    retVal = false;
                }

            }
            else
            {
                if (decimalValue < 0m)
                {
                    string errorDesc = "created decimal object should  is larger than 0 ";
                    TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                    retVal = false;
                }
            }

            string subLast;
            int resScale;
            int index = decimalValue.ToString().IndexOf(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            if (index != -1)
            {
                subLast = decimalValue.ToString().Substring(index);
                resScale = subLast.Length - 1;
            }
            else
            {
                resScale = 0;
            }

            if (Convert.ToInt64(scale) != resScale)
            {
                string errorDesc = "scale Value is not " + scale.ToString() + " as expected: actual<" + resScale.ToString() + ">";
                TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2:Verify the isNagetive is false... ";
        const string c_TEST_ID = "P002";

        int low = TestLibrary.Generator.GetInt32(-55);
        int mid = TestLibrary.Generator.GetInt32(-55);
        int hi = TestLibrary.Generator.GetInt32(-55);
        bool isNagetive = false;
        Byte scale = TestLibrary.Generator.GetByte(-55);

        while (scale > 28)
        {
            scale = TestLibrary.Generator.GetByte(-55);
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(low, mid, hi, isNagetive, scale);
            int[] arrInt = Decimal.GetBits(decimalValue);

            if (arrInt[0] != low)
            {
                string errorDesc = "lo Value is not " + low.ToString() + " as expected: param is " + arrInt[0].ToString();
                TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
            if (arrInt[1] != mid)
            {
                string errorDesc = "mid Value is not " + mid.ToString() + " as expected: param is " + arrInt[1].ToString();
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
            if (arrInt[2] != hi)
            {
                string errorDesc = "hi Value is not " + hi.ToString() + " as expected: param is " + arrInt[2].ToString();
                TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (isNagetive)
            {
                if (decimalValue > 0m)
                {
                    string errorDesc = "created decimal object should  is less than 0 ";
                    TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
                    retVal = false;
                }

            }
            else
            {
                if (decimalValue < 0m)
                {
                    string errorDesc = "created decimal object should  is larger than 0 ";
                    TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, errorDesc);
                    retVal = false;
                }
            }

            string subLast;
            int resScale;
            int index = decimalValue.ToString().IndexOf(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            if (index != -1)
            {
                subLast = decimalValue.ToString().Substring(index);
                resScale = subLast.Length - 1;
            }
            else 
            {
                resScale=0;
            }
            

            if (Convert.ToInt64(scale) != resScale)
            {
                string errorDesc = "scale Value is not " + scale.ToString() + " as expected: actual<" + resScale.ToString() + ">";
                TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }
    #endregion

    #region Nagetive Testing
    public bool NegTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest1:Verify the scale is larger than 28";
        const string c_TEST_ID = "N001";

        int low = TestLibrary.Generator.GetInt32(-55);
        int mid = TestLibrary.Generator.GetInt32(-55);
        int hi = TestLibrary.Generator.GetInt32(-55);
        bool isNagetive = false;
        Byte scale = TestLibrary.Generator.GetByte(-55);

        while (scale < 28)
        {
            scale = TestLibrary.Generator.GetByte(-55);
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);


        try
        {
            Decimal decimalValue = new Decimal(low,mid,hi,isNagetive,scale);
            TestLibrary.TestFramework.LogError("015" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected ." + "\n scale value is " + scale.ToString());
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    
    #endregion
}

