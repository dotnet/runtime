// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection.Emit;

/// <summary>
/// System.Reflection.Emit.OpCode.StackBehaviourPop[v-jiajul]
/// </summary>
public class OpCodeStackBehaviourPop
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
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Add, StackBehaviour.Pop1_pop1, "001.0") && retVal;
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Ldc_I4, StackBehaviour.Pop0, "001.1") && retVal;
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Stloc, StackBehaviour.Pop1, "001.2") && retVal;
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Nop, StackBehaviour.Pop0, "001.3") && retVal;
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Localloc, StackBehaviour.Popi, "001.4") && retVal;
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Endfilter, StackBehaviour.Popi, "001.5") && retVal;
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Ret, StackBehaviour.Varpop, "001.6") && retVal;
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Callvirt, StackBehaviour.Varpop, "001.7") && retVal;
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Cpobj, StackBehaviour.Popi_popi, "001.8") && retVal;
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Ldobj, StackBehaviour.Popi, "001.9") && retVal;
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Prefix3, StackBehaviour.Pop0, "001.10") && retVal;
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Prefix6, StackBehaviour.Pop0, "001.11") && retVal;
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Prefixref, StackBehaviour.Pop0, "001.12") && retVal;
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Constrained, StackBehaviour.Pop0, "001.13") && retVal;
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Readonly, StackBehaviour.Pop0, "001.14") && retVal;
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Initblk, StackBehaviour.Popi_popi_popi, "001.15") && retVal;
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Ldvirtftn, StackBehaviour.Popref, "001.16") && retVal;
            retVal = VerifyOpCodeStackBehaviourPop(OpCodes.Sub_Ovf_Un, StackBehaviour.Pop1_pop1, "001.17") && retVal;
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
        OpCodeStackBehaviourPop test = new OpCodeStackBehaviourPop();

        TestLibrary.TestFramework.BeginTestCase("OpCodeStackBehaviourPop");

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
    private bool VerifyOpCodeStackBehaviourPop(OpCode op1, StackBehaviour sb, string errNum)
    {
        bool retVal = true;

        try
        {
            if (op1.StackBehaviourPop != sb)
            {
                TestLibrary.TestFramework.LogError(errNum, "Result is not the value as pop behaviour is: " + op1.StackBehaviourPop + ",Expected is: " + sb);
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
