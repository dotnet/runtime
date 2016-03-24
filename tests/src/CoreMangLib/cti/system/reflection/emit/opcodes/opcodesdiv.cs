// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection.Emit;

/// <summary>
/// Div [v-jianq]
/// </summary>
///

public class OpCodesDiv
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        
        //
        // TODO: Add your negative test cases here
        //
        // TestLibrary.TestFramework.LogInformation("[Negative]");
        // retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify OpCodes Div field value");

        try
        {
            retVal = VerificationHelper(OpCodes.Div,
                "div", 
                StackBehaviour.Pop1_pop1,
                StackBehaviour.Push1, 
                OperandType.InlineNone,
                OpCodeType.Primitive,
                1, 
                (byte)0xff,
                (byte)0x5b, 
                FlowControl.Next, 
                "001.",
                "Div") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    //public bool NegTest1()
    //{
    //    bool retVal = true;

    //    TestLibrary.TestFramework.BeginScenario("NegTest1: ");

    //    try
    //    {
    //          //
    //          // Add your test logic here
    //          //
    //    }
    //    catch (Exception e)
    //    {
    //        TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
    //        TestLibrary.TestFramework.LogInformation(e.StackTrace);
    //        retVal = false;
    //    }

    //    return retVal;
    //}
    #endregion
    #endregion

    public static int Main()
    {
        OpCodesDiv test = new OpCodesDiv();

        TestLibrary.TestFramework.BeginTestCase("OpCodesDiv");

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
    private bool VerificationHelper(OpCode code,
        string name,
        StackBehaviour pop,
        StackBehaviour push,
        OperandType oprandType,
        OpCodeType type,
        int size,
        byte s1,
        byte s2,
        FlowControl ctrl,
        string errorno,
        string errordesp)
    {
        bool retVal = true;

        string actualName = code.Name;
        if (actualName != name)
        {
            TestLibrary.TestFramework.LogError(errorno + ".0", "Name returns wrong value for OpCode " + errordesp);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actualName = " + actualName + ", name = " + name);
            retVal = false;
        }

        StackBehaviour actualPop = code.StackBehaviourPop;
        if (actualPop != pop)
        {
            TestLibrary.TestFramework.LogError(errorno + ".1", "StackBehaviourPop returns wrong value for OpCode " + errordesp);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actualPop = " + actualPop + ", pop = " + pop);
            retVal = false;
        }

        StackBehaviour actualPush = code.StackBehaviourPush;
        if (actualPush != push)
        {
            TestLibrary.TestFramework.LogError(errorno + ".2", "StackBehaviourPush returns wrong value for OpCode " + errordesp);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actualPush = " + actualPush + ", push = " + push);
            retVal = false;
        }

        OperandType actualOperandType = code.OperandType;
        if (actualOperandType != oprandType)
        {
            TestLibrary.TestFramework.LogError(errorno + ".3", "OperandType returns wrong value for OpCode " + errordesp);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actualOperandType = " + actualOperandType + ", oprandType = " + oprandType);
            retVal = false;
        }

        OpCodeType actualOpCodeType = code.OpCodeType;
        if (actualOpCodeType != type)
        {
            TestLibrary.TestFramework.LogError(errorno + ".4", "OpCodeType returns wrong value for OpCode " + errordesp);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actualOpCodeType = " + actualOpCodeType + ", type = " + type);
            retVal = false;
        }

        int actualSize = code.Size;
        if (actualSize != size)
        {
            TestLibrary.TestFramework.LogError(errorno + ".5", "Size returns wrong value for OpCode " + errordesp);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actualSize = " + actualSize + ", size = " + size);
            retVal = false;
        }

        short expectedValue = 0;
        if (size == 2)
            expectedValue = (short)(s1 << 8 | s2);
        else
            expectedValue = (short)s2;

        short actualValue = code.Value;
        if (actualValue != expectedValue)
        {
            TestLibrary.TestFramework.LogError(errorno + ".6", "Value returns wrong value for OpCode " + errordesp);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actualValue = " + actualValue + ", s1 = " + s1 + ", s2 = " + s2 + ", expectedValue = " + expectedValue);
            retVal = false;
        }

        FlowControl actualCtrl = code.FlowControl;
        if (actualCtrl != ctrl)
        {
            TestLibrary.TestFramework.LogError(errorno + ".7", "FlowControl returns wrong value for OpCode " + errordesp);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actualCtrl = " + actualCtrl + ", ctrl = " + ctrl);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
