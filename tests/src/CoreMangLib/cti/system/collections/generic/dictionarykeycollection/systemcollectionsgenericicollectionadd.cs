// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
/// <summary>
/// Dictionary.KeyCollection.System.Collections.Generic.ICollection.Add(TKey)
/// </summary>
public class SystemCollectionsGenericICollectionAdd
{
    public static int Main()
    {
        SystemCollectionsGenericICollectionAdd keycollectICollectionAdd = new SystemCollectionsGenericICollectionAdd();
        TestLibrary.TestFramework.BeginTestCase("SystemCollectionsGenericICollectionAdd");
        if (keycollectICollectionAdd.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:Invoke the method Add in ICollection");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            Dictionary<string, string>.KeyCollection keyCollect = new Dictionary<string, string>.KeyCollection(dic);
            ICollection<string> icollect = (ICollection<string>)keyCollect;
            icollect.Add("str1");
            TestLibrary.TestFramework.LogError("N001", "The ExpectResult should throw exception but the ActualResult not throw exception");
            retVal = false;
        }
        catch (NotSupportedException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}