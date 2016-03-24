// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;
/// <summary>
/// InAttribute.ctor()
/// </summary>
public class InAttributeCtor
{
    public static int Main()
    {
        InAttributeCtor test = new InAttributeCtor();
        TestLibrary.TestFramework.BeginTestCase("InAttribute.Ctor");
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Initialize a instance of InAttribute class");
        try
        {
            InAttribute myInAttribute = new InAttribute();
            if (myInAttribute == null)
            {
                TestLibrary.TestFramework.LogError("001", "Initialize the instance of InAttribute not successfully");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}