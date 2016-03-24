// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections;
/// <summary>
/// DictionaryEnumerator.IEnumerator.get_Current
/// </summary>
public class DictionaryEnumeratorIEnumeratorget_Current
{
    public static int Main()
    {
        DictionaryEnumeratorIEnumeratorget_Current dicEnumIDicEnumget_Current = new DictionaryEnumeratorIEnumeratorget_Current();
        TestLibrary.TestFramework.BeginTestCase("DictionaryEnumeratorIEnumeratorget_Current");
        if (dicEnumIDicEnumget_Current.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Return the property get_Current in the IEnumerator 1");
        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("str1", "helloworld");
            Dictionary<string, string>.Enumerator enumer = dictionary.GetEnumerator();
            IEnumerator iEnumer = (IEnumerator)enumer;
            while (iEnumer.MoveNext())
            {
                object objCurrent = iEnumer.Current;
                KeyValuePair<string, string> keyVal = new KeyValuePair<string, string>("str1", "helloworld");
                if (!objCurrent.Equals(keyVal))
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
        TestLibrary.TestFramework.BeginScenario("Return the property get_Current in the IEnumerator 2");
        try
        {
            Dictionary<TestClass, TestClass> dictionary = new Dictionary<TestClass, TestClass>();
            TestClass Tkey1 = new TestClass();
            TestClass TVal1 = new TestClass();
            dictionary.Add(Tkey1, TVal1);
            Dictionary<TestClass, TestClass>.Enumerator enumer = dictionary.GetEnumerator();
            IEnumerator iEnumer = (IEnumerator)enumer;
            while(iEnumer.MoveNext())
            {
                object objCurrent = iEnumer.Current;
                KeyValuePair<TestClass, TestClass> keyVal = new KeyValuePair<TestClass, TestClass>(Tkey1, TVal1);
                if (!objCurrent.Equals(keyVal))
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
        TestLibrary.TestFramework.BeginScenario("NegTest1:The enumerator is positioned before the first element of the collection");
        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("str1", "helloworld");
            Dictionary<string, string>.Enumerator enumer = dictionary.GetEnumerator();
            IEnumerator iEnumer = (IEnumerator)enumer;
            object objCurrent = iEnumer.Current;
            TestLibrary.TestFramework.LogError("N001", "The enumerator is positioned before the first element of the collection but not throw exception");
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
    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2:The enumerator is positioned after the last element of the collection");
        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("str1", "helloworld");
            Dictionary<string, string>.Enumerator enumer = dictionary.GetEnumerator();
            IEnumerator iEnumer = (IEnumerator)enumer;
            while (iEnumer.MoveNext()) { }
            object objCurrent = iEnumer.Current;
            TestLibrary.TestFramework.LogError("N003", "The enumerator is positioned after the last element of the collection but not throw exception");
            retVal = false;   
        }
        catch (InvalidOperationException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region ForTestClass
    public class TestClass
    {

    }
    #endregion
}