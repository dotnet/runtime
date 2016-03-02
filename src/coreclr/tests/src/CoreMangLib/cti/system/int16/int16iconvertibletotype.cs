// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// System.Int16.System.IConvertible.ToType(System.Type,System.IFormatProvider)
/// </summary>
public class Int16IConvertibleToType
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a random Int16 to String ");

        try
        {
            Int16 i1 = TestLibrary.Generator.GetInt16(-55);
            IConvertible Icon1 = (IConvertible)i1;
            CultureInfo cultureinfo = new CultureInfo("en-US");
            string s1 = Icon1.ToType(typeof(System.String), cultureinfo) as string;
            if (s1 != i1.ToString())
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected.The random number is :" + i1.ToString());
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

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert a random Int16 to Boolean ");

        try
        {
            Int16 i1 = 0;
            while (i1 == 0)
            {
                i1 = TestLibrary.Generator.GetInt16(-55);
            }
            IConvertible Icon1 = (IConvertible)i1;
            CultureInfo cultureinfo = new CultureInfo("en-US");
            bool s1 = (bool)Icon1.ToType(typeof(System.Boolean), cultureinfo);
            if (s1 != true)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected.The random number is :" + i1.ToString());
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

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest3: Check the boundary value ");

        try
        {
            Int16 i1 = Int16.MaxValue;
            IConvertible Icon1 = (IConvertible)i1;
            CultureInfo cultureinfo = new CultureInfo("en-US");
            long s1 = (long)Icon1.ToType(typeof(System.Int64), cultureinfo);
            if (s1 != i1)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected.");
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

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest4: Convert \'-0\' to char ");

        try
        {
            Int16 i1 = -0;
            IConvertible Icon1 = (IConvertible)i1;
            char s1 = (char)Icon1.ToType(typeof(System.Char), null);
            if (s1 != '\0')
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected.");
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

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Test the overflowException ");

        try
        {
            Int16 i1 = 0;
            while (i1 <= 255)
            {
                i1 = TestLibrary.Generator.GetInt16(-55);
            }
            IConvertible Icon1 = (IConvertible)i1;
            byte s1 = (byte)Icon1.ToType(typeof(System.Byte), null);
            TestLibrary.TestFramework.LogError("101", "The overflow exception was not thrown as expected: ");
            retVal = false;
        }
        catch (OverflowException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: Convert an int16 to a custom class type ");

        try
        {
            Int16 i1 = TestLibrary.Generator.GetInt16(-55);
            IConvertible Icon1 = (IConvertible)i1;
            CultureInfo cultureinfo = new CultureInfo("en-US");
            TestClass testclass = Icon1.ToType(typeof(TestClass), cultureinfo) as TestClass;
            TestLibrary.TestFramework.LogError("103", "The InvalidCastException was not thrown as expected: " + i1);
            retVal = false;
        }
        catch (InvalidCastException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        Int16IConvertibleToType test = new Int16IConvertibleToType();

        TestLibrary.TestFramework.BeginTestCase("Int16IConvertibleToType");

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
public class TestClass
{
}

