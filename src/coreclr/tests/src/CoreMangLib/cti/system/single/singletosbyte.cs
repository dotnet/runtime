// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
///System.IConvertible.ToSByte(System.IFormatProvider)
/// </summary>
public class SingleToSByte
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;
        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        TestLibrary.TestFramework.LogInformation("[Negtive]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
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
            int expectValue = 0;
            if (i1 > 0.5)
                expectValue = 1;
            else
                expectValue = 0;
            CultureInfo myCulture =  new CultureInfo("en-US");
            sbyte actualValue = ((IConvertible)i1).ToSByte(myCulture);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.1", "ToSByte  return failed. ");
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
            sbyte actualValue = ((IConvertible)i1).ToSByte(myCulture);
            if (actualValue != (sbyte)(-123))
            {
                TestLibrary.TestFramework.LogError("002.1", "ToSByte  return failed. ");
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
            sbyte actualValue = ((IConvertible)i1).ToSByte(myCulture);
            if (actualValue != (sbyte)(+123))
            {
                TestLibrary.TestFramework.LogError("003.1", "ToSByte  return failed. ");
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
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Check a single which is  >SByte.MaxValue.");

        try
        {

            Single i1 = (float)SByte.MaxValue + 1.0f;
            CultureInfo myCulture =  new CultureInfo("en-US");
            sbyte actualValue = ((IConvertible)i1).ToSByte(myCulture);
            TestLibrary.TestFramework.LogError("101.1", "ToSByte  return failed. ");
            retVal = false;


        }
        catch (OverflowException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Check a single which is  <SByte.MinValue.");

        try
        {
            Single i1 = (float)SByte.MinValue - 1.0f;
            CultureInfo myCulture =  new CultureInfo("en-US");
            sbyte actualValue = ((IConvertible)i1).ToSByte(myCulture);
            TestLibrary.TestFramework.LogError("102.1", "ToSByte  return failed. ");
            retVal = false;


        }
        catch (OverflowException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #endregion

    public static int Main()
    {
        SingleToSByte test = new SingleToSByte();

        TestLibrary.TestFramework.BeginTestCase("SingleToSByte");

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
