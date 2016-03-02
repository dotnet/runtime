// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.IConvertible.ToDateTime(System.IFormatProvider)
/// </summary>

public class DoubleIConvertibleToDateTime
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;
        try
        {
            Double d = TestLibrary.Generator.GetDouble(-55);
            DateTime v = (d as IConvertible).ToDateTime(null);
            TestLibrary.TestFramework.LogError("001",
                String.Format("expected a InvalidCastException on (d as IConvertible).ToDateTime(null)) but got {0}", v));
            retVal = false;
        }
        catch (InvalidCastException)
        {
            retVal = true;
            return retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    #endregion
    #endregion

    public static int Main()
    {
        DoubleIConvertibleToDateTime test = new DoubleIConvertibleToDateTime();

        TestLibrary.TestFramework.BeginTestCase("DoubleIConvertibleToDateTime");

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
