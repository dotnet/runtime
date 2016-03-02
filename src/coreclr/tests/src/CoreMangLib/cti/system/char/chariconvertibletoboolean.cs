// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Char.System.IConvertible.ToBoolean(IFormatProvider)
/// This method supports the .NET Framework infrastructure and is not intended to be used
/// directly from your code. 
/// Note: This conversion is not supported. Attempting to do so throws an InvalidCastException. 
/// </summary>
public class CharIConvertibleToBoolean
{
    public static int Main()
    {
        CharIConvertibleToBoolean testObj = new CharIConvertibleToBoolean();

        TestLibrary.TestFramework.BeginTestCase("for method: Char.System.IConvertible.ToBoolean(IFormatProvider)");
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

        char ch = TestLibrary.Generator.GetChar(-55);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            IConvertible converter = ch;
            converter.ToBoolean(null);

            errorDesc = "InvalidCastException is not thrown as expected.";
            errorDesc +=  string.Format("\nThe character is \\u{0:x}", (int)ch);
            TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (InvalidCastException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nThe character is \\u{0:x}", (int)ch);
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}

