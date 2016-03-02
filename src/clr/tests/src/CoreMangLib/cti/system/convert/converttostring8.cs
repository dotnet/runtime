// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// Convert.ToString(System.DateTime,System.IFormatProvider)
/// </summary>
public class ConvertToString8
{
    public static int Main()
    {
        ConvertToString8 testObj = new ConvertToString8();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToString(System.DateTime,System.IFormatProvider)");
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
        //ur-PK doesn't exist in telesto 
        // retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest1: Verify the DateTime is now and IFormatProvider is en-US CultureInfo... ";
        string c_TEST_ID = "P001";


        DateTime dt = DateTime.Now;
        IFormatProvider provider = new CultureInfo("en-US");
        String actualValue = dt.ToString(provider);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(dt,provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc = "\n IFormatProvider is en-US CultureInfo.";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest2: Verify the DateTime is now and IFormatProvider is fr-FR CultureInfo... ";
        string c_TEST_ID = "P002";


        DateTime dt = DateTime.Now;
        IFormatProvider provider = new CultureInfo("fr-FR");
        String actualValue = dt.ToString(provider);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(dt,provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc = "\n IFormatProvider is fr-FR CultureInfo.";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest3: Verify DateTime instance is created by ctor(int year,int month,int day) and IFormatProvider is ur-PK... ";
        string c_TEST_ID = "P003";

        Random rand = new Random(-55);
        int year = rand.Next(1900, 2050);
        int month = rand.Next(1, 12);
        int day = rand.Next(1, 28);
        DateTime dt = new DateTime(year, month, day);

        IFormatProvider provider = new CultureInfo("ur-PK");
        String actualValue = dt.ToString(provider);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(dt,provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc = "\n IFormatProvider is ur-PK CultureInfo.";
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest4: Verify DateTime instance is created by ctor(int year,int month,int day,int hour,int minute,int second) and IFormatProvider is ru-RU CultureInfo...... ";
        string c_TEST_ID = "P004";


        Random rand = new Random(-55);
        int year = rand.Next(1900, 2050);
        int month = rand.Next(1, 12);
        int day = rand.Next(1, 28);
        int hour = rand.Next(0, 23);
        int minute = rand.Next(0, 59);
        int second = rand.Next(0, 59);
        DateTime dt = new DateTime(year, month, day);

        IFormatProvider provider = new CultureInfo("ru-RU");
        String actualValue = dt.ToString(provider);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(dt,provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc = "\n IFormatProvider is ur-PK CultureInfo.";
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest5: Verify DateTime instance is created by ctor(int year,int month,int day,int hour,int minute,int second) and IFormatProvider is a null reference... ";
        string c_TEST_ID = "P005";


        Random rand = new Random(-55);
        int year = rand.Next(1900, 2050);
        int month = rand.Next(1, 12);
        int day = rand.Next(1, 28);
        int hour = rand.Next(0, 23);
        int minute = rand.Next(0, 59);
        int second = rand.Next(0, 59);
        DateTime dt = new DateTime(year, month, day);

        IFormatProvider provider = null;
        String actualValue = dt.ToString(provider);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(dt, provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc = "\n IFormatProvider is a null reference.";
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    #endregion
}
