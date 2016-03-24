// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;
/// <summary>
/// StructLayoutAttribute.CharSet [v-minch]
/// </summary>
public class StructLayoutAttributeCharSet
{
    public static int Main()
    {
        StructLayoutAttributeCharSet test = new StructLayoutAttributeCharSet();
        TestLibrary.TestFramework.BeginTestCase("StructLayoutAttribute.CharSet()");
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
        retVal = PosTest2() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Return the field CharSet value in StructLayoutAttribute class 1");
        try
        {
            LayoutKind mylayoutkind = LayoutKind.Auto;
            StructLayoutAttribute myInstance = new StructLayoutAttribute(mylayoutkind);
            if (myInstance.CharSet != 0)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is 0 but the ActualResult is " + myInstance.CharSet.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Return the field CharSet value in StructLayoutAttribute class 2");
        try
        {
            LayoutKind mylayoutkind = LayoutKind.Sequential;
            StructLayoutAttribute myInstance = new StructLayoutAttribute(mylayoutkind);
            if (myInstance.CharSet != 0)
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is 0 but the ActualResult is " + myInstance.CharSet.ToString());
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