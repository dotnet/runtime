// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection.Emit;

/// <summary>
/// System.Reflection.Emit.OpCode.Equals(Object)[v-jiajul]
/// </summary>
public class OpCodeFlowControl
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check the value of the property");

        try
        {
            retVal = VerifyFlowControl(OpCodes.Add, FlowControl.Next, "001.0") && retVal;
            retVal = VerifyFlowControl(OpCodes.Ldc_I4, FlowControl.Next, "001.1") && retVal;
            retVal = VerifyFlowControl(OpCodes.Stloc, FlowControl.Next, "001.2") && retVal;
            retVal = VerifyFlowControl(OpCodes.Nop, FlowControl.Next, "001.3") && retVal;
            retVal = VerifyFlowControl(OpCodes.Localloc, FlowControl.Next, "001.4") && retVal;
            retVal = VerifyFlowControl(OpCodes.Endfilter, FlowControl.Return, "001.5") && retVal;
            retVal = VerifyFlowControl(OpCodes.Ret, FlowControl.Return, "001.6") && retVal;
            retVal = VerifyFlowControl(OpCodes.Prefixref, FlowControl.Meta, "001.7") && retVal;
            retVal = VerifyFlowControl(OpCodes.Prefix3, FlowControl.Meta, "001.8") && retVal;
            retVal = VerifyFlowControl(OpCodes.Leave, FlowControl.Branch, "001.9") && retVal;
            retVal = VerifyFlowControl(OpCodes.Throw, FlowControl.Throw, "001.10") && retVal;
            retVal = VerifyFlowControl(OpCodes.Rethrow, FlowControl.Throw, "001.11") && retVal;
            retVal = VerifyFlowControl(OpCodes.Ble, FlowControl.Cond_Branch, "001.12") && retVal;
            retVal = VerifyFlowControl(OpCodes.Blt_S, FlowControl.Cond_Branch, "001.13") && retVal;
            retVal = VerifyFlowControl(OpCodes.Br_S, FlowControl.Branch, "001.14") && retVal;
            retVal = VerifyFlowControl(OpCodes.Jmp, FlowControl.Call, "001.15") && retVal;
            retVal = VerifyFlowControl(OpCodes.Call, FlowControl.Call, "001.16") && retVal;
            retVal = VerifyFlowControl(OpCodes.Callvirt, FlowControl.Call, "001.17") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    //public bool PosTest2()
    //{
    //    bool retVal = true;

    //    TestLibrary.TestFramework.BeginScenario("PosTest2: ");

    //    try
    //    {
    //          //
    //          // Add your test logic here
    //          //
    //    }
    //    catch (Exception e)
    //    {
    //        TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
    //        retVal = false;
    //    }

    //    return retVal;
    //}


    #endregion

    #region Nagetive Test Cases
    #endregion
    #endregion

    public static int Main()
    {
        OpCodeFlowControl test = new OpCodeFlowControl();

        TestLibrary.TestFramework.BeginTestCase("OpCodeFlowControl");

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
    #region Private Methods
    private bool VerifyFlowControl(OpCode op1, FlowControl fc, string errNum)
    {
        bool retVal = true;

        try
        {
            if (op1.FlowControl != fc)
            {
                TestLibrary.TestFramework.LogError(errNum, "Result is not the value as expected,FlowControl is: " + op1.FlowControl + ",Expected is: " + fc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(errNum, "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
