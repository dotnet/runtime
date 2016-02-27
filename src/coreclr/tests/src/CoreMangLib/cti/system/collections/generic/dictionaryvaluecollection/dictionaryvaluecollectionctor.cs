// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
/// <summary>
/// Dictionary.ValueCollection.ctor(Dictionay<TKey,TValue>)
/// </summary>
public class DictionaryValueCollectionctor
{
    public static int Main()
    {
        DictionaryValueCollectionctor valueCollectCtor = new DictionaryValueCollectionctor();
        TestLibrary.TestFramework.BeginTestCase("DictionaryValueCollectionctor");
        if (valueCollectCtor.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Initialize the generic class ValueCollection 1");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            Dictionary<string, string>.ValueCollection valueCollect = new Dictionary<string, string>.ValueCollection(dic);
            if (valueCollect == null)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Initialize the generic class ValueCollection 2");
        try
        {
            Dictionary<TestClass, TestClass> dic = new Dictionary<TestClass, TestClass>();
            Dictionary<TestClass, TestClass>.ValueCollection valueCollect = new Dictionary<TestClass, TestClass>.ValueCollection(dic);
            if (valueCollect == null)
            {
                TestLibrary.TestFramework.LogError("003", "The ExpectResult is not the ActualResult");
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
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:the argument dictionary is null");
        try
        {
            Dictionary<object, object>.ValueCollection valueCollect = new Dictionary<object, object>.ValueCollection(null);
            TestLibrary.TestFramework.LogError("N001", "the argument dictionary is null but not throw exception");
            retVal = false;
        }
        catch (ArgumentNullException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region ForTestClass
    public class TestClass
    {
    }
    #endregion
}