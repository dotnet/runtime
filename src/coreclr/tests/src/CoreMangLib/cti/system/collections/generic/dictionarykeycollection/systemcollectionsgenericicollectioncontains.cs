// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
/// <summary>
/// Dictionary.KeyCollection.System.Collections.Generic.ICollection.Contains()
/// </summary>
public class SystemCollectionsGenericICollectionContains
{
    public static int Main()
    {
        SystemCollectionsGenericICollectionContains keycollectICollectionContains = new SystemCollectionsGenericICollectionContains();
        TestLibrary.TestFramework.BeginTestCase("SystemCollectionsGenericICollectionContains");
        if (keycollectICollectionContains.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the method Contains in ICollection 1");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("str1", "Test1");
            dic.Add("str2", "Test2");
            Dictionary<string, string>.KeyCollection keyCollect = new Dictionary<string, string>.KeyCollection(dic);
            ICollection<string> icollect = (ICollection<string>)keyCollect;
            if (!icollect.Contains("str1") || !icollect.Contains("str2") || icollect.Contains("str3"))
            {
                TestLibrary.TestFramework.LogError("001", "The ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Invoke the method Contains in ICollection 2");
        try
        {
            Dictionary<TestClass, TestClass> dic = new Dictionary<TestClass, TestClass>();
            TestClass Tkey1 = new TestClass();
            TestClass Tval1 = new TestClass();
            dic.Add(Tkey1,Tval1);
            Dictionary<TestClass, TestClass>.KeyCollection keyCollect = new Dictionary<TestClass, TestClass>.KeyCollection(dic);
            ICollection<TestClass> icollect = (ICollection<TestClass>)keyCollect;
            if (!icollect.Contains(Tkey1) || icollect.Contains(Tval1))
            {
                TestLibrary.TestFramework.LogError("001", "The ExpectResult is not the ActualResult");
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
    #endregion
    #region ForTestClass
    public class TestClass { }
    #endregion
}