// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
///System.IConvertible.ToDouble(System.IFormatProvider)
/// </summary>
public class SingleToDouble
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;
        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        return retVal;
    }

    #region Positive Test Cases
	//CultureInfo.GetCultureInfo has been removed. Replaced by CultureInfo ctor.
	public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check a  random single.");

        try
        {
            Single i1 = TestLibrary.Generator.GetSingle(-55);
            CultureInfo myCulture = new CultureInfo("en-us");
            double actualValue = ((IConvertible)i1).ToDouble(myCulture);
            if (actualValue.ToString("G9") != i1.ToString("G9"))
            {
                TestLibrary.TestFramework.LogError("001.1", "ToDouble  return failed. ");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Check a single which is  -.12300000339746475.");

        try
        {
            Single i1 = (Single)(-.12300000339746475);
            CultureInfo myCulture = new CultureInfo("en-us");
            Double actualValue = ((IConvertible)i1).ToDouble(myCulture);
            if (actualValue != (Double)(-.12300000339746475))
            {
                TestLibrary.TestFramework.LogError("002.1", "ToDouble  return failed. ");
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

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Check a single which is  +.12300000339746475.");

        try
        {
            Single i1 = (Single)(+.12300000339746475);
            CultureInfo myCulture = new CultureInfo("en-us");
            Double actualValue = ((IConvertible)i1).ToDouble(myCulture);
            if (actualValue != (Double)(+0.12300000339746475))
            {
                TestLibrary.TestFramework.LogError("003.1", "ToDouble  return failed. ");
                retVal = false;
            }

        }

        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #endregion

    public static int Main()
    {
        SingleToDouble test = new SingleToDouble();

        TestLibrary.TestFramework.BeginTestCase("SingleToDouble");

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
