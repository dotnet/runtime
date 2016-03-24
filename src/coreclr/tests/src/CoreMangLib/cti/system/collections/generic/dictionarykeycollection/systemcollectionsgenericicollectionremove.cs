// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
/// <summary>
/// Dictionary.KeyCollection.System.Collections.Generic.ICollection.Remove(TKey)
/// </summary>
public class SystemCollectionsGenericICollectionRemove
{
    public static int Main()
    {
        SystemCollectionsGenericICollectionRemove keycollectICollectionRemove = new SystemCollectionsGenericICollectionRemove();
        TestLibrary.TestFramework.BeginTestCase("SystemCollectionsGenericICollectionRemove");
        if (keycollectICollectionRemove.RunTests())
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
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:Invoke the method Remove in ICollection 1");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("str1", "Test1");
            Dictionary<string, string>.KeyCollection keyCollect = new Dictionary<string, string>.KeyCollection(dic);
            ICollection<string> icollect = (ICollection<string>)keyCollect;
            bool boolVal = icollect.Remove("str1");
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