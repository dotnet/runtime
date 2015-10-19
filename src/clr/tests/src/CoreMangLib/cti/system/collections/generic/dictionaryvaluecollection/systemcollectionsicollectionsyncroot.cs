using System;
using System.Collections.Generic;
using System.Collections;
/// <summary>
/// Dictionary.ValueCollection.System.Collections.ICollection.SyncRoot
/// </summary>
public class ValueCollectionICollectionSyncRoot
{
    public static int Main()
    {
        ValueCollectionICollectionSyncRoot valCollectSyncRoot = new ValueCollectionICollectionSyncRoot();
        TestLibrary.TestFramework.BeginTestCase("ValueCollectionICollectionSyncRoot");
        if (valCollectSyncRoot.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the property SyncRoot in the ValueCollection ICollection 1");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("str1", "Test1");
            ICollection icollect = (ICollection)new Dictionary<string, string>.ValueCollection(dic);
            object objVal = icollect.SyncRoot;
            if (objVal == null)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Return the property SyncRoot in the ValueCollection ICollection 2");
        try
        {
            Dictionary<TestClass, TestClass> dic = new Dictionary<TestClass, TestClass>();
            ICollection icollect = (ICollection)new Dictionary<TestClass, TestClass>.ValueCollection(dic);
            object objVal = icollect.SyncRoot;
            if (objVal == null)
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
