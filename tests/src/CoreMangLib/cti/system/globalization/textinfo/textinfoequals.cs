// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// System.Globalization.TextInfo.Equals(Object)
/// </summary>
public class TextInfoEquals
{

    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;

    public static int Main()
    {
        TextInfoEquals testObj = new TextInfoEquals();
        TestLibrary.TestFramework.BeginTestCase("for Method:System.Globalization.TextInfo.Equals(Object)");

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
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Verify same culture TextInfo equals original TextInfo. ";
        const string c_TEST_ID = "P001";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        CultureInfo ci = new CultureInfo("en-US");
        CultureInfo ci2 = new CultureInfo("en-US");
        object textInfo = ci2.TextInfo;

        try
        {
            
            if (!ci.TextInfo.Equals(textInfo))
            {
                string errorDesc = "the second TextInfo should equal original TextInfo.";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2: Verify the TextInfo is not same CultureInfo's . ";
        const string c_TEST_ID = "P002";


       TextInfo textInfoFrance = new CultureInfo("fr-FR").TextInfo;
       TextInfo textInfoUS = new CultureInfo("en-US").TextInfo;

       TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (textInfoFrance.Equals((object)textInfoUS))
            {
                string errorDesc = "the TextInfos of differente CultureInfo should not equal. ";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
           
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest3: Verify the TextInfo not equal a null reference . ";
        const string c_TEST_ID = "P003";


        TextInfo textInfoUS = new CultureInfo("en-US").TextInfo;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (textInfoUS.Equals(null))
            {
                string errorDesc = "the US CultureInfo's TextInfo should not equal a null reference. ";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest4: Verify the TextInfo not equal another type object . ";
        const string c_TEST_ID = "P004";


        TextInfo textInfoUS = new CultureInfo("en-US").TextInfo;
        object obj = (object)(new MyClass());
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (textInfoUS.Equals(obj))
            {
                string errorDesc = "the US CultureInfo's TextInfo should not equal user-defined type object. ";
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest5: Verify the TextInfo not equal a int object . ";
        const string c_TEST_ID = "P005";


        TextInfo textInfoUS = new CultureInfo("en-US").TextInfo;
        int i = TestLibrary.Generator.GetInt32(-55);
        object intObject = i as object;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (textInfoUS.Equals(intObject))
            {
                string errorDesc = "the US CultureInfo's TextInfo should not equal int object. ";
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest6: Verify the TextInfo not equal a string object . ";
        const string c_TEST_ID = "P006";


        TextInfo textInfoUS = new CultureInfo("en-US").TextInfo;
        String str = TestLibrary.Generator.GetString(-55, false,c_MINI_STRING_LENGTH,c_MAX_STRING_LENGTH);
        object strObject = str as object;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (textInfoUS.Equals(strObject))
            {
                string errorDesc = "the US CultureInfo's TextInfo should not equal string object. ";
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Customer Class
    public class MyClass
    {
 
    }
    #endregion
}

