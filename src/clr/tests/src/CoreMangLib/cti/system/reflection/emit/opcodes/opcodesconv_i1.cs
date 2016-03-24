// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection.Emit;

/// <summary>
/// OpCodes.Conv_I1 [v-yaduoj]
/// </summary>
public class OpCodesTest
{
    public static int Main()
    {
        OpCodesTest testObj = new OpCodesTest();

        TestLibrary.TestFramework.BeginTestCase("for Field: OpCodes.Conv_I1");
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

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Verify OpCodes.Conv_I1.";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            retVal = VerifyAllTheFileds(OpCodes.Conv_I1,
                                        "conv.i1", 
                                        StackBehaviour.Pop1, 
                                        StackBehaviour.Pushi, 
                                        OperandType.InlineNone, 
                                        OpCodeType.Primitive, 
                                        1, (byte)0xff, (byte)0x67, 
                                        FlowControl.Next, 
                                        "001");
        }
        catch (Exception e)
        {
            string errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("002", errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Helper methods for positive tests
    private bool VerifyAllTheFileds(OpCode opCode, 
                                    String opCodeName, 
                                    StackBehaviour pop, 
                                    StackBehaviour push, 
                                    OperandType operandType, 
                                    OpCodeType type, 
                                    int size, 
                                    byte s1, byte s2, 
                                    FlowControl ctrl, 
                                    string errorNum)
    {
        bool retVal = true;
        string errorDesc;

        string actualName = opCode.Name;
        if (actualName != opCodeName)
        {
            errorDesc = "Actual name of the specified MSIL instruction: \"" + actualName +
                        "\" does not equal expected name: \"" + opCodeName + "\"";
            TestLibrary.TestFramework.LogError( errorNum + ".1", errorDesc);
            retVal = false;
        }

        StackBehaviour actualStackBehaviourPop = opCode.StackBehaviourPop;
        if (actualStackBehaviourPop != pop)
        {
            errorDesc = "Actual pop statck behaviour of the specified MSIL instruction: (" + actualStackBehaviourPop +
                        ") does not equal expected pop stack behaviour: (" + pop + ")";
            TestLibrary.TestFramework.LogError(errorNum + ".2", errorDesc);
            retVal = false;
        }

        StackBehaviour actualStackBehaviourPush = opCode.StackBehaviourPush;
        if (actualStackBehaviourPush != push)
        {
            errorDesc = "Actual push statck behaviour of the specified MSIL instruction: (" + actualStackBehaviourPush +
                        ") does not equal expected push stack behaviour: (" + push + ")";
            TestLibrary.TestFramework.LogError(errorNum + ".3", errorDesc);
            retVal = false;
        }


        OperandType actualOperandType = opCode.OperandType;
        if (actualOperandType != operandType)
        {
            errorDesc = "Actual operand type of the specified MSIL instruction: (" + actualOperandType +
                        ") does not equal expected operand type: (" + operandType + ")";
            TestLibrary.TestFramework.LogError(errorNum + ".4", errorDesc);
            retVal = false;
        }

        OpCodeType actualOpCodeType = opCode.OpCodeType;
        if (actualOpCodeType != type)
        {
            errorDesc = "Actual OpCode type of the specified MSIL instruction: (" + actualOpCodeType +
                        ") does not equal expected OpCode type: (" + type + ")";
            TestLibrary.TestFramework.LogError(errorNum + ".5", errorDesc);
            retVal = false;
        }

        int actualSize = opCode.Size;
        if (actualSize != size)
        {
            errorDesc = "Actual size of the specified MSIL instruction: (" + actualSize +
                        ") does not equal expected size: (" + size + ")";
            TestLibrary.TestFramework.LogError(errorNum + ".6", errorDesc);
            retVal = false;
        }

        short actualValue = opCode.Value;
        short expectedValue = (2 == size) ? (short)(s1 << 8 | s2) : s2;
        if (actualValue != expectedValue)
        {
            errorDesc = "Actual immediate operand value of the specified MSIL instruction: (" + actualValue +
                        ") does not equal expected immediate operand value: (" + expectedValue + ")";
            TestLibrary.TestFramework.LogError(errorNum + ".7", errorDesc);
            retVal = false;
        }

        FlowControl actualCtrl = opCode.FlowControl;
        if (actualCtrl != ctrl)
        {
            errorDesc = "Actual flow control of the specified MSIL instruction: (" + actualCtrl +
                        ") does not equal expected flow control: (" + ctrl + ")";
            TestLibrary.TestFramework.LogError(errorNum + ".8", errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}