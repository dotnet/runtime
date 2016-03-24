// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Dictionary.Enumerator.MoveNext()
/// </summary>
public class DictionaryEnumeratorMoveNext
{
    public static int Main()
    {
        DictionaryEnumeratorMoveNext dicEnumMoveNext = new DictionaryEnumeratorMoveNext();
        TestLibrary.TestFramework.BeginTestCase("DictionaryEnumeratorMoveNext");
        if (dicEnumMoveNext.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Invoke the MoveNext method of in the Dictionary Enumerator 1");
        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("str1", "helloworld");
            Dictionary<string, string>.Enumerator enumer = dictionary.GetEnumerator();
            bool boolVal = enumer.MoveNext();
            if (!boolVal)
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
        TestLibrary.TestFramework.BeginScenario("Invoke the MoveNext method of in the Dictionary Enumerator 2");
        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("str1", "helloworld");
            Dictionary<string, string>.Enumerator enumer = dictionary.GetEnumerator();
            bool boolVal1 = enumer.MoveNext();
            bool boolVal2 = enumer.MoveNext();
            if (boolVal1 != true || boolVal2 != false)
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is not the ActualResult");
                retVal = false;
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
        TestLibrary.TestFramework.BeginScenario("Invoke the MoveNext method of in the Dictionary Enumerator 3");
        try
        {
            Dictionary<TestClass,TestClass> dictionary = new Dictionary<TestClass,TestClass>();
            TestClass TKey1 = new TestClass();
            TestClass TVal1 = new TestClass();
            dictionary.Add(TKey1,TVal1);
            Dictionary<TestClass,TestClass>.Enumerator enumer = dictionary.GetEnumerator();
            bool boolVal1 = enumer.MoveNext();
            bool boolVal2 = enumer.MoveNext();
            if (boolVal1 != true || boolVal2 != false)
            {
                TestLibrary.TestFramework.LogError("005", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("The collection was modified after the enumerator was created");
        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("str1", "helloworld");
            Dictionary<string, string>.Enumerator enumer = dictionary.GetEnumerator();
            bool boolVal1 = enumer.MoveNext();
            dictionary.Add("str2", "goodluck");
            bool boolVal2 = enumer.MoveNext();
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
    public class TestClass
    {
    }
    #endregion
}
