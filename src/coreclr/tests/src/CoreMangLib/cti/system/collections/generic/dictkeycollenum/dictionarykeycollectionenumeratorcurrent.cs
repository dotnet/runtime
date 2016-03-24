// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
/// <summary>
/// System.Collections.Generic.Dictionary.KeyCollection.Enumerator.Current
/// </summary>
public class DictionaryKeyCollectionEnumeratorCurrent
{
    public static int Main()
    {
        DictionaryKeyCollectionEnumeratorCurrent dicKeyCollectEnumCurrent = new DictionaryKeyCollectionEnumeratorCurrent();
        TestLibrary.TestFramework.BeginTestCase("DictionaryKeyCollectionEnumeratorCurrent");
        if (dicKeyCollectEnumCurrent.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the property Current in Dictionary KeyCollection Enumerator 1");
        try
        {
            Dictionary<string, string>.KeyCollection.Enumerator keyEnumer = new Dictionary<string, string>.KeyCollection.Enumerator();
            if (keyEnumer.Current != null)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
            keyEnumer.Dispose();
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Return the property Current in Dictionary KeyCollection Enumerator 2");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("1", "test1");
            Dictionary<string, string>.KeyCollection.Enumerator keyEnumer = new Dictionary<string, string>.KeyCollection(dic).GetEnumerator();
            while (keyEnumer.MoveNext())
            {
                if (keyEnumer.Current != "1")
                {
                    TestLibrary.TestFramework.LogError("003", "the ExpectResult is not the ActualResult");
                    retVal = false;
                }
            }
            keyEnumer.Dispose();
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