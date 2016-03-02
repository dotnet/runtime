// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// UInt64.System.IConvertible.ToDateTime
/// </summary>
public class UInt64IConvertibleToDateTime
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Convert an random UInt64 to DateTime ");

        try
        {
            UInt64 u64 = (ulong)TestLibrary.Generator.GetInt64(-55);
            IConvertible Icon1 = (IConvertible)u64;
            DateTime d1 = Icon1.ToDateTime(null);
            TestLibrary.TestFramework.LogError("101", "The InvalidCastException was not thrown as expected");
            retVal = false;

        }
        catch (System.InvalidCastException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: Convert zero to DateTime ");

        try
        {
            UInt64 u64 = 0;
            DateTime d1 = (u64 as IConvertible).ToDateTime(null);
            TestLibrary.TestFramework.LogError("103", "The InvalidCastException was not thrown as expected");
            retVal = false;

        }
        catch (System.InvalidCastException)
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
        UInt64IConvertibleToDateTime test = new UInt64IConvertibleToDateTime();

        TestLibrary.TestFramework.BeginTestCase("UInt64IConvertibleToDateTime");

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
    #region ForTestObject
    private UInt64 GetInt64(UInt64 minValue, UInt64 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + (UInt64)TestLibrary.Generator.GetInt64(-55) % (maxValue - minValue);
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }
    #endregion
}
