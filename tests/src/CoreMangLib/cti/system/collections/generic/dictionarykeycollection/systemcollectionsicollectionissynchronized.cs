// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections;
/// <summary>
/// Dictionary.KeyCollection.System.Collections.ICollection.IsSynchronized
/// </summary>
public class SystemCollectionsICollectionIsSynchronized
{
    public static int Main()
    {
        SystemCollectionsICollectionIsSynchronized keycollectICollectionIsSynchronized = new SystemCollectionsICollectionIsSynchronized();
        TestLibrary.TestFramework.BeginTestCase("SystemCollectionsICollectionIsSynchronized");
        if (keycollectICollectionIsSynchronized.RunTests())
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
        bool retVal = true; ;
        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the property IsSynchronized in the ICollection 1");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("str1", "Test1");
            ICollection icollect = (ICollection)new Dictionary<string, string>.KeyCollection(dic);
            if (icollect.IsSynchronized)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpecResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Return the property IsSynchronized in the ICollection 2");
        try
        {
            Dictionary<TestClass,TestClass> dic = new Dictionary<TestClass,TestClass>();
            ICollection icollect = (ICollection)new Dictionary<TestClass,TestClass>.KeyCollection(dic);
            if (icollect.IsSynchronized)
            {
                TestLibrary.TestFramework.LogError("003", "the ExpecResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region ForTestClass
    public class TestClass { }
    #endregion
}