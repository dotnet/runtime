// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
///System.IConvertible.ToSingle(System.IFormatProvider)
/// </summary>
public class SingleToSingle
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check a  random single.");

        try
        {
            Single i1 = TestLibrary.Generator.GetSingle(-55);
          
            CultureInfo myCulture =  new CultureInfo("en-US");
            Single actualValue = ((IConvertible)i1).ToSingle(myCulture);
            if (actualValue != i1)
            {
                TestLibrary.TestFramework.LogError("001.1", "ToSingle  return failed. ");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Check a single which is  -123.");

        try
        {
            Single i1 = (Single)(-123);
            CultureInfo myCulture =  new CultureInfo("en-US");
            Single actualValue = ((IConvertible)i1).ToSingle(myCulture);
            if (actualValue != i1)
            {
                TestLibrary.TestFramework.LogError("002.1", "ToSingle  return failed. ");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Check a single which is  +123.");

        try
        {
            Single i1 = (Single)(+123);
            CultureInfo myCulture =  new CultureInfo("en-US");
            Single actualValue = ((IConvertible)i1).ToSingle(myCulture);
            if (actualValue != i1)
            {
                TestLibrary.TestFramework.LogError("003.1", "ToSingle  return failed. ");
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

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: check a single  which is  not a number .");

        try
        {
            Single i1 = Single.NaN;
            CultureInfo myCulture =  new CultureInfo("en-US");
            Single actualValue = ((IConvertible)i1).ToSingle(myCulture);
            if (!Single.IsNaN(actualValue))
            {
                TestLibrary.TestFramework.LogError("004.1", "ToSingle  return failed. ");
                retVal = false;
            }
            i1 = Single.NegativeInfinity;
            myCulture =  new CultureInfo("en-US");
            actualValue = ((IConvertible)i1).ToSingle(myCulture);
            if (!Single.IsNegativeInfinity(actualValue))
            {
                TestLibrary.TestFramework.LogError("004.2", "ToSingle  return failed. ");
                retVal = false;
            }
            i1 = Single.PositiveInfinity;
            myCulture =  new CultureInfo("en-US");
            actualValue = ((IConvertible)i1).ToSingle(myCulture);
            if (!Single.IsPositiveInfinity(actualValue))
            {
                TestLibrary.TestFramework.LogError("004.3", "ToSingle  return failed. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #endregion

    public static int Main()
    {
        SingleToSingle test = new SingleToSingle();

        TestLibrary.TestFramework.BeginTestCase("SingleToSingle");

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
