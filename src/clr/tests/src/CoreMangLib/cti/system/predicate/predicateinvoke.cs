// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

///<summary>
///System.Predicate.Invoke [v-yaduoj]
///</summary>

public class PredicateInvoke
{
    private const int c_CRITERIA = 80;

    public static int Main()
    {
        PredicateInvoke testObj = new PredicateInvoke();
        TestLibrary.TestFramework.BeginTestCase("for method of Predicate.Invoke");
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

        int count;
        bool actualValue;
        bool expectedValue;

        count = 20;
        expectedValue = count >= c_CRITERIA;

        TestLibrary.TestFramework.BeginScenario("PosTest1: call the method Predicate.EndInvoke synchronously");
        try
        {
            MyClass myClass = new MyClass(count);
            Predicate<int> selector = myClass.IsGreatEnough;
            actualValue = selector(c_CRITERIA);
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

        int count;
        bool actualValue;
        bool expectedValue;

        count = 99;
        expectedValue = count >= c_CRITERIA;

        TestLibrary.TestFramework.BeginScenario("PosTest2: call the method Predicate.EndInvoke synchronously");
        try
        {
            MyClass myClass = new MyClass(count);
            Predicate<int> selector = myClass.IsGreatEnough;
            actualValue = selector(c_CRITERIA);
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
    #endregion
}

class MyClass
{
    private int m_count;

    public MyClass(int count)
    {
        m_count = count;
    }

    public int Count
    {
        get
        {
            return m_count;
        }
    }

    public bool IsGreatEnough(int criteria)
    {
        return m_count >= criteria;
    }
}
