// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text; // for StringBuilder
using System.Collections.Generic;

/// <summary>
///  Stack.Pop() [v-yaduoj]
/// </summary>
public class StackPop
{
    public static int Main()
    {
        StackPop testObj = new StackPop();

        TestLibrary.TestFramework.BeginTestCase("for method: Stack.Pop()");
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        string c_TEST_DESC = "PosTest1: Pop the top item from the non-empty stack.";
        string errorDesc;

        Stack<int> operandStack = new Stack<int>();
        int expectedValue = TestLibrary.Generator.GetInt32(-55);
        int actualValue;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            operandStack.Push(expectedValue);
            int expectedCount = operandStack.Count - 1;
            actualValue = operandStack.Pop();
            int actualCount = operandStack.Count;
            if (actualValue != expectedValue)
            {
                errorDesc = "The item at the top of stack is not the value " + 
                    expectedValue + " as expected, actually(" + actualValue + 
                    "). The stack is " + this.GetStackData(operandStack);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
            if (actualCount != expectedCount)
            {
                errorDesc = "The number of elements in the stack after popping is not the value " +
                    expectedCount + " as expected, actually(" + actualCount + 
                    ").\nThe stack is " + this.GetStackData(operandStack);
                TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe stack is " + GetStackData(operandStack);
            TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Helper methods for Positive tests
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

    #region Negative tests
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "N001";
        string c_TEST_DESC = "NegTest1: The Stack is empty.";
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Stack<int> operandStack = new Stack<int>();
            operandStack.Pop();
            errorDesc = "InvalidOperationException is not thrown as expected when the stack is empty.";
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (InvalidOperationException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e + "\nThe stack is empty.";
            TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
