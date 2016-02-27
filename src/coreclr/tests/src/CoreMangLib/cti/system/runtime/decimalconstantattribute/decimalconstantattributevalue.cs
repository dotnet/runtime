// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Value
/// </summary>
public class DecimalConstantAttributeValue
{


    public static int Main(string[] args)
    {
        DecimalConstantAttributeValue DecimalConstantAttributeValue = new DecimalConstantAttributeValue();
        TestLibrary.TestFramework.BeginTestCase("DecimalConstantAttributeValue");

        if (DecimalConstantAttributeValue.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Get the DecimalConstantAttribute Value.");

        try
        {
            DecimalConstantAttribute myDAttribute = new DecimalConstantAttribute(0, 0, 0x00000000, 0x00000000, 0x00000000);
            decimal expectDecimal = 0;
            if (myDAttribute.Value != expectDecimal)
            {
                TestLibrary.TestFramework.LogError("001.1", "Occurs error when Construct DecimalConstantAttribute !");
                retVal = false;
            }

            myDAttribute = new DecimalConstantAttribute(28, 1, 0x00000000, 0x00000000, 0x00000000);
            expectDecimal = 0;
            if (myDAttribute.Value != expectDecimal)
            {
                TestLibrary.TestFramework.LogError("001.2", "Occurs error when Construct DecimalConstantAttribute,should return " + expectDecimal);
                retVal = false;
            }

            myDAttribute = new DecimalConstantAttribute(28, 0, 0x00000000, 0x00000000, 0x00000001);
            expectDecimal = 1e-28m;
            if (myDAttribute.Value != expectDecimal)
            {
                TestLibrary.TestFramework.LogError("001.3", "Occurs error when Construct DecimalConstantAttribute ,should return " + expectDecimal);
                retVal = false;
            }

            myDAttribute = new DecimalConstantAttribute(28, 1, 0x00000000, 0x00000000, 0x00000001);
            expectDecimal = -(1e-28m);
            if (myDAttribute.Value != expectDecimal)
            {
                TestLibrary.TestFramework.LogError("001.4", "Occurs error when Construct DecimalConstantAttribute ,should return " + expectDecimal);
                retVal = false;
            }
            myDAttribute = new DecimalConstantAttribute(28, 1, 0x00000001, 0x00000001, 0x00000001);
            expectDecimal = -0.0000000018446744078004518913m;
            if (myDAttribute.Value != expectDecimal)
            {
                TestLibrary.TestFramework.LogError("001.5", "Occurs error when Construct DecimalConstantAttribute ,should return " + expectDecimal);
                retVal = false;
            }

            myDAttribute = new DecimalConstantAttribute(28, 100, 0x00000001, 0x00000001, 0x00000001);
            expectDecimal = -0.0000000018446744078004518913m;
            if (myDAttribute.Value != expectDecimal)
            {
                TestLibrary.TestFramework.LogError("001.6", "Occurs error when Construct DecimalConstantAttribute ,should return " + expectDecimal);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }


}
