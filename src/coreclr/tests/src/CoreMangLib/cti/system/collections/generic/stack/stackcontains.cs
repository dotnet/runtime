// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text; //for StringBuilder
using System.Collections.Generic;

/// <summary>
///  Stack.Contains(T) [v-yaduoj]
/// </summary>
public class StackContains
{
    public static int Main()
    {
        StackContains testObj = new StackContains();

        TestLibrary.TestFramework.BeginTestCase("for method: Stack.Contains(T)");
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
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        string c_TEST_DESC = "PosTest1: element exists in the value type stack.";
        string errorDesc;

        int[] operands = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Stack<int> operandStack = new Stack<int>((IEnumerable<int>)operands);
        int item = TestLibrary.Generator.GetInt32(-55) % operands.Length;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (!operandStack.Contains(item))
            {
                errorDesc = "Cannot find the item " + item 
                    + " , which exists in the stack indeed.\n The stack is " + GetStackData(operandStack);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe item to locate is " + item +  ", the stack is " + GetStackData(operandStack);
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        string c_TEST_DESC = "PosTest2: element does not exist in the value type stack.";
        string errorDesc;

        int[] operands = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Stack<int> operandStack = new Stack<int>((IEnumerable<int>)operands);
        int item = -1 * TestLibrary.Generator.GetInt32(-55) - 1;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (operandStack.Contains(item))
            {
                errorDesc = "The stack should not contain item " + item
                    + ".\nThe stack is " + GetStackData(operandStack);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe item to locate is " + item + ", the stack is " + GetStackData(operandStack);
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "P003";
        string c_TEST_DESC = "PosTest3: element does not exist in the empty value type stack.";
        string errorDesc;

        Stack<int> operandStack = new Stack<int>();
        int item = TestLibrary.Generator.GetInt32(-55);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (operandStack.Contains(item))
            {
                errorDesc = "The stack should not contain item " + item
                    + ".\nThe stack is empty stack";
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe item to locate is " + item + ", the stack is empty stack.";
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_ID = "P004";
        string c_TEST_DESC = "PosTest4: element exists in the reference type stack.";
        string errorDesc;

        string[] words = new string[] { "A", "B", "C", "D", "E", "F", "G", "7", "8", "\n" };
        Stack<string> wordStack = new Stack<string>((IEnumerable<string>)words);
        string item = words[TestLibrary.Generator.GetInt32(-55) % words.Length];
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (!wordStack.Contains(item))
            {
                errorDesc = "Cannot find the item \"" + item
                    + "\", which exists in the stack indeed. The stack is " + GetStackData(wordStack);
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe item to locate is \"" + item + "\", the stack is " + GetStackData(wordStack);
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        const string c_TEST_ID = "P005";
        string c_TEST_DESC = "PosTest5: element does not exist in the reference type stack.";
        string errorDesc;

        string[] words = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        Stack<string> wordStack = new Stack<string>((IEnumerable<string>)words);
        string item = "AA";
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (wordStack.Contains(item))
            {
                errorDesc = "The stack should not contain item \"" + item + 
                    "\".The stack is " + GetStackData(wordStack);
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe item to locate is \"" + item + "\", the stack is " + GetStackData(wordStack);
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        const string c_TEST_ID = "P006";
        string c_TEST_DESC = "PosTest6: element does not exist in the empty reference type stack.";
        string errorDesc;

        Stack<string> wordStack = new Stack<string>();
        string item = TestLibrary.Generator.GetString(-55, false, 1, 5);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (wordStack.Contains(item))
            {
                errorDesc = "The stack should not contain item \"" + item
                    + "\".The stack is empty stack";
                TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe item to locate is \"" + item + "\", and the stack is empty stack.";
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;

        const string c_TEST_ID = "P007";
        string c_TEST_DESC = "PosTest7: null reference exists in the reference type stack.";
        string errorDesc;

        string[] words = new string[] { null, "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        Stack<string> wordStack = new Stack<string>((IEnumerable<string>)words);
        string item = null;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (!wordStack.Contains(item))
            {
                errorDesc = "Cannot find item (null reference). The stack is" + 
                    GetStackData(wordStack);
                TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe item to locate is null reference. The stack is " + GetStackData(wordStack);
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;

        const string c_TEST_ID = "P008";
        string c_TEST_DESC = "PosTest8: null reference does not exist in the reference type stack.";
        string errorDesc;

        string[] words = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        Stack<string> wordStack = new Stack<string>((IEnumerable<string>)words);
        string item = null;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (wordStack.Contains(item))
            {
                errorDesc = "The stack should not contain item (null reference). The stack is" +
                    GetStackData(wordStack);
                TestLibrary.TestFramework.LogError("015" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe item to locate is null reference. The stack is " + GetStackData(wordStack);
            TestLibrary.TestFramework.LogError("016" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest9()
    {
        bool retVal = true;

        const string c_TEST_ID = "P009";
        string c_TEST_DESC = "PosTest9: null reference does not exist in the empty reference type stack.";
        string errorDesc;

        Stack<string> wordStack = new Stack<string>();
        string item = null;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            if (wordStack.Contains(item))
            {
                errorDesc = "The stack should not contain item (null reference). The stack is empty.";
                TestLibrary.TestFramework.LogError("017" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe item to locate is null reference. The stack is emtpy";
            TestLibrary.TestFramework.LogError("018" + " TestId-" + c_TEST_ID, errorDesc);
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

    private string GetStackData(Stack<string> values)
    {
        if (null == values) return "null reference";
        string[] tempVals = new string[values.Count];
        values.CopyTo(tempVals, 0);
        StringBuilder sb = new StringBuilder();
        sb.Append("{");
        for (int i = 0; i < tempVals.Length; ++i)
        {
            if (null == tempVals[i])
            {
                sb.AppendFormat("null,");
            }
            else
            {
                sb.AppendFormat("\"{0}\", ", tempVals[i]);
            }
        }
        sb.Append("}");

        return sb.ToString();
    }
    #endregion
}
