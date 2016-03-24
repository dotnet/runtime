// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections;
/// <summary>
/// Dictionary.Enumerator.Current
/// </summary>
public class DictionaryEnumeratorCurrent
{
    public static int Main()
    {
        DictionaryEnumeratorCurrent dicEnumCurrent = new DictionaryEnumeratorCurrent();
        TestLibrary.TestFramework.BeginTestCase("DictionaryEnumeratorCurrent");
        if (dicEnumCurrent.RunTests())
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
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Return the property Current of in the Dictionary Enumerator 1");
        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("str1", "helloworld");
            Dictionary<string, string>.Enumerator enumer = dictionary.GetEnumerator();
            KeyValuePair<string, string> keyVal = enumer.Current;
            if (keyVal.Key != null || keyVal.Value != null)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is not the ActualResult");
                retVal = false;
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
        TestLibrary.TestFramework.BeginScenario("Return the property Current of in the Dictionary Enumerator 2");
        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("str1", "helloworld");
            Dictionary<string, string>.Enumerator enumer = dictionary.GetEnumerator();
            if (enumer.MoveNext())
            {
                KeyValuePair<string, string> keyVal = enumer.Current;
                if (keyVal.Key != "str1" || keyVal.Value != "helloworld")
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
    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Return the property Current of in the Dictionary Enumerator 3");
        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("str1", "helloworld");
            dictionary.Add("str2", "goodluck");
            Dictionary<string, string>.Enumerator enumer = dictionary.GetEnumerator();
            if (enumer.MoveNext())
            {
                KeyValuePair<string, string> keyVal = enumer.Current;
                if (enumer.MoveNext())
                {
                    KeyValuePair<string, string> keyVal2 = enumer.Current;
                    if (keyVal2.Key != "str2" || keyVal2.Value != "goodluck")
                    {
                        TestLibrary.TestFramework.LogError("005", "the ExpectResult is not the ActualResult");
                        retVal = false;
                    }
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Return the property Current of in the Dictionary Enumerator 4");
        try
        {
            TestClass TKey1 = new TestClass();
            TestClass TValue1 = new TestClass();
            Dictionary<TestClass,TestClass> dictionary = new Dictionary<TestClass,TestClass>();
            dictionary.Add(TKey1,TValue1);
            Dictionary<TestClass,TestClass>.Enumerator enumer = dictionary.GetEnumerator();
            if (enumer.MoveNext())
            {
                KeyValuePair<TestClass,TestClass> keyVal = enumer.Current;
                if (keyVal.Key != TKey1 || keyVal.Value != TValue1)
                {
                    TestLibrary.TestFramework.LogError("007", "the ExpectResult is not the ActualResult");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpect exception:" + e);
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
