// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;
using System.Reflection.Emit;

///<summary>
///System.Reflection.Emit.OpCodes.TakesSingleByteArgument(OpCode) [v-zuolan]
///</summary>

public class OpCodesTakesSingleByteArgument
{

    public static int Main()
    {
        OpCodesTakesSingleByteArgument testObj = new OpCodesTakesSingleByteArgument();
        TestLibrary.TestFramework.BeginTestCase("for method of System.Reflection.Emit.OpCodes.TakesSingleByteArugment");
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
        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }


    #region Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        bool expectedValue = true;
        bool actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:invoke the method with single byte argument.");
        try
        {
            actualValue = OpCodes.TakesSingleByteArgument(OpCodes.Bne_Un_S);
            //Bne_Un_S is a single byte opcode

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

        bool expectedValue = false;
        bool actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2:invoke the method with multi-bytes argument.");
        try
        {
            actualValue = OpCodes.TakesSingleByteArgument(OpCodes.Br);
            //Br isn't a single byte opcode.

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
