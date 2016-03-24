// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;
/// <summary>
/// PreserveSigAttribute.ctor() [v-minch]
/// </summary>
public class PreserveSigAttributeCtor
{
    public static int Main()
    {
        PreserveSigAttributeCtor test = new PreserveSigAttributeCtor();
        TestLibrary.TestFramework.BeginTestCase("PreserveSigAttribute.ctor()");
        if (test.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Initialize a instance of PreserveSigAttribute class");
        try
        {
            PreserveSigAttribute myInstance = new PreserveSigAttribute();
            if (myInstance == null)
            {
                TestLibrary.TestFramework.LogError("001", "the instance of PreserveSigAttribute creating failed");
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