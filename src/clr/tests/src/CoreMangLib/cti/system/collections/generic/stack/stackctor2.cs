// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
///  Stack Constructor(Int32) [v-yaduoj]
/// </summary>
public class StackTest
{
    public static int Main()
    {
        StackTest testObj = new StackTest();

        TestLibrary.TestFramework.BeginTestCase("for constructor: Stack(Int32)");
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
        const string c_TEST_DESC = "PosTest1: initialize a new instance of statck<T> using a positive default initial capacity.";
        string errorDesc;

        int defaultInitialCapacity = 100;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Stack<int> operandStack = new Stack<int>(defaultInitialCapacity);
            int actualCount = operandStack.Count;
            if (0 != actualCount)
            {
                errorDesc = "The item count of stack is not the value 0 as expected, actually " + actualCount
                    + "\nThe specified default capacity is (" + defaultInitialCapacity + ")";
                TestLibrary.TestFramework.LogError("P001.1", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe specified default capacity is (" + defaultInitialCapacity + ")";
            TestLibrary.TestFramework.LogError("P001.2", errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2: initialize a new instance of statck<T> using a default initial capacity whose value is zero.";
        string errorDesc;

        int defaultInitialCapacity = 0;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Stack<int> operandStack = new Stack<int>(defaultInitialCapacity);
            int actualCount = operandStack.Count;
            if (0 != actualCount)
            {
                errorDesc = "The item count of stack is not the value 0 as expected, actually " + actualCount
                    + "\nThe specified default capacity is (" + defaultInitialCapacity + ")";
                TestLibrary.TestFramework.LogError("P002.1", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe specified default capacity is (" + defaultInitialCapacity + ")";
            TestLibrary.TestFramework.LogError("P002.2", errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Negative tests
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "N001";
        const string c_TEST_DESC = "NegTest1: capacity is less than zero.";
        string errorDesc;

        int defaultInitialCapacity;
        defaultInitialCapacity = -1 * TestLibrary.Generator.GetInt32(-55) - 1;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Stack<int> operandStack = new Stack<int>(defaultInitialCapacity);
            errorDesc = "ArgumentOutOfRangeException is not thrown as expected when the speified default initial capacity  is ("
                + defaultInitialCapacity + ")";
            TestLibrary.TestFramework.LogError(c_TEST_ID + ".1", errorDesc);
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e +
                "\nThe specified default capacity is (" + defaultInitialCapacity + ")";
            TestLibrary.TestFramework.LogError(c_TEST_ID + ".2", errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
