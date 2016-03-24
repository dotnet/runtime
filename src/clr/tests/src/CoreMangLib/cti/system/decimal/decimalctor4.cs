// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Decimal.ctor(Int32[])
/// </summary>
public class DecimalCtor4
{

    public static int Main()
    {
        DecimalCtor4 dCtor4 = new DecimalCtor4();
        TestLibrary.TestFramework.BeginTestCase("for Constructor:System.Decimal.Ctor(Int32[])");

        if (dCtor4.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1:Verify the top 3 param are  random Int32... ";
        const string c_TEST_ID = "P001";

        int low = TestLibrary.Generator.GetInt32(-55);
        int mid = TestLibrary.Generator.GetInt32(-55);
        int hi = TestLibrary.Generator.GetInt32(-55);
        int flags = 0x1C0000;
        int[] arrInt ={low, mid, hi, flags};


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(arrInt);
            int[] arrBits = Decimal.GetBits(decimalValue);

            if (arrBits[0] != low)
            {
                string errorDesc = "lo Value is not " + low.ToString() + " as expected: param is " + arrInt[0].ToString();
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
            if (arrBits[1] != mid)
            {
                string errorDesc = "mid Value is not " + mid.ToString() + " as expected: param is " + arrInt[1].ToString();
                TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
            if (arrBits[2] != hi)
            {
                string errorDesc = "hi Value is not " + hi.ToString() + " as expected: param is " + arrInt[2].ToString();
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
            if (arrBits[3] != flags)
            {
                string errorDesc = "flags Value is not " + flags.ToString() + " as expected: param is " + arrInt[3].ToString();
                TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
            
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }

   
    #endregion

    #region Nagetive Testing
    public bool NegTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest1:Verify the param is a null reference";
        const string c_TEST_ID = "N001";

        int[] bits = null;

        
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);


        try
        {
            Decimal decimalValue = new Decimal(bits);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, "ArgumentNullException is not thrown as expected ." );
            retVal = false;
        }
        catch (ArgumentNullException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest2:the bits's length is not 4...";
        const string c_TEST_ID = "N002";

        int low = TestLibrary.Generator.GetInt32(-55);
        int mid = TestLibrary.Generator.GetInt32(-55);
        int hi = TestLibrary.Generator.GetInt32(-55);
        int flag = TestLibrary.Generator.GetInt32(-55);
        int[] bits ={ low, mid, hi, flag,low };


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);


        try
        {
            Decimal decimalValue = new Decimal(bits);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected ." );
            retVal = false;
        }
        catch (ArgumentException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest3:Verfiy the decimal value in bits is not valid.";
        const string c_TEST_ID = "N003";

        int low = TestLibrary.Generator.GetInt32(-55);
        int mid = TestLibrary.Generator.GetInt32(-55);
        int hi = TestLibrary.Generator.GetInt32(-55);
        int flag = 0x1C0001;
        int[] bits ={ low, mid, hi, flag};


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);


        try
        {
            Decimal decimalValue = new Decimal(bits);
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected .");
            retVal = false;
        }
        catch (ArgumentException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
