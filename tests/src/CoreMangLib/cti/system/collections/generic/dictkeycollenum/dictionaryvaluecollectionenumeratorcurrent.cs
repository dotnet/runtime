// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
/// <summary>
/// System.Collections.Generic.Dictionary.ValueCollection.Enumerator.Current
/// </summary>
public class DictionaryValueCollectionEnumeratorCurrent
{
    public static int Main()
    {
        DictionaryValueCollectionEnumeratorCurrent dicValCollectEnumCurrent = new DictionaryValueCollectionEnumeratorCurrent();
        TestLibrary.TestFramework.BeginTestCase("DictionaryValueCollectionEnumeratorCurrent");
        if (dicValCollectEnumCurrent.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the property Current in Dictionary ValueCollection Enumerator 1");
        try
        {
            Dictionary<string, string>.ValueCollection.Enumerator valEnumer = new Dictionary<string, string>.ValueCollection.Enumerator();
            if (valEnumer.Current != null)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
            valEnumer.Dispose();
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Return the property Current in Dictionary ValueCollection Enumerator 2");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("1", "test1");
            Dictionary<string, string>.ValueCollection.Enumerator valEnumer = new Dictionary<string, string>.ValueCollection(dic).GetEnumerator();
            while (valEnumer.MoveNext())
            {
                if (valEnumer.Current != "test1")
                {
                    TestLibrary.TestFramework.LogError("003", "the ExpectResult is not the ActualResult");
                    retVal = false;
                }
            }
            valEnumer.Dispose();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}