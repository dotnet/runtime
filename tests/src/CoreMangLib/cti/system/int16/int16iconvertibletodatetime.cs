// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Int16.IConvertible.ToDateTime(IFormatProvider)
/// </summary>
public class Int16IConvertibleToDateTime
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: Convert a random int16 number to dateTime");

        try
        {
            Int16 i1 = TestLibrary.Generator.GetInt16(-55);
            DateTime datetime = (i1 as IConvertible).ToDateTime(null);
            TestLibrary.TestFramework.LogError("101", "The InvalidCastException was not thrown as expected");
            retVal = false;
        }
        catch (InvalidCastException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: Convert a zero to dateTime");

        try
        {
            Int16 i1 = 0;
            DateTime datetime = (i1 as IConvertible).ToDateTime(null);
            TestLibrary.TestFramework.LogError("103", "The InvalidCastException was not thrown as expected");
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
        Int16IConvertibleToDateTime test = new Int16IConvertibleToDateTime();

        TestLibrary.TestFramework.BeginTestCase("Int16IConvertibleToDateTime");

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
