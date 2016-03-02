// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text; //for StringBuilder
using System.Collections.Generic;

/// <summary>
///  Stack.Enumerator.MoveNext() [v-yaduoj]
/// </summary>
public class StackEnumeratorMoveNext
{
    public static int Main()
    {
        StackEnumeratorMoveNext testObj = new StackEnumeratorMoveNext();

        TestLibrary.TestFramework.BeginTestCase("for method: Stack.Enumerator.MoveNext()");
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        string c_TEST_DESC = "PosTest1: Move the enumerator of non-empty stack.";
        string errorDesc;

        Stack<int>.Enumerator enumerator;
        int[] operands = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Stack<int> operandStack = new Stack<int>((IEnumerable<int>)operands);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            int[] expectedValues = new int[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
            enumerator = operandStack.GetEnumerator();
            retVal = this.VerifyEnumerator(c_TEST_ID, enumerator, expectedValues);
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe stack is " + GetStackData(operandStack);
            TestLibrary.TestFramework.LogError(c_TEST_ID + ".3", errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        string c_TEST_DESC = "PosTest2: Move the enumerator of empty stack.";
        string errorDesc;

        Stack<int>.Enumerator enumerator;
        Stack<int> operandStack = new Stack<int>();
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            enumerator = operandStack.GetEnumerator();
            retVal = this.VerifyEnumerator(c_TEST_ID, enumerator, new int[] { });
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe stack is " + GetStackData(operandStack);
            TestLibrary.TestFramework.LogError(c_TEST_ID + ".3", errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Helper methods for Positive tests
    private bool VerifyEnumerator(string testId, Stack<int>.Enumerator enumerator, int[] expectedValues)
    {
        string errorInfo;
        int i = 0;
        while (enumerator.MoveNext())
        {
            if (enumerator.Current != expectedValues[i])
            {
                errorInfo = "The element at the current positon [" + i + "] is not the value " +
                    expectedValues[i] + " as expected, actually is " + enumerator.Current + ".";
                TestLibrary.TestFramework.LogError(testId + ".1", errorInfo);
                return false;
            }
            ++i;
        };

        if (i != expectedValues.Length)
        {
            errorInfo = "The item count of the stack is not the value " +
                expectedValues.Length + " as expected, actually is " + i + ".";
            TestLibrary.TestFramework.LogError(testId + ".2", errorInfo);
            return false;
        }

        return true;
    }

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
        string c_TEST_DESC = "NegTest1: The collection was modified after the enumerator was created.";
        string errorDesc;

        Stack<int>.Enumerator enumerator;
        Stack<int> operandStack = new Stack<int>();
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            enumerator = operandStack.GetEnumerator();
            operandStack.Push(1);
            enumerator.MoveNext();
            errorDesc = "InvalidOperationException is not thrown as expected when the collection was modified after the enumerator was created.";
            TestLibrary.TestFramework.LogError(c_TEST_ID + ".1", errorDesc);
            retVal = false;
        }
        catch (InvalidOperationException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe collection was modified after the enumerator was created.";
            TestLibrary.TestFramework.LogError(c_TEST_ID + ".2", errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}