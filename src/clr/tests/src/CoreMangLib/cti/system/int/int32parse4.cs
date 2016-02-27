// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

//Parse(System.String,System.IFormatProvider)
public class Int32Parse4
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Test the IFormatInfo parameter.");

        try
        {
            string s1 = "  #1345";
            NumberFormatInfo n1 = new NumberFormatInfo();
            n1.NegativeSign = "#";
            int i1 = Int32.Parse(s1, n1);
            if (i1 != -1345)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Test the string with white space in both the beginning and the end");

        try
        {
            int i2 = TestLibrary.Generator.GetInt32(-55);
            string s1 = "      " + i2.ToString() + "    ";
            NumberFormatInfo n1 = new NumberFormatInfo();
            n1.NegativeSign = "#";
            int i1 = Int32.Parse(s1, n1);
            if (i1 != i2)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Using number as the negative sign");

        try
        {
            int i2 = TestLibrary.Generator.GetInt32(-55);
            string s1 = "  00" + i2;
            NumberFormatInfo n1 = new NumberFormatInfo();
            n1.NegativeSign = "00";
            int i1 = Int32.Parse(s1, n1);
            if (i1 != -i2)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Using null reference as the second parameter");

        try
        {
            int i2 = TestLibrary.Generator.GetInt32(-55);
            string s1 = i2.ToString();
            NumberFormatInfo n1 = null;
            int i1 = Int32.Parse(s1, n1);
            if (i1 != i2)
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Negative Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Set the parameter string to null reference ");

        try
        {
            String s1 = null;
            int i1 = Int32.Parse(s1, new NumberFormatInfo());
            TestLibrary.TestFramework.LogError("101", "The ArgumentNullException was not thrown as expected ");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Test FormatException ");

        string s1 = " (321)677 ";
        try
        {
            Int32 i1 = Int32.Parse(s1, new NumberFormatInfo());
            TestLibrary.TestFramework.LogError("103", "the Method did not throw a FormatException,patameter is: " + s1);
            retVal = false;
        }
        catch (System.FormatException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: Test OverflowException ");

        try
        {
            string s1 = "2147483650";
            Int32 i1 = Int32.Parse(s1, new NumberFormatInfo());
            TestLibrary.TestFramework.LogError("105", "the Method did not throw a FormatException,patameter is: " + s1);
            retVal = false;
        }
        catch (System.OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        Int32Parse4 test = new Int32Parse4();

        TestLibrary.TestFramework.BeginTestCase("Int32Parse4");

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
