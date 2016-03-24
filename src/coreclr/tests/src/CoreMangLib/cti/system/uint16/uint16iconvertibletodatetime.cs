// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// UInt16.System.IConvertible.ToDateTime(IFormatProvider)
/// Note   This conversion is not supported. Attempting to do so throws an InvalidCastException. 
/// </summary>
public class UInt16IConvertibleToDateTime
{
    public static int Main()
    {
        UInt16IConvertibleToDateTime testObj = new UInt16IConvertibleToDateTime();

        TestLibrary.TestFramework.BeginTestCase("for method: UInt16.System.IConvertible.ToDateTime(IFormatProvider)");
        if(testObj.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Negative tests
    //InvalidCastException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "N001";
        const string c_TEST_DESC = "NegTest1: Random character";
        string errorDesc;

        UInt16 i = (UInt16)(TestLibrary.Generator.GetInt32(-55) % (UInt16.MaxValue + 1));
        IConvertible convert;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            convert = (IConvertible)i;
            convert.ToDateTime(null);

            errorDesc = "InvalidCastException is not thrown as expected.";
            errorDesc += string.Format("\nThe UInt16 value is {0}", i);
            TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (InvalidCastException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nThe character is {0}", i);
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}

