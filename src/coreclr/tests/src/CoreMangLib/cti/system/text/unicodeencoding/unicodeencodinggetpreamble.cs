// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

///<summary>
///System.Text.UnicodeEncoding.GetPreamble() [v-zuolan]
///</summary>

public class UnicodeEncodingGetPreamble
{

    public static int Main()
    {
        UnicodeEncodingGetPreamble testObj = new UnicodeEncodingGetPreamble();
        TestLibrary.TestFramework.BeginTestCase("for method of System.Text.UnicodeEncoding.GetPreamble");
        if (testObj.RunTests())
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
        TestLibrary.TestFramework.LogInformation("Positive");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }


    #region Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        UnicodeEncoding uE = new UnicodeEncoding(true,false);

        Byte[] expectedValue = new Byte[] { };
        Byte[] actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the method with bigEndian true and byteOrderMark false.");
        try
        {
            actualValue = uE.GetPreamble();

            if (expectedValue.Equals(actualValue))
            {
                TestLibrary.TestFramework.LogError("001", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        UnicodeEncoding uE = new UnicodeEncoding(true, true);

        Byte[] expectedValue = new Byte[] { 0xfe,0xff};
        Byte[] actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2:Invoke the method with bigEndian true and byteOrderMark true.");
        try
        {
            actualValue = uE.GetPreamble();

            if (expectedValue.Equals(actualValue))
            {
                TestLibrary.TestFramework.LogError("003", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        UnicodeEncoding uE = new UnicodeEncoding(false, true);

        Byte[] expectedValue = new Byte[] { 0xff, 0xfe };
        Byte[] actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest3:Invoke the method with bigEndian false and byteOrderMark true.");
        try
        {
            actualValue = uE.GetPreamble();

            if (expectedValue.Equals(actualValue))
            {
                TestLibrary.TestFramework.LogError("005", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
