// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;

/// <summary>
/// MethodImplOptions.NoInlining(v-yaduoj)
/// </summary>
public class MethodImplOptionsTest
{
    private enum MyMethodImplOptions
    {
        Unmanaged = System.Reflection.MethodImplAttributes.NoInlining,
        ForwardRef = System.Reflection.MethodImplAttributes.NoInlining,
        PreserveSig = System.Reflection.MethodImplAttributes.NoInlining,
        InternalCall = System.Reflection.MethodImplAttributes.InternalCall,
        Synchronized = System.Reflection.MethodImplAttributes.Synchronized,
        NoInlining = System.Reflection.MethodImplAttributes.NoInlining,
    }

    public static int Main()
    {
        MethodImplOptionsTest testObj = new MethodImplOptionsTest();

        TestLibrary.TestFramework.BeginTestCase("for Enumeration: MethodImplOptions.NoInlining");
        if (testObj.RunTests())
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

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: method implementation options is NoInlining";
        string errorDesc;

        int expectedValue;
        int actualValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedValue = (int)MyMethodImplOptions.NoInlining;
            actualValue = (int)MethodImplOptions.NoInlining;
            if (actualValue != expectedValue)
            {
                errorDesc = "NoInlining value of MethodImplOptions is not the value " + expectedValue +
                            "as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
