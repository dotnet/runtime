// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text; //for StringBuilder
using System.Collections.Generic;

/// <summary>
///  Stack Constructor(IEnumerable<T>) [v-yaduoj]
/// </summary>
public class StackTest
{
    public static int Main()
    {
        StackTest testObj = new StackTest();

        TestLibrary.TestFramework.BeginTestCase("for constructor: Stack(IEnumerable<T>)");
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
        string c_TEST_DESC = "PosTest1: initialize a new instance of statck<T> using a non-empty array.";
        string errorDesc;

        int[] operands = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Stack<int> operandStack = new Stack<int>((IEnumerable<int>)operands);
            if (!this.VerifyNewStack(operandStack, operands))
            {
                errorDesc = "Failed to initialize the new instance of Statck<T> using specified array." +
                    "\nThe actual items in the stack are " + this.GetStackData(operandStack) +
                    "\nThe expected items in the stack are " + this.GetExpectedItems(operands);
                TestLibrary.TestFramework.LogError(c_TEST_ID + ".1", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe specified array is " + this.GetArrayData(operands);
            TestLibrary.TestFramework.LogError(c_TEST_ID + ".2", errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        string c_TEST_DESC = "PosTest2: initialize a new instance of statck<T> using a zero-length array.";
        string errorDesc;

        int[] operands = new int[] { };
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Stack<int> operandStack = new Stack<int>((IEnumerable<int>)operands);
            if (!this.VerifyNewStack(operandStack, operands))
            {
                errorDesc = "Failed to initialize the new instance of Statck<T> using specified array." +
                    "\nThe actual items in the stack are " + this.GetStackData(operandStack) +
                    "\nThe expected items in the stack are " + this.GetExpectedItems(operands);
                TestLibrary.TestFramework.LogError(c_TEST_ID + ".1", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe specified array is " + this.GetArrayData(operands);
            TestLibrary.TestFramework.LogError(c_TEST_ID + ".2", errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Helper methods for Positive tests
    private bool VerifyNewStack(Stack<int> operandStack, int[] operands)
    {
        bool retVal = true;
        if (operands.Length != operandStack.Count) return false;

        int[] tempValues = operandStack.ToArray();
        for (int i = 0; i < tempValues.Length; ++i)
        {
            if (operands[i] != tempValues[tempValues.Length - 1 - i]) return false;
        }
        return retVal;
    }

    private string GetArrayData(int[] values)
    {
        if (null == values) return "null reference";
        StringBuilder sb = new StringBuilder();
        sb.Append("{");
        for (int i = 0; i < values.Length; ++i)
        {
            sb.AppendFormat("{0}, ", values[i]);
        }
        sb.Append("}");

        return sb.ToString();
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

    //Get expected items in stack from the input array, 
    //which is just the reverse of the input.
    private string GetExpectedItems(int[] inputValues)
    {
        if (null == inputValues) return "null reference";
        StringBuilder sb = new StringBuilder();
        sb.Append("{");
        for (int i = inputValues.Length - 1; i >= 0; --i)
        {
            sb.AppendFormat("{0}, ", inputValues[i]);
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
        string c_TEST_DESC = "NegTest1: collection is a null reference (Nothing in Visual Basic).";
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Stack<int> operandStack = new Stack<int>(null as IEnumerable<int>);
            errorDesc = "ArgumentNullException is not thrown as expected when collection is a null reference (Nothing in Visual Basic).";
            TestLibrary.TestFramework.LogError(c_TEST_ID + ".1", errorDesc);
            retVal = false;
        }
        catch (ArgumentNullException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nCollection is a null reference (Nothing in Visual Basic).";
            TestLibrary.TestFramework.LogError(c_TEST_ID + ".2", errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}