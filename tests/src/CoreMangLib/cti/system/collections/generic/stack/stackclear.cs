// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text; //for StringBuilder
using System.Collections.Generic;

/// <summary>
///  Stack.Clear() [v-yaduoj]
/// </summary>
public class StackClear
{
    public static int Main()
    {
        StackClear testObj = new StackClear();

        TestLibrary.TestFramework.BeginTestCase("for method: Stack.Clear()");
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

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string errorDesc;

        int[] operands = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        TestLibrary.TestFramework.BeginScenario("PosTest1: remove all the elements from a non-empty stack.");
        try
        {
            Stack<int> operandStack = new Stack<int>((IEnumerable<int>)operands);
            operandStack.Clear();
            if (0 != operandStack.Count)
            {
                errorDesc = "Failed to clear stack " + this.GetStackData(operandStack);
                TestLibrary.TestFramework.LogError("P001.1", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("P001.2", errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string errorDesc;

        int[] operands = new int[] { };
        TestLibrary.TestFramework.BeginScenario("PosTest2: remove all the elements from an empty stack.");
        try
        {
            Stack<int> operandStack = new Stack<int>((IEnumerable<int>)operands);
            operandStack.Clear();
            if (0 != operandStack.Count)
            {
                errorDesc = "Failed to clear stack " + this.GetStackData(operandStack);
                TestLibrary.TestFramework.LogError("P002.1", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("P002.2", errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Helper methods for positive tests
    private string GetStackData(Stack<int> values)
    {
        if (null == values) return "null reference";
        int[] tempVals = new int[values.Count];
        values.CopyTo(tempVals, 0);
        StringBuilder sb = new StringBuilder();
        sb.Append("{");
        for (int i = 0; i < tempVals.Length; ++i)
        {
            sb.AppendFormat("{0}, ", tempVals[i]);
        }
        sb.Append("}");

        return sb.ToString();
    }
    #endregion
}