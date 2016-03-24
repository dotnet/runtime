// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections;
/// <summary>
/// DictionaryEnumerator.IEnumerator.Reset()
/// </summary>
public class DictionaryEnumeratorIEnumeratorReset
{
    public static int Main()
    {
        DictionaryEnumeratorIEnumeratorReset dicEnumIEnumReset = new DictionaryEnumeratorIEnumeratorReset();
        TestLibrary.TestFramework.BeginTestCase("DictionaryEnumeratorIEnumeratorReset");
        if (dicEnumIEnumReset.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("Invoke the method Reset in the IEnumerator 1");
        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("str1", "helloworld");
            dictionary.Add("str2", "goodluck");
            Dictionary<string, string>.Enumerator enumer = dictionary.GetEnumerator();
            IEnumerator iEnumer = (IEnumerator)enumer;
            while (iEnumer.MoveNext()) { }
            iEnumer.Reset();
            if (iEnumer.MoveNext())
            {
                KeyValuePair<string, string> keyVal3 = (KeyValuePair<string, string>)iEnumer.Current;
                if (keyVal3.Key != "str1" || keyVal3.Value != "helloworld")
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
        TestLibrary.TestFramework.BeginScenario("Invoke the method Reset in the IEnumerator 2");
        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("str1", "helloworld");
            Dictionary<string, string>.Enumerator enumer = dictionary.GetEnumerator();
            IEnumerator iEnumer = (IEnumerator)enumer;
            while (iEnumer.MoveNext()) { }
            iEnumer.Reset();
            while (iEnumer.MoveNext())
            {
                KeyValuePair<string, string> keyVal1 = (KeyValuePair<string, string>)iEnumer.Current;
                if (keyVal1.Key != "str1" || keyVal1.Value != "helloworld")
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
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:The collection was modified after the enumerator was created");
        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            Dictionary<string, string>.Enumerator enumer = dictionary.GetEnumerator();
            IEnumerator iEnumer = (IEnumerator)enumer;
            dictionary.Add("str1", "helloworld");
            iEnumer.Reset();
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
}
