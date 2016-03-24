// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
///System.IConvertible.ToUInt32(System.IFormatProvider)
/// </summary>
public class SingleToUInt32
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;
        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
   
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
            short expectValue = 0;
            if (i1 > 0.5)
                expectValue = 1;
            else
                expectValue = 0;
            CultureInfo myCulture =  new CultureInfo("en-US");
            uint actualValue = ((IConvertible)i1).ToUInt32(myCulture);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.1", "ToUInt32  return failed. ");
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

   
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Check a single which is  >UInt32.MaxValue.");

        try
        {
            Single i1 = (float)UInt32.MaxValue + 1.0f;
            CultureInfo myCulture =  new CultureInfo("en-US");
            uint actualValue = ((IConvertible)i1).ToUInt32(myCulture);
            TestLibrary.TestFramework.LogError("101.1", "ToUInt32  return failed. ");
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: Check a single which is  <UInt32.MinValue.");

        try
        {
            Single i1 = (float)UInt32.MinValue - 1.0f;
            CultureInfo myCulture =  new CultureInfo("en-US");
            uint actualValue = ((IConvertible)i1).ToUInt32(myCulture);
            TestLibrary.TestFramework.LogError("102.1", "ToUInt32  return failed. ");
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
        SingleToUInt32 test = new SingleToUInt32();

        TestLibrary.TestFramework.BeginTestCase("SingleToUInt32");

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
