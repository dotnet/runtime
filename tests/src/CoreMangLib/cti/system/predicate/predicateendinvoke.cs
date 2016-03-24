// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

///<summary>
///System.Predicate.EndInvoke [v-yaduoj]
///</summary>

public class PredicateEndInvoke
{
    private const int c_CRITERIA = 80;

    public static int Main()
    {
        PredicateEndInvoke testObj = new PredicateEndInvoke();
        TestLibrary.TestFramework.BeginTestCase("for method of Predicate.EndInvoke");
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }
    #region Negative tests
    public bool NegTest1()
    {
        bool retVal = true;

        int count;
        bool actualValue;
        bool expectedValue;

        count = 20;
        expectedValue = count >= c_CRITERIA;

        TestLibrary.TestFramework.BeginScenario("NegTest1: call the method Predicate.EndInvoke asynchronously");
        try
        {
            MyClass myClass = new MyClass(count);
            Predicate<int> selector = myClass.IsGreatEnough;
            IAsyncResult asyncResult = null;
            actualValue = selector.EndInvoke(asyncResult);
            retVal = false;
            TestLibrary.TestFramework.LogError("003", "NotSupportedException expected");
        }
        catch (NotSupportedException)
        {
            // expected     
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
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
