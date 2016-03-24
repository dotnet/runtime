// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection.Emit;

///<summary>
///System.Reflection.Emit.OpCodes.Tailcall [v-zuolan]
///</summary>

public class OpCodesTailcall
{

    public static int Main()
    {
        OpCodesTailcall testObj = new OpCodesTailcall();
        TestLibrary.TestFramework.BeginTestCase("for field of System.Reflection.Emit.OpCodes.Tailcall");
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

    #region Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        CompareResult expectedValue = CompareResult.Equal;
        CompareResult actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:get the property and verify its fields");
        try
        {
            OpCode opcode = OpCodes.Tailcall;

            actualValue = CompareOpCode(opcode, "tail.", StackBehaviour.Pop0, StackBehaviour.Push0, OperandType.InlineNone, OpCodeType.Prefix, 2, (byte)0xfe, (byte)0x14, FlowControl.Meta);

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("001", "Errors accured in fields:" + GetResult(actualValue));
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

    #endregion

    #region helper method

    //verify the opcode fields
    //if not equal,retun the field name which contains error. 
    private CompareResult CompareOpCode(
        OpCode opcode,
        String stringname,
        StackBehaviour pop,
        StackBehaviour push,
        OperandType operand,
        OpCodeType type,
        int size,
        byte s1,
        byte s2,
        FlowControl ctrl)
    {
        CompareResult returnValue = CompareResult.Equal;
        if (opcode.Name != stringname) returnValue = returnValue | CompareResult.Name;
        if (opcode.StackBehaviourPop != pop) returnValue = returnValue | CompareResult.Pop;
        if (opcode.StackBehaviourPush != push) returnValue = returnValue | CompareResult.Push;
        if (opcode.OperandType != operand) returnValue = returnValue | CompareResult.OpenrandType;
        if (opcode.OpCodeType != type) returnValue = returnValue | CompareResult.OpCodeType;
        if (opcode.Size != size) returnValue = returnValue | CompareResult.Size;
        if (size == 2)
        {
            if (opcode.Value != ((short)(s1 << 8 | s2)))
            {
                returnValue = returnValue | CompareResult.Value;
            }
        }
        else
        {
            if (opcode.Value != ((short)s2))
            {
                returnValue = returnValue | CompareResult.Value;
            }
        }
        if (opcode.FlowControl != ctrl)
        {
            returnValue = returnValue | CompareResult.FlowControl;
        }
        return returnValue;
    }


    //Transform numeric result into string description
    public String GetResult(CompareResult result)
    {
        String retVal = null;
        for (int i = 0; i < 8; i++)
        {
            if (((int)result & (0x01 << i)) == (0x01 << i))
            {
                if (retVal == null)
                {
                    retVal = "{" + ((CompareResult)(0x01 << i)).ToString();
                }
                else
                {
                    retVal = retVal + "," + ((CompareResult)(0x01 << i)).ToString();
                }
            }
        }
        if (retVal == null)
        {
            retVal = "{Equal}";
        }
        else
        {
            retVal = retVal + "}";
        }
        return retVal;
    }
    #endregion
}

#region Enum CompareResult

public enum CompareResult
{
    Equal = 0x00,
    Name = 0x01,
    Pop = 0x02,
    Push = 0x04,
    OpenrandType = 0x08,
    OpCodeType = 0x10,
    Size = 0x20,
    Value = 0x40,
    FlowControl = 0x80
}

#endregion