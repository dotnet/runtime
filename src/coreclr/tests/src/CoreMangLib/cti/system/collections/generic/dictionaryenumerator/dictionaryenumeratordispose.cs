// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Dictionary.Enumerator.Dispose()
/// </summary>

public class DictionaryEnumeratorDispose
{
    
    public static int Main()
    {
        DictionaryEnumeratorDispose dicEnumDis = new DictionaryEnumeratorDispose();
        TestLibrary.TestFramework.BeginTestCase("DictionaryEnumeratorDispose");
        if (dicEnumDis.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the Dispose method in Dictionary Enumerator 1");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("1", "test1");
            dic.Add("2", "test2");
            Dictionary<string, string>.Enumerator enumer1 = new Dictionary<string, string>.Enumerator();
            Dictionary<string, string>.Enumerator enumer2 = dic.GetEnumerator();
            enumer2.Dispose();
            enumer1.Dispose();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2:Invoke the Dispose method in Dictionary Enumerator 2");
        try
        {
            myClass TKey = new myClass();
            myClass TValue = new myClass();
            Dictionary<myClass, myClass> dic = new Dictionary<myClass, myClass>();
            dic.Add(TKey,TValue );
            Dictionary<myClass, myClass>.Enumerator enumer1 = new Dictionary<myClass, myClass>.Enumerator();
            Dictionary<myClass, myClass>.Enumerator enumer2 = dic.GetEnumerator();
            enumer2.Dispose();
            enumer1.Dispose();
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
    public class myClass { }
    #endregion
}
