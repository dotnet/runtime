// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Resources;

///<summary>
///System.Resouces.SatelliteContractVersionAttribute.Version [v-zuolan]
///</summary>

public class SatelliteContractVersionAttributeVersion
{

    public static int Main()
    {
        SatelliteContractVersionAttributeVersion testObj = new SatelliteContractVersionAttributeVersion();
        TestLibrary.TestFramework.BeginTestCase("for property of System.Resouces.SatelliteContractVersionAttribute.Version");
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

        return retVal;
    }


    #region Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        String version = TestLibrary.Generator.GetString(-55, false, 1, 255);
        SatelliteContractVersionAttribute sCVA = new SatelliteContractVersionAttribute(version);

        String expectedValue = version;
        String actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Set Version as normal String and get it.");
        try
        {    
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
        SatelliteContractVersionAttribute sCVA = new SatelliteContractVersionAttribute(version);

        String expectedValue = version;
        String actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Set Version as long String and get it.");
        try
        {
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

}
