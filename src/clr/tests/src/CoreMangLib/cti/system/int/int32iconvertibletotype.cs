// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

//System.Int32.System.IConvertible.ToType(System.Type,System.IFormatProvider)
public class Int32IConvertibleToType
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a random Int32 to String ");

        try
        {
            Int32 i1 = TestLibrary.Generator.GetInt32(-55);
            IConvertible Icon1 = (IConvertible)i1;
            string s1 = Icon1.ToType(typeof(System.String), null) as string;
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert a random Int32 to Boolean ");

        try
        {
            Int32 i1 = 0;
            while (i1 == 0)
            {
                i1 = TestLibrary.Generator.GetInt32(-55);
            }
            IConvertible Icon1 = (IConvertible)i1;
            bool s1 = (bool)Icon1.ToType(typeof(System.Boolean), null);
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
            Int32 i1 = Int32.MaxValue;
            IConvertible Icon1 = (IConvertible)i1;
            long s1 = (long)Icon1.ToType(typeof(System.Int64), null);
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
            Int32 i1 = -0;
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
            Int32 i1 = 0;
            while (i1 <= 255)
            {
                i1 = TestLibrary.Generator.GetInt32(-55);
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
    #endregion
    #endregion

    public static int Main()
    {
        Int32IConvertibleToType test = new Int32IConvertibleToType();

        TestLibrary.TestFramework.BeginTestCase("Int32IConvertibleToType");

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

