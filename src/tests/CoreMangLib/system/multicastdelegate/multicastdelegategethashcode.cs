// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Security;
using Xunit;

public class MulticastDelegateGetHashCode
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases

    [SecuritySafeCritical]
    public bool PosTest1()
    {
        bool retVal = true;
        
        TestLibrary.TestFramework.BeginScenario("PosTest1: Hash code of two delegate with the same P/Invoke function from same type are equal");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.VoidParameterValueDelegate =
                new VoidParameterValueDelegate(DelegateDefinitions.GetCurrentProcessId);
            DelegateDefinitions dd1 = new DelegateDefinitions();
            dd1.VoidParameterValueDelegate =
                new VoidParameterValueDelegate(DelegateDefinitions.GetCurrentProcessId);

            if (dd.VoidParameterValueDelegate.GetHashCode() != dd1.VoidParameterValueDelegate.GetHashCode())
            {
                TestLibrary.TestFramework.LogError("001", "Hash code of two delegate with the same P/Invoke function from same type are not equal");
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

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Hash code of two delegate with the same instance function from same type are equal");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(dd.TestValueParameterVoidCallback);

            DelegateDefinitions dd1 = new DelegateDefinitions();
            dd1.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(dd.TestValueParameterVoidCallback);

            if (dd.ValueParameterVoidDelegate.GetHashCode() != dd1.ValueParameterVoidDelegate.GetHashCode())
            {
                TestLibrary.TestFramework.LogError("003", "Hash code of two delegate with the same instance function from same type are not equal");
                retVal = false;
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
    #endregion

    [Fact]
    public static int TestEntryPoint()
    {
        MulticastDelegateGetHashCode mdghc = new MulticastDelegateGetHashCode();

        TestLibrary.TestFramework.BeginTestCase("MulticastDelegateGetHashCode");

        if (mdghc.RunTests())
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
