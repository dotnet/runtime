// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Dictionary.ValueCollection.Enumerator.System.Collections.IEnumerator.Current
/// </summary>
public class ValueCollectionEnumeratorIEnumeratorCurrent
{
    public static int Main()
    {
        ValueCollectionEnumeratorIEnumeratorCurrent valCollectEnumerIEnumerCurrent = new ValueCollectionEnumeratorIEnumeratorCurrent();
        TestLibrary.TestFramework.BeginTestCase("ValueCollectionEnumeratorIEnumeratorCurrent");
        if (valCollectEnumerIEnumerCurrent.RunTests())
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
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the property Current in ValueCollection IEnumerator 1");
        try
        {
            Dictionary<TestClass, TestClass> dic = new Dictionary<TestClass, TestClass>();
            TestClass Tkey1 = new TestClass();
            TestClass TVal1 = new TestClass();
            dic.Add(Tkey1, TVal1);
            IEnumerator valEnumerIEnumer = (IEnumerator)new Dictionary<TestClass, TestClass>.ValueCollection(dic).GetEnumerator();
            while (valEnumerIEnumer.MoveNext())
            {
                if (!valEnumerIEnumer.Current.Equals(TVal1))
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Return the property Current in ValueCollection IEnumerator 2");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("1", "test1");
            IEnumerator valEnumerIEnumer = (IEnumerator)new Dictionary<string, string>.ValueCollection(dic).GetEnumerator();
            while (valEnumerIEnumer.MoveNext())
            {
                if (valEnumerIEnumer.Current.ToString() != "test1")
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
    #region ForTestClass
    public class TestClass { }
    #endregion
}