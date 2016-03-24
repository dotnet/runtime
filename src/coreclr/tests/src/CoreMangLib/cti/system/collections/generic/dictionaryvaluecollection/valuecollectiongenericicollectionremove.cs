// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
/// <summary>
/// ValueCollection.System.Collections.Generic.ICollection.Remove()
/// </summary>
public class ValueCollectionGenericICollectionRemove
{
    public static int Main()
    {
        ValueCollectionGenericICollectionRemove valCollectGenericRemove = new ValueCollectionGenericICollectionRemove();
        TestLibrary.TestFramework.BeginTestCase("ValueCollectionGenericICollectionRemove");
        if (valCollectGenericRemove.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("NegTest1:Invoke the method Remove in ValueCollection ICollection 1");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("str1", "Test1");
            Dictionary<string, string>.ValueCollection valueCollect = new Dictionary<string, string>.ValueCollection(dic);
            ICollection<string> icollect = (ICollection<string>)valueCollect;
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