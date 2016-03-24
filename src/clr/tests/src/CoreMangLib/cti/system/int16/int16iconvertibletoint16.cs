// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Int16.IConverible.ToInt16(IFormatProvider)
/// </summary>
public class Int16IConvertibleToInt16
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Check the int16.MaxValue ");

        try
        {
            Int16 i1 = Int16.MaxValue;
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToInt16(null) != Int16.MaxValue)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Check Int16.MinValue ");

        try
        {
            Int16 i1 = Int16.MinValue;
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToInt16(null) != i1)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Check zero ");

        try
        {
            Int16 i1 = 0;
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToInt16(null) != i1)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Check a random Int16 number ");

        try
        {
            Int16 i1 = TestLibrary.Generator.GetInt16(-55);
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToInt16(null) != i1)
            {
                TestLibrary.TestFramework.LogError("007", string.Format("The result is not the value {0} as expected", i1));
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

    #endregion
    #endregion

    public static int Main()
    {
        Int16IConvertibleToInt16 test = new Int16IConvertibleToInt16();

        TestLibrary.TestFramework.BeginTestCase("Int16IConvertibleToInt16");

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
