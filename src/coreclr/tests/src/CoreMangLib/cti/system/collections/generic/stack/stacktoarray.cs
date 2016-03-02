// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text; //for StringBuilder
using System.Collections.Generic;

/// <summary>
///  Stack.ToArray() [v-yaduoj]
/// </summary>
public class StackToArray
{
    public static int Main()
    {
        StackToArray testObj = new StackToArray();

        TestLibrary.TestFramework.BeginTestCase("for method: Stack.ToArray()");
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

        const string c_TEST_ID = "P001";
        string c_TEST_DESC = "PosTest1: copy data from non-empty stack to array.";
        string errorDesc;

        int[] operands = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Stack<int> operandStack = new Stack<int>((IEnumerable<int>)operands);
        int[] expectedValues = new int[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        int[] actualValues;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            actualValues = operandStack.ToArray();
            if (!this.VerifyToArray(actualValues, expectedValues))
            {
                errorDesc = "Failed to copy data from stack to array." +
                    "\nThe stack is " + this.GetArrayData(expectedValues) +
                    "\nThe actual array is " + this.GetArrayData(actualValues);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e + 
                "\nThe stack is " + this.GetArrayData(expectedValues);
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
        int[] expectedValues = new int[] {};
        int[] actualValues;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            actualValues = operandStack.ToArray();
            if (!this.VerifyToArray(actualValues, expectedValues))
            {
                errorDesc = "Failed to copy data from stack to array." +
                    "\nThe stack is empty." + 
                    "\nThe actual array is " + this.GetArrayData(actualValues);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe stack is " + this.GetArrayData(expectedValues);
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Helper methods for Positive tests
    private bool VerifyToArray(int[] actualValues, int[] expectedValues)
    {
        if (null == actualValues) return expectedValues == null;
        if (null == expectedValues) return false;
        if (actualValues.Length != expectedValues.Length) return false;
        for (int i = 0; i < actualValues.Length; ++i)
        {
            if (actualValues[i] != expectedValues[i]) return false;
        }

        return true;
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
}
