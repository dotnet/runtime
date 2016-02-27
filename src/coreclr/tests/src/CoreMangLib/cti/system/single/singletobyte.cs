// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
///System.IConvertible.ToByte(System.IFormatProvider)
/// </summary>
public class SingleToByte
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        return retVal;
    }

    #region Positive Test Cases
	//CultureInfo.GetCultureInfo has been removed. Replaced by CultureInfo ctor.
	public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check a random single.");

        try
        {
            Single i1 = TestLibrary.Generator.GetSingle(-55);
            CultureInfo myCulture = new CultureInfo("en-us");
            byte expectVaule;
            if (i1 > 0.5)
                expectVaule = 1;
            else
                expectVaule = 0;
            byte actualValue = ((IConvertible)i1).ToByte(myCulture);
            if (actualValue != expectVaule)
            {
                TestLibrary.TestFramework.LogError("001.1", "ToByte  return failed. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }


    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Check a single which is  -.123.");

        try
        {
            Single i1 = (Single)(-.123);
            CultureInfo myCulture = new CultureInfo("en-us");
            byte actualValue = ((IConvertible)i1).ToByte(myCulture);
            if (actualValue != 0)
            {
                TestLibrary.TestFramework.LogError("002.1", "ToByte return failed. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }


    #endregion

    #endregion

    public static int Main()
    {
        SingleToByte test = new SingleToByte();

        TestLibrary.TestFramework.BeginTestCase("SingleToByte");

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
