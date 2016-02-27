// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Resources;

///<summary>
///System.Resouces.SatelliteContractVersionAttribute.Ctor(String) [v-zuolan]
///</summary>

public class SatelliteContractVersionAttributeCtor
{

    public static int Main()
    {
        SatelliteContractVersionAttributeCtor testObj = new SatelliteContractVersionAttributeCtor();
        TestLibrary.TestFramework.BeginTestCase("for Constructor of System.Resouces.SatelliteContractVersionAttribute");
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

        TestLibrary.TestFramework.LogInformation("Negative");
        retVal = NegTest1() && retVal;

        return retVal;
    }


    #region Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        String version = TestLibrary.Generator.GetString(-55, false, 1, 255);

        String expectedValue = version;
        String actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Create a instance with normal string");
        try
        {
            SatelliteContractVersionAttribute sCVA = new SatelliteContractVersionAttribute(version);
            actualValue = sCVA.Version;
            if (expectedValue != actualValue)
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

        String version = TestLibrary.Generator.GetString(-55, false, 256, 512);

        String expectedValue = version;
        String actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Create a instance with long string");
        try
        {
            SatelliteContractVersionAttribute sCVA = new SatelliteContractVersionAttribute(version);
            actualValue = sCVA.Version;
            if (expectedValue != actualValue)
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
    #endregion

    #region Negative Test Logic
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1:Create a instance with null string");
        try
        {
            SatelliteContractVersionAttribute sCVA = new SatelliteContractVersionAttribute(null);
            TestLibrary.TestFramework.LogError("005", "No ArgumentNullException throw out expected.");
            retVal = false;
        }
        catch (ArgumentNullException)
        {

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
