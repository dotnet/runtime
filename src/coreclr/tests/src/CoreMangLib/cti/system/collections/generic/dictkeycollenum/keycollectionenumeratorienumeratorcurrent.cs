// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections;
/// <summary>
/// Dictionary.KeyCollection.Enumerator.System.Collections.IEnumerator.Current
/// </summary>
public class KeyCollectionEnumeratorIEnumeratorCurrent
{
    public static int Main()
    {
        KeyCollectionEnumeratorIEnumeratorCurrent keyCollectEnumerIEnumerCurrent = new KeyCollectionEnumeratorIEnumeratorCurrent();
        TestLibrary.TestFramework.BeginTestCase("KeyCollectionEnumeratorIEnumeratorCurrent");
        if (keyCollectEnumerIEnumerCurrent.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the property Current in Collections IEnumerator 1");
        try
        {
            Dictionary<TestClass, TestClass> dic = new Dictionary<TestClass, TestClass>();
            TestClass Tkey1 = new TestClass();
            TestClass TVal1 = new TestClass();
            dic.Add(Tkey1, TVal1);
            IEnumerator keyEnumerIEnumer = (IEnumerator)new Dictionary<TestClass,TestClass>.KeyCollection(dic).GetEnumerator();
            while (keyEnumerIEnumer.MoveNext())
            {
                if (!keyEnumerIEnumer.Current.Equals(Tkey1))
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Return the property Current in Collections IEnumerator 2");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("1", "test1");
            IEnumerator keyEnumerIEnumer = (IEnumerator)new Dictionary<string, string>.KeyCollection(dic).GetEnumerator();
            while (keyEnumerIEnumer.MoveNext())
            {
                if (keyEnumerIEnumer.Current.ToString() != "1")
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