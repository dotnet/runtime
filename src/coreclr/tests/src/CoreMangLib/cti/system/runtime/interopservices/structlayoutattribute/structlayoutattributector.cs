// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;
/// <summary>
/// StructLayoutAttribute.ctor(LayoutKind) [v-minch]
/// </summary>
public class StructLayoutAttributeCtor
{
    public static int Main()
    {
        StructLayoutAttributeCtor test = new StructLayoutAttributeCtor();
        TestLibrary.TestFramework.BeginTestCase("StructLayoutAttribute.ctor()");
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Initialize a instance of StructLayoutAttribute class");
        try
        {
            LayoutKind mylayoutkind = LayoutKind.Auto;
            StructLayoutAttribute myInstance = new StructLayoutAttribute(mylayoutkind);
            if (myInstance == null || myInstance.Value != mylayoutkind)
            {
                TestLibrary.TestFramework.LogError("001", "the instance of StructLayoutAttribute creating failed");
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