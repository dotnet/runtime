// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
/// <summary>
/// System.Collections.Generic.Dictionary.KeyCollection.Enumerator.Dispose()
/// </summary>
public class DictionaryKeyCollectionEnumeratorDispose
{
    public static int Main()
    {
        DictionaryKeyCollectionEnumeratorDispose dicKeyCollectEnumDis = new DictionaryKeyCollectionEnumeratorDispose();
        TestLibrary.TestFramework.BeginTestCase("DictionaryKeyCollectionEnumeratorDispose");
        if (dicKeyCollectEnumDis.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the Dispose method in Dictionary KeyCollection Enumerator 1");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("1", "test1");
            dic.Add("2", "test2");
            Dictionary<string, string>.KeyCollection.Enumerator keyEnumer1 = new Dictionary<string, string>.KeyCollection.Enumerator();
            Dictionary<string, string>.KeyCollection.Enumerator keyEnumer2 = new Dictionary<string,string>.KeyCollection(dic).GetEnumerator();
            keyEnumer2.Dispose();
            keyEnumer1.Dispose();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}