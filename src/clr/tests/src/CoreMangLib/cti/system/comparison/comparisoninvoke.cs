// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

///<summary>
///System.Comparison.Invoke
///</summary>

public class ComparisonInvoke
{
    private const int c_MIN_ARRAY_LENGTH = 0;
    private const int c_MAX_ARRAY_LENGTH = byte.MaxValue;

    public static int Main()
    {
        ComparisonInvoke testObj = new ComparisonInvoke();
        TestLibrary.TestFramework.BeginTestCase("for method of Comparison.Invoke");
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

        return retVal;
    }
    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        char[] charsA, charsB;
        int actualValue;
        int expectedValue;

        charsA = new char[TestLibrary.Generator.GetInt32(-55) % (c_MAX_ARRAY_LENGTH + 1)];
        charsB = new char[TestLibrary.Generator.GetInt32(-55) % (c_MAX_ARRAY_LENGTH + 1)];

        TestLibrary.TestFramework.BeginScenario("PosTest1: call the method BeginInvoke");
        try
        {
            if (charsA.Length == charsB.Length) expectedValue = 0;
            else expectedValue = (charsA.Length > charsB.Length) ? 1 : -1;
            Comparison<Array> comparison = CompareByLength;
            actualValue = comparison.Invoke(charsA, charsB);
            if (actualValue != expectedValue)
            {
                TestLibrary.TestFramework.LogError("001", "ExpectedValue(" + expectedValue + ") != ActualValue(" + actualValue + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        int actualValue;
        int expectedValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2: call the method BeginInvoke");
        try
        {
            expectedValue = 0;
            Comparison<Array> comparison = CompareByLength;
            actualValue = comparison.Invoke(null as Array, null as Array);
            if (actualValue != expectedValue)
            {
                TestLibrary.TestFramework.LogError("003", "ExpectedValue(" + expectedValue + ") != ActualValue(" + actualValue + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        int actualValue;
        int expectedValue;

        char[] chars = new char[TestLibrary.Generator.GetByte(-55) % (c_MAX_ARRAY_LENGTH + 1)];

        TestLibrary.TestFramework.BeginScenario("PosTest3: call the method BeginInvoke");
        try
        {
            expectedValue = -1;
            Comparison<Array> comparison = CompareByLength;
            actualValue = comparison.Invoke(null as Array, chars);

            if (actualValue != expectedValue)
            {
                TestLibrary.TestFramework.LogError("005", "ExpectedValue(" + expectedValue + ") != ActualValue(" + actualValue + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        int actualValue;
        int expectedValue;

        char[] chars = new char[TestLibrary.Generator.GetByte(-55) % (c_MAX_ARRAY_LENGTH + 1)];

        TestLibrary.TestFramework.BeginScenario("PosTest4: call the method BeginInvoke");
        try
        {
            expectedValue = -1;
            Comparison<Array> comparison = CompareByLength;
            actualValue = comparison.Invoke(null as Array, chars);

            if (actualValue != expectedValue)
            {
                TestLibrary.TestFramework.LogError("007", "ExpectedValue(" + expectedValue + ") != ActualValue(" + actualValue + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion

    #region Helper methods
    private int CompareByLength(Array arrayA, Array arrayB)
    {
        if (null == arrayA)
        {
            if (null == arrayB) return 0;
            else return -1;
        }
        else
        {
            if (null == arrayB) return 1;
            else
            {
                if (arrayA.Length == arrayB.Length) return 0;
                else return (arrayA.Length > arrayB.Length) ? 1 : -1;
            }
        }
    }
    #endregion
}
