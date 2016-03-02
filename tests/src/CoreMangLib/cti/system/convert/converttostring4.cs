// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
using System.Runtime.CompilerServices;

/// <summary>
/// Convert.ToString(System.Byte,System.Int32)
/// </summary>
public class ConvertToString4
{
    const int flags  =0x40;
    const int width = -1;
    const char paddingChar = ' ';

    public static int Main()
    {
        ConvertToString4 testObj = new ConvertToString4();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToString(System.Byte,System.Int32)");
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest1: Verify value is 15 and radix is 2,8,10 or 16... ";
        string c_TEST_ID = "P001";


        Byte byteValue = 15;
        int  radix ;
        String actualValue;
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            actualValue = "1111";
            radix = 2;
            String resValue = Convert.ToString(byteValue, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(byteValue, radix);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            radix = 8;
            actualValue = "17";
            errorDesc = "";
            resValue = Convert.ToString(byteValue, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(byteValue, radix);
                TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            radix = 10;
            actualValue = "15";
            errorDesc = "";
            resValue = Convert.ToString(byteValue, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(byteValue, radix);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            radix = 16;
            actualValue = "f";
            errorDesc = "";
            resValue = Convert.ToString(byteValue, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(byteValue, radix);
                TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest2: Verify value is Byte.MaxValue and radix is 2,8,10 or 16... ";
        string c_TEST_ID = "P002";

        Byte byteValue = Byte.MaxValue;
        int radix;
        String actualValue;
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            actualValue = "11111111";
            radix = 2;
            String resValue = Convert.ToString(byteValue, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(byteValue, radix);
                TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            radix = 8;
            actualValue = "377";
            errorDesc = "";
            resValue = Convert.ToString(byteValue, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(byteValue, radix);
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            radix = 10;
            actualValue = "255";
            errorDesc = "";
            resValue = Convert.ToString(byteValue, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(byteValue, radix);
                TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            radix = 16;
            actualValue = "ff";
            errorDesc = "";
            resValue = Convert.ToString(byteValue, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(byteValue, radix);
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest3: Verify value is Byte.MinValue and radix is 2,8,10 or 16... ";
        string c_TEST_ID = "P003";

        Byte byteValue = Byte.MinValue;
        int[] radices ={ 2, 8, 10, 16 };
        String actualValue ="0";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            foreach (int radix in radices)
            {
                String resValue = Convert.ToString(byteValue, radix);
                if (actualValue != resValue)
                {
                    string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                    errorDesc += DataString(byteValue, radix);
                    TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
                    retVal = false;
                }
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "unexpected exception occurs :" + e );
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region NegativeTesting
    public bool NegTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest1: the radix is 32...";
        const string c_TEST_ID = "N001";

        Byte byteValue = TestLibrary.Generator.GetByte(-55);
        int  radix =32;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);


        
            try
            {
                Convert.ToString(byteValue, radix);
                TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected ." + DataString(byteValue,radix));
                retVal = false;
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + DataString(byteValue, radix));
                retVal = false;
            }
      
        return retVal;

    }
    #endregion

    #region Help Methods
    private string DataString(Byte byteValue, int radix)
    {
        string  str;

        str = string.Format("\n[byteValue value]\n \"{0}\"", byteValue);
        str += string.Format("\n[radix value ]\n {0}", radix);

        return str;
    }
    #endregion
}
