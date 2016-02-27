// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;

/// <summary>
/// ctor(System.Byte,System.Byte,System.UInt32,System.UInt32,System.UInt32)
/// </summary>
public class DecimalConstantAttributeCtor
{
  

    public static int Main(string[] args)
    {
        DecimalConstantAttributeCtor DecimalConstantAttributeCtor = new DecimalConstantAttributeCtor();
        TestLibrary.TestFramework.BeginTestCase("DecimalConstantAttributeCtor");

        if (DecimalConstantAttributeCtor.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Create a new DecimalConstantAttribute instance.");

        try
        {
            DecimalConstantAttribute myDAttribute = new DecimalConstantAttribute(0, 0, 0x00000000, 0x00000000, 0x00000000);

            if (myDAttribute == null)
            {
                TestLibrary.TestFramework.LogError("001.1", "Occurs error when Construct DecimalConstantAttribute !");
                retVal = false;
            }
           
            myDAttribute = new DecimalConstantAttribute(28, 1, 0x00000000, 0x00000000, 0x00000000);

            if (myDAttribute == null)
            {
                TestLibrary.TestFramework.LogError("001.2", "Occurs error when Construct DecimalConstantAttribute !");
                retVal = false;
            }

            myDAttribute = new DecimalConstantAttribute(28, 0, 0x00000000, 0x00000000, 0x00000001);

            if (myDAttribute == null)
            {
                TestLibrary.TestFramework.LogError("001.3", "Occurs error when Construct DecimalConstantAttribute !");
                retVal = false;
            }

            myDAttribute = new DecimalConstantAttribute(28, 1, 0x00000000, 0x00000000, 0x00000001);

            if (myDAttribute == null)
            {
                TestLibrary.TestFramework.LogError("001.4", "Occurs error when Construct DecimalConstantAttribute !");
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

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: scale > 28.");

        try
        {
            DecimalConstantAttribute myDAttribute = new DecimalConstantAttribute(29, 0, 0x00000000, 0x00000000, 0x00000000);
            TestLibrary.TestFramework.LogError("101.1", "ArgumentOutOfRangeException should be caught.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.0", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
