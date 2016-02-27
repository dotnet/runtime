// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text; //for StringBuilder
using System.Collections.Generic;

/// <summary>
///  Stack.CopyTo(T[], Int32) [v-yaduoj]
/// </summary>
public class StackCopyTo
{
    private const int c_MAX_ARRAY_LENGTH = 1000;

    public static int Main()
    {
        StackCopyTo testObj = new StackCopyTo();

        TestLibrary.TestFramework.BeginTestCase("for method: Stack.CopyTo(T[], Int32)");
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
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        string c_TEST_DESC = "PosTest1: copy data from non-empty stack to array.";
        string errorDesc;

        int[] operands = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Stack<int> operandStack = new Stack<int>((IEnumerable<int>)operands);
        int[] expectedValues = new int[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        int[] actualValues;
        int startIndex, length;
        length = operands.Length + 
            TestLibrary.Generator.GetInt32() % (c_MAX_ARRAY_LENGTH - operands.Length + 1);
        actualValues = new int[length];

        if(actualValues.Length == operands.Length) {
            startIndex = 0;
        }
        else {	
            startIndex = TestLibrary.Generator.GetInt32() % (actualValues.Length - operands.Length);
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            operandStack.CopyTo(actualValues, startIndex);
            if (!this.VerifyCopyToResult(actualValues, startIndex, expectedValues))
            {
                errorDesc = "Failed to copy data from stack to array." +
                    "\nThe stack is " + this.GetArrayData(expectedValues) +
                    "\nThe start index is " + startIndex + ", and actual array elements is " +
                    this.GetArrayData(actualValues, startIndex, expectedValues.Length);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe stack is " + this.GetArrayData(expectedValues) + 
                "\nThe start index is " + startIndex + ".";
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        string c_TEST_DESC = "PosTest2: copy data from empty stack to array.";
        string errorDesc;

        Stack<int> operandStack = new Stack<int>();
        int[] actualValues;
        int startIndex, length;
        length = (TestLibrary.Generator.GetInt32() % (c_MAX_ARRAY_LENGTH))+1;
        actualValues = new int[length];
        int expectedValue = -1;

        startIndex = TestLibrary.Generator.GetInt32() % actualValues.Length;	
        actualValues[startIndex] = expectedValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            operandStack.CopyTo(actualValues, startIndex);
            if (actualValues[startIndex] != expectedValue)
            {
                errorDesc = "Failed to copy data from empty stack to array." + 
                    "\nThe start index is " + startIndex + "and values at this postion is " +
                    actualValues[startIndex] + ".";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e + 
                "\nThe stack is empty." +
                "\nThe start index is " + startIndex + "and values at this postion is " + 
                actualValues[startIndex] + ".";
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Helper methods for Positive tests
    private bool VerifyCopyToResult(int[] actualValues, int startIndex, int[] expectedValues)
    {
        bool retVal = true;

        if (actualValues.Length - startIndex < expectedValues.Length) return false;
        for (int i = 0; i < expectedValues.Length; ++i)
        {
            if (expectedValues[i] != actualValues[startIndex + i]) return false;
        }

        return retVal;
    }

    private string GetArrayData(int[] values)
    {
        return this.GetArrayData(values, 0, values.Length);
    }

    private string GetArrayData(int[] values, int startIndex, int count)
    {
        if (null == values) return "null reference";
        if (startIndex < 0 || startIndex >= values.Length) throw new ArgumentOutOfRangeException("startIndex");
        if (startIndex + count > values.Length) throw new ArgumentOutOfRangeException("count");

        StringBuilder sb = new StringBuilder();
        sb.Append("{");
        for (int i = startIndex; i < count; ++i)
        {
            sb.AppendFormat("{0}, ", values[i]);
        }
        sb.Append("}");

        return sb.ToString();
    }
    #endregion

    #region  Negative tests
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "N001";
        string c_TEST_DESC = "NegTest1: array is a null reference (Nothing in Visual Basic).";
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Stack<int> operandStack = new Stack<int>();
            operandStack.CopyTo(null, 0);
            errorDesc = "ArgumentNullException is not thrown as expected when array is a null reference (Nothing in Visual Basic).";
            TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (ArgumentNullException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\narray is a null reference (Nothing in Visual Basic).";
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "N002";
        string c_TEST_DESC = "NegTest2: arrayIndex is less than zero.";
        string errorDesc;

        int[] values = new int[TestLibrary.Generator.GetInt32() % (c_MAX_ARRAY_LENGTH + 1)];
        int startIndex = -1 * TestLibrary.Generator.GetInt32() - 1;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Stack<int> operandStack = new Stack<int>();
            operandStack.CopyTo(values, startIndex);
            errorDesc = "ArgumentOutOfRangeException is not thrown as expected when arrayIndex is " + 
                startIndex + ".";
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\narrayIndex is " + startIndex + ".";
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "N003";
        string c_TEST_DESC = "NegTest3: The number of elements in the source Stack is greater than the available space from arrayIndex to the end of the destination array.";
        string errorDesc;

        int[] operands = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Stack<int> operandStack = new Stack<int>((IEnumerable<int>)operands);
        int[] values = new int[TestLibrary.Generator.GetInt32() % (operandStack.Count)];
        int startIndex = 0;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            operandStack.CopyTo(values, startIndex);
            errorDesc = "ArgumentException is not thrown as expected when arrayIndex is " +
                startIndex + ", the length of array is " + values.Length;
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (ArgumentException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\narrayIndex is " + startIndex + ", the length of array is " + values.Length;
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        const string c_TEST_ID = "N004";
        string c_TEST_DESC = "NegTest4: arrayIndex is equal to or greater than the length of array.";
        string errorDesc;

        int[] operands = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Stack<int> operandStack = new Stack<int>((IEnumerable<int>)operands);
        int length = operands.Length +
            TestLibrary.Generator.GetInt32() % (c_MAX_ARRAY_LENGTH - operands.Length + 1);
        int[] values = new int[length];
        int startIndex = values.Length + TestLibrary.Generator.GetInt32() % (int.MaxValue - length + 1);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            operandStack.CopyTo(values, startIndex);
            errorDesc = "ArgumentException is not thrown as expected when arrayIndex is " +
                startIndex + ", the length of array is " + values.Length;
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (ArgumentException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\narrayIndex is " + startIndex + ", the length of array is " + values.Length;
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
