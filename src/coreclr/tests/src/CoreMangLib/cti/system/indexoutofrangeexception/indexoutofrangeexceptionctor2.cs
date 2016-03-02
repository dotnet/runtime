// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.IndexOutOfRangeException.ctor(string) [v-minch]
/// </summary>
public class IndexOutOfRangeExceptionctor2
{
    public static int Main()
    {
        IndexOutOfRangeExceptionctor2 indexOutOfRangeExceptionctor2 = new IndexOutOfRangeExceptionctor2();
        TestLibrary.TestFramework.BeginTestCase("IndexOutOfRangeExceptionctor2");
        if (indexOutOfRangeExceptionctor2.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Initialize the IndexOutOfRangeException instance with message 1");
        try
        {
            string message = "HelloWorld";
            IndexOutOfRangeException myException = new IndexOutOfRangeException(message);
            if (myException == null || myException.Message != message)
            {
                TestLibrary.TestFramework.LogError("001", "the IndexOutOfRangeException with message instance creating failed");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Initialize the IndexOutOfRangeException instance with message 2");
        try
        {
            string message = null;
            IndexOutOfRangeException myException = new IndexOutOfRangeException(message);
            if (myException == null || myException.Message == null)
            {
                TestLibrary.TestFramework.LogError("003", "Initialize the IndexOutOfRangeException instance with null message not create");
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
}

