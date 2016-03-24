// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
/// <summary>
/// System.Collections.Generic.Dictionary.ValueCollection.Enumerator.Dispose()
/// </summary>
public class DictionaryValueCollectionEnumeratorDispose
{
    public static int Main()
    {
        DictionaryValueCollectionEnumeratorDispose dicValCollectEnumDis = new DictionaryValueCollectionEnumeratorDispose();
        TestLibrary.TestFramework.BeginTestCase("DictionaryValueCollectionEnumeratorDispose");
        if (dicValCollectEnumDis.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the Dispose method in Dictionary ValueCollection Enumerator 1");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("1", "test1");
            dic.Add("2", "test2");
            Dictionary<string, string>.ValueCollection.Enumerator ValEnumer1 = new Dictionary<string, string>.ValueCollection.Enumerator();
            Dictionary<string, string>.ValueCollection.Enumerator ValEnumer2 = new Dictionary<string, string>.ValueCollection(dic).GetEnumerator();
            ValEnumer2.Dispose();
            ValEnumer1.Dispose();
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