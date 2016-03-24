// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
/// <summary>
/// System.Collections.Generic.Dictionary.KeyCollection.Enumerator.MoveNext()
/// </summary>
public class DictionaryKeyCollectionEnumeratorMoveNext
{
    public static int Main()
    {
        DictionaryKeyCollectionEnumeratorMoveNext dicKeyCollectEnumMoveNext = new DictionaryKeyCollectionEnumeratorMoveNext();
        TestLibrary.TestFramework.BeginTestCase("DictionaryKeyCollectionEnumeratorMoveNext");
        if (dicKeyCollectEnumMoveNext.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the MoveNext method in Dictionary KeyCollection Enumerator");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("1", "test1");
            Dictionary<string, string>.KeyCollection.Enumerator keyEnumer1 = new Dictionary<string, string>.KeyCollection(dic).GetEnumerator();
            if (keyEnumer1.Current != null || keyEnumer1.MoveNext() != true)
            {
                TestLibrary.TestFramework.LogError("001.1", "The ExpectResult is not the ActualResult");
                retVal = false;
            }
            if (keyEnumer1.MoveNext())
            {
                TestLibrary.TestFramework.LogError("001.2", "The method MoveNext should return false but it did not");
            }
            keyEnumer1.Dispose();
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
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("1", "test1");
            Dictionary<string, string>.KeyCollection.Enumerator keyEnumer = new Dictionary<string, string>.KeyCollection(dic).GetEnumerator();
            dic.Add("2", "test2");
            bool boolVal = keyEnumer.MoveNext();
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