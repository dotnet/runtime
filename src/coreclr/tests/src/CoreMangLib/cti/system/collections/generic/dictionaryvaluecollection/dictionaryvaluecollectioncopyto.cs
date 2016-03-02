// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
/// <summary>
/// Dictionary.ValueCollection.CopyTo(Array,Int32)
/// </summary>
public class DictionaryValueCollectionCopyTo
{
    private const int SIZE = 10;
    public static int Main()
    {
        DictionaryValueCollectionCopyTo valuecollectCopyTo = new DictionaryValueCollectionCopyTo();
        TestLibrary.TestFramework.BeginTestCase("DictionaryValueCollectionCopyTo");
        if (valuecollectCopyTo.RunTests())
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
        retVal = PosTest3() && retVal;
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the method CopyTo in the ValueCollection 1");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("str1", "Test1");
            dic.Add("str2", "Test2");
            Dictionary<string, string>.ValueCollection values = new Dictionary<string, string>.ValueCollection(dic);
            string[] TVals = new string[SIZE];
            values.CopyTo(TVals, 0);
            string strVals = null;
            for (int i = 0; i < TVals.Length; i++)
            {
                if (TVals[i] != null)
                {
                    strVals += TVals[i].ToString();
                }
            }
            if (TVals[0].ToString() != "Test1" || TVals[1].ToString() != "Test2" || strVals != "Test1Test2")
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Invoke the method CopyTo in the ValueCollection 2");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("str1", "Test1");
            dic.Add("str2", "Test2");
            Dictionary<string, string>.ValueCollection values = new Dictionary<string, string>.ValueCollection(dic);
            string[] TVals = new string[SIZE];
            values.CopyTo(TVals, 5);
            string strVals = null;
            for (int i = 0; i < TVals.Length; i++)
            {
                if (TVals[i] != null)
                {
                    strVals += TVals[i].ToString();
                }
            }
            if (TVals[5].ToString() != "Test1" || TVals[6].ToString() != "Test2" || strVals != "Test1Test2")
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
    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3:Invoke the method CopyTo in the ValueCollection 3");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            Dictionary<string, string>.ValueCollection values = new Dictionary<string, string>.ValueCollection(dic);
            string[] TVals = new string[SIZE];
            values.CopyTo(TVals, 0);
            for (int i = 0; i < TVals.Length; i++)
            {
                if (TVals[i] != null)
                {
                    TestLibrary.TestFramework.LogError("005", "the ExpecResult is not the ActualResult");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:The argument array is null");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            Dictionary<string, string>.ValueCollection values = new Dictionary<string, string>.ValueCollection(dic);
            string[] TVals = null;
            values.CopyTo(TVals, 0);
            TestLibrary.TestFramework.LogError("N001", "The argument array is null but not throw exception");
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
    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2:The argument index is less than zero");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            Dictionary<string, string>.ValueCollection values = new Dictionary<string, string>.ValueCollection(dic);
            string[] TVals = new string[SIZE];
            int index = -1;
            values.CopyTo(TVals, index);
            TestLibrary.TestFramework.LogError("N003", "The argument index is less than zero but not throw exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest3:The argument index is larger than array length");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("str1", "Test1");
            Dictionary<string, string>.ValueCollection values = new Dictionary<string, string>.ValueCollection(dic);
            string[] TVals = new string[SIZE];
            int index = SIZE + 1;
            values.CopyTo(TVals, index);
            TestLibrary.TestFramework.LogError("N005", "The argument index is larger than array length but not throw exception");
            retVal = false;
        }
        catch (ArgumentException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest4:The number of elements in the source Dictionary.ValueCollection is greater than the available space from index to the end of the destination array");
        try
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("str1", "Test1");
            dic.Add("str1", "Test1");
            Dictionary<string, string>.ValueCollection values = new Dictionary<string, string>.ValueCollection(dic);
            string[] TVals = new string[SIZE];
            int index = SIZE - 1;
            values.CopyTo(TVals, index);
            TestLibrary.TestFramework.LogError("N007", "The ExpectResult should throw exception but the ActualResult not throw exception");
            retVal = false;
        }
        catch (ArgumentException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}