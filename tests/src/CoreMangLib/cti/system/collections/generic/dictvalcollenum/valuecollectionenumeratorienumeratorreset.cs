// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections;
/// <summary>
/// 
/// </summary>
public class ValueCollectionEnumeratorIEnumeratorReset
{
    public static int Main()
    {
        ValueCollectionEnumeratorIEnumeratorReset valCollectEnumerIEnumerReset = new ValueCollectionEnumeratorIEnumeratorReset();
        TestLibrary.TestFramework.BeginTestCase("ValueCollectionEnumeratorIEnumeratorReset");
        if (valCollectEnumerIEnumerReset.RunTests())
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
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Invoke the method Reset in the ValueCollection IEnumerator 1");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("1", "test1");
            dic.Add("2", "test2");
            IEnumerator valcollectIEnumer = (IEnumerator)new Dictionary<string, string>.ValueCollection(dic).GetEnumerator();
            while (valcollectIEnumer.MoveNext()) { }
            valcollectIEnumer.Reset();
            if (valcollectIEnumer.MoveNext())
            {
                if (valcollectIEnumer.Current.ToString() != "test1")
                {
                    TestLibrary.TestFramework.LogError("001", "the ExpectResult is not the ActualResult");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Invoke the method Reset in the ValueCollection IEnumerator 2");
        try
        {
            Dictionary<TestClass, TestClass> dic = new Dictionary<TestClass, TestClass>();
            TestClass Tkey1 = new TestClass();
            TestClass TVal1 = new TestClass();
            dic.Add(Tkey1, TVal1);
            IEnumerator valcollectIEnumer = (IEnumerator)new Dictionary<TestClass, TestClass>.ValueCollection(dic).GetEnumerator();
            while (valcollectIEnumer.MoveNext()) { }
            valcollectIEnumer.Reset();
            if (valcollectIEnumer.MoveNext())
            {
                if (!valcollectIEnumer.Current.Equals(TVal1))
                {
                    TestLibrary.TestFramework.LogError("003", "the ExpectResult is not the ActualResult");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:The collection was modified after the enumerator was created");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            IEnumerator valcollectIEnumer = (IEnumerator)new Dictionary<string, string>.ValueCollection(dic).GetEnumerator();
            dic.Add("str1", "helloworld");
            valcollectIEnumer.Reset();
            TestLibrary.TestFramework.LogError("N001", "The collection was modified after the enumerator was created but not throw exception");
            retVal = false;
        }
        catch (InvalidOperationException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region ForTestClass
    public class TestClass { }
    #endregion
}