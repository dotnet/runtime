// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// System.IndexOutOfRangeException.ctor() [v-minch]
/// </summary>
public class IndexOutOfRangeExceptionctor1
{
    public static int Main()
    {
        IndexOutOfRangeExceptionctor1 indexOutOfRangeExceptionctor1 = new IndexOutOfRangeExceptionctor1();
        TestLibrary.TestFramework.BeginTestCase("IndexOutOfRangeExceptionctor1");
        if (indexOutOfRangeExceptionctor1.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Initialize the IndexOutOfRangeException instance");
        try
        {
            IndexOutOfRangeException myException = new IndexOutOfRangeException();
            if (myException == null)
            {
                TestLibrary.TestFramework.LogError("001", "the IndexOutOfRangeException instance creating failed");
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
}