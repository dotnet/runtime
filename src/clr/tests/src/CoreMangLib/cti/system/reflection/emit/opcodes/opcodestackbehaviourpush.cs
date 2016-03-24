// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection.Emit;

/// <summary>
/// System.Reflection.Emit.OpCode.StackBehaviourPush[v-jiajul]
/// </summary>
public class OpCodeStackBehaviourPush
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
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Add, StackBehaviour.Push1, "001.0") && retVal;
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Ldc_I4, StackBehaviour.Pushi, "001.1") && retVal;
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Stloc, StackBehaviour.Push0, "001.2") && retVal;
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Nop, StackBehaviour.Push0, "001.3") && retVal;
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Localloc, StackBehaviour.Pushi, "001.4") && retVal;
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Ldelem_R8, StackBehaviour.Pushr8, "001.5") && retVal;
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Ret, StackBehaviour.Push0, "001.6") && retVal;
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Callvirt, StackBehaviour.Varpush, "001.7") && retVal;
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Cpobj, StackBehaviour.Push0, "001.8") && retVal;
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Ldobj, StackBehaviour.Push1, "001.9") && retVal;
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Prefix3, StackBehaviour.Push0, "001.10") && retVal;
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Ldelem_Ref, StackBehaviour.Pushref, "001.11") && retVal;
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Ldelem_R4, StackBehaviour.Pushr4, "001.12") && retVal;
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Constrained, StackBehaviour.Push0, "001.13") && retVal;
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Readonly, StackBehaviour.Push0, "001.14") && retVal;
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Ldelem_I8, StackBehaviour.Pushi8, "001.15") && retVal;
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Ldvirtftn, StackBehaviour.Pushi, "001.16") && retVal;
            retVal = VerifyOpCodeStackBehaviourPush(OpCodes.Sub_Ovf_Un, StackBehaviour.Push1, "001.17") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    #endregion
    #endregion

    public static int Main()
    {
        OpCodeStackBehaviourPush test = new OpCodeStackBehaviourPush();

        TestLibrary.TestFramework.BeginTestCase("OpCodeStackBehaviourPush");

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
    private bool VerifyOpCodeStackBehaviourPush(OpCode op1, StackBehaviour sb, string errNum)
    {
        bool retVal = true;

        try
        {
            if (op1.StackBehaviourPush != sb)
            {
                TestLibrary.TestFramework.LogError(errNum, "Result is not the value as expected,push behaviour is: " + op1.StackBehaviourPush + ",Expected is: " + sb);
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
