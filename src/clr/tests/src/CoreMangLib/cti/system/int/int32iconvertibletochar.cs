// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class Int32IConvertibleToChar
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a random (Int32)char to char ");

        try
        {
            Int32 i1 = (Int32)TestLibrary.Generator.GetChar(-55);
            IConvertible Icon1 = (IConvertible)i1;
            char c1 = Icon1.ToChar(null);
            if (c1 != i1)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected ");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert zero to char ");

        try
        {
            Int32 i1 = 0;
            IConvertible Icon1 = (IConvertible)i1;
            char c1 = Icon1.ToChar(null);
            if (c1 != i1)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected ");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Check the border value of char ");

        try
        {
            Int32 i1 = 65535;
            IConvertible Icon1 = (IConvertible)i1;
            char c1 = Icon1.ToChar(null);
            if (c1 != i1)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected ");
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Convert a negative int32 to char");

        try
        {
            Int32 i1 = (Int32)TestLibrary.Generator.GetChar(-55);
            IConvertible Icon1 = (IConvertible)(-i1);
            char c1 = Icon1.ToChar(null);
            TestLibrary.TestFramework.LogError("101", "OverflowException was not thrown as expected  ");
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: Convert a int32 which is bigger than char to char ");

        try
        {
            Int32 i1 = 65536;
            IConvertible Icon1 = (IConvertible)i1;
            char c1 = Icon1.ToChar(null);
            TestLibrary.TestFramework.LogError("103", "OverflowException was not thrown as expected  ");
            retVal = false;
        }
        catch (OverflowException)
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
        Int32IConvertibleToChar test = new Int32IConvertibleToChar();

        TestLibrary.TestFramework.BeginTestCase("Int32IConvertibleToChar");

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
