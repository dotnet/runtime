// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;
using System;
using System.IO;

[SecuritySafeCritical]
public class StreamReaderNull
{
    public static int Main()
    {
        StreamReaderNull srNull = new StreamReaderNull();
        TestLibrary.TestFramework.BeginTestCase("StreamReaderNull");
        if (srNull.RunTests())
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
        //retVal = PosTest3() && retVal;may be a bug
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Null StreamReader is not null");
        try
        {
            if (StreamReader.Null == null)
            {
                TestLibrary.TestFramework.LogError("001", "The ExpectResult is not ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect Exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Null StreamReader Invoke ReadLine method");
        try
        {
            if (StreamReader.Null.ReadLine() != null)
            {
                TestLibrary.TestFramework.LogError("003", "The ExpectResult is not ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect Exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Null StreamReader Invoke some other methods");
        try
        {
            if (StreamReader.Null.Peek() != 0)
            {
                TestLibrary.TestFramework.LogError("005.1", "The ExpectResult is not ActualResult");
                retVal = false;
            }
            if (StreamReader.Null.Read() != 0)
            {
                TestLibrary.TestFramework.LogError("005.2", "The ExpectResult is not ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpect Exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: Null StreamReader Invoke ReadBlock method");
        try
        {
            char[] buffer = new char[] { 'a', 'b', 'c' };
            int index = 0;
            int count = buffer.Length;
            if (StreamReader.Null.ReadBlock(buffer,index,count) != 0)
            {
                TestLibrary.TestFramework.LogError("007", "The ExpectResult is not ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpect Exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5: Null StreamReader Invoke ToString method");
        try
        {
            if (StreamReader.Null.ToString() != "System.IO.StreamReader+NullStreamReader")
            {
                TestLibrary.TestFramework.LogError("009", "The ExpectResult is not ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpect Exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}