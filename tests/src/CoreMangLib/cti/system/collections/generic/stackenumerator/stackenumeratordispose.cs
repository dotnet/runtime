// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text; //for StringBuilder
using System.Collections.Generic;

/// <summary>
///  Stack.Enumerator.Dispose() [v-yaduoj]
/// </summary>
public class StackEnumeratorDispose
{
    public static int Main()
    {
        StackEnumeratorDispose testObj = new StackEnumeratorDispose();

        TestLibrary.TestFramework.BeginTestCase("for method: Stack.Enumerator.Dispose()");
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
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        string c_TEST_DESC = "PosTest1: Release resources used by enumerator of non-empty stack.";
        string errorDesc;

        Stack<int>.Enumerator enumerator;
        int[] operands = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Stack<int> operandStack = new Stack<int>((IEnumerable<int>)operands);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            enumerator = operandStack.GetEnumerator();
            enumerator.Dispose();
            if (enumerator.MoveNext())
            {
                errorDesc = "Failed to release resources used by the stack enumerator. The stack is " +
                    this.GetStackData(operandStack);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe stack is " + GetStackData(operandStack);
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        string c_TEST_DESC = "PosTest2: Release resources used by enumerator of empty stack.";
        string errorDesc;

        Stack<int>.Enumerator enumerator;
        Stack<int> operandStack = new Stack<int>();
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            enumerator = operandStack.GetEnumerator();
            enumerator.Dispose();
            if (enumerator.MoveNext())
            {
                errorDesc = "Failed to release resources used by the stack enumerator. The stack is emtpy.";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e + "\nThe stack is empty.";
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "P003";
        string c_TEST_DESC = "PosTest3: Release resources used by enumerator of non-empty stack repeatedly.";
        string errorDesc;

        Stack<int>.Enumerator enumerator;
        int[] operands = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Stack<int> operandStack = new Stack<int>((IEnumerable<int>)operands);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            enumerator = operandStack.GetEnumerator();
            enumerator.Dispose();
            enumerator.Dispose();
            if (enumerator.MoveNext())
            {
                errorDesc = "Failed to release resources used by the stack enumerator. The stack is " +
                    this.GetStackData(operandStack);
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe stack is " + GetStackData(operandStack);
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
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
}
