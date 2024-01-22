// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Security;
using Xunit;

public class MulticastDelegateGetInvocationList
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NagTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases

    [SecuritySafeCritical]
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call GetInvocationList against a delegate with one function");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.VoidParameterValueDelegate =
                new VoidParameterValueDelegate(DelegateDefinitions.GetCurrentProcessId);
            Delegate[] invocationList = dd.VoidParameterValueDelegate.GetInvocationList();

            if (invocationList.Length != 1)
            {
                TestLibrary.TestFramework.LogError("001", "Call GetInvocationList against a delegate with one function returns wrong result: " + invocationList.Length);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    [SecuritySafeCritical]
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the function order of the returned invocation list");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.VoidParameterValueDelegate =
                new VoidParameterValueDelegate(DelegateDefinitions.GetCurrentProcessId);
            dd.VoidParameterValueDelegate +=
                new VoidParameterValueDelegate(DelegateDefinitions.TestVoidParameterValueStaticCallback);
            dd.VoidParameterValueDelegate +=
                new VoidParameterValueDelegate(dd.TestVoidParameterValueCallback);
            Delegate[] invocationList = dd.VoidParameterValueDelegate.GetInvocationList();

            if (invocationList.Length != 3)
            {
                TestLibrary.TestFramework.LogError("003", "Call GetInvocationList against a delegate with multiple functions returns wrong result: " + invocationList.Length);
                retVal = false;
            }

            // Hack: I don't know how to iterater through the invocation list to verify the invocation order
            // is correct. I have to hard code the desired invocation order
            //if (invocationList[0].Method.Name != "GetCurrentProcessId" ||    ***** MikeRou 8/3: Method's getter is not supported
            //     invocationList[1].Method.Name != "TestVoidParameterValueStaticCallback" ||
            //     invocationList[2].Method.Name != "TestVoidParameterValueCallback")
            //{
            //    TestLibrary.TestFramework.LogError("004", "Invocation order of the returned invocation list is wrong");
            //    retVal = false;
            //}

            // Test the invocation list can be invoked
            for (int i = 0; i < invocationList.Length; ++i)
            {
                // invocationList[i].Method.Invoke(invocationList[i].Target, null); ***** MikeRou 8/3: Method's getter is not supported
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Negative Test Cases

    [SecuritySafeCritical]
    public bool NagTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NagTest1: Insert multiple functions with null embedded in the function list");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.VoidParameterValueDelegate =
                new VoidParameterValueDelegate(DelegateDefinitions.GetCurrentProcessId);
            dd.VoidParameterValueDelegate += null;
            dd.VoidParameterValueDelegate +=
                new VoidParameterValueDelegate(DelegateDefinitions.TestVoidParameterValueStaticCallback);
            dd.VoidParameterValueDelegate +=
                new VoidParameterValueDelegate(dd.TestVoidParameterValueCallback);
            Delegate[] invocationList = dd.VoidParameterValueDelegate.GetInvocationList();

            if (invocationList.Length != 3)
            {
                TestLibrary.TestFramework.LogError("101", "Call GetInvocationList against a delegate with one function returns wrong result: " + invocationList.Length);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    [Fact]
    public static int TestEntryPoint()
    {
        MulticastDelegateGetInvocationList mdgil = new MulticastDelegateGetInvocationList();

        TestLibrary.TestFramework.BeginTestCase("MulticastDelegateGetInvocationList");

        if (mdgil.RunTests())
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
}
