// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
/// <summary>
/// ValueCollection.System.Collections.Generic.ICollection.IsReadOnly
/// </summary>
public class ValueCollectionGenericICollectionIsReadOnly
{
    public static int Main()
    {
        ValueCollectionGenericICollectionIsReadOnly valCollectGenericIsReadOnly = new ValueCollectionGenericICollectionIsReadOnly();
        TestLibrary.TestFramework.BeginTestCase("ValueCollectionGenericICollectionIsReadOnly");
        if (valCollectGenericIsReadOnly.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the property IsReadOnly in the ValueCollection Generic ICollection 1");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("str1", "Test1");
            ICollection<string> icollect = (ICollection<string>)new Dictionary<string, string>.ValueCollection(dic);
            if (!icollect.IsReadOnly)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Return the property IsReadOnly in the ValueCollection Generic ICollection 2");
        try
        {
            Dictionary<TestClass, TestClass> dic = new Dictionary<TestClass, TestClass>();
            ICollection<TestClass> icollect = (ICollection<TestClass>)new Dictionary<TestClass, TestClass>.ValueCollection(dic);
            if (!icollect.IsReadOnly)
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