// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Security;
using Xunit;

public class MulticastDelegateEquals
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;
        
        TestLibrary.TestFramework.BeginScenario("PosTest1: Determine whether two delegate with the same function from same type are equals");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(dd.TestValueParameterVoidCallback);
            DelegateDefinitions dd1 = new DelegateDefinitions();
            dd1.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(dd.TestValueParameterVoidCallback);

            if (!dd.ValueParameterVoidDelegate.Equals(dd1.ValueParameterVoidDelegate))
            {
                TestLibrary.TestFramework.LogError("001", "Two delegate with the same function from same type are equal");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Determine whether two delegate with the same class method from same type are equal");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(DelegateDefinitions.TestValueParameterVoidStaticCallback);
            DelegateDefinitions dd1 = new DelegateDefinitions();
            dd1.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(DelegateDefinitions.TestValueParameterVoidStaticCallback);

            if (!dd.ValueParameterVoidDelegate.Equals(dd1.ValueParameterVoidDelegate))
            {
                TestLibrary.TestFramework.LogError("003", "Two delegate with the same class method from same type are not equal");
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

    [SecuritySafeCritical]
    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Determine whether two delegate with the same P/Invoke method from same type are equal");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.VoidParameterValueDelegate =
                new VoidParameterValueDelegate(DelegateDefinitions.GetCurrentProcessId);
            DelegateDefinitions dd1 = new DelegateDefinitions();
            dd1.VoidParameterValueDelegate =
                new VoidParameterValueDelegate(DelegateDefinitions.GetCurrentProcessId);

            if (!dd.VoidParameterValueDelegate.Equals(dd1.VoidParameterValueDelegate))
            {
                TestLibrary.TestFramework.LogError("005", "Two delegate with the same P/Invoke method from same type are not equal");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Determine whether an initialized delegate is not equal to null");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(dd.TestValueParameterVoidCallback);

            if (dd.ValueParameterVoidDelegate.Equals(null))
            {
                TestLibrary.TestFramework.LogError("007", "An initiailzed delegate is equal to null");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Determine two delegate with same number of invoke list which is initialized with same instance methods are equal");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(dd.TestValueParameterVoidCallback);
            dd.ValueParameterVoidDelegate +=
                new ValueParameterVoidDelegate(TestValueParameterVoidCallback);

            DelegateDefinitions dd1 = new DelegateDefinitions();
            dd1.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(dd.TestValueParameterVoidCallback);
            dd1.ValueParameterVoidDelegate +=
                new ValueParameterVoidDelegate(TestValueParameterVoidCallback);

            if (!dd.ValueParameterVoidDelegate.Equals(dd1.ValueParameterVoidDelegate))
            {
                TestLibrary.TestFramework.LogError("009", "two delegate with same number of invoke list which is initialized with same instance methods are not equal");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    [SecuritySafeCritical]
    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Determine two delegate with same number of invoke list which is initialized with same multi-type functions are equal");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.VoidParameterValueDelegate =
                new VoidParameterValueDelegate(dd.TestVoidParameterValueCallback);
            dd.VoidParameterValueDelegate +=
                new VoidParameterValueDelegate(DelegateDefinitions.TestVoidParameterValueStaticCallback);
            dd.VoidParameterValueDelegate +=
                new VoidParameterValueDelegate(DelegateDefinitions.GetCurrentProcessId);

            DelegateDefinitions dd1 = new DelegateDefinitions();
            dd1.VoidParameterValueDelegate =
                new VoidParameterValueDelegate(dd.TestVoidParameterValueCallback);
            dd1.VoidParameterValueDelegate +=
                new VoidParameterValueDelegate(DelegateDefinitions.TestVoidParameterValueStaticCallback);
            dd1.VoidParameterValueDelegate +=
                new VoidParameterValueDelegate(DelegateDefinitions.GetCurrentProcessId);

            if (!dd.VoidParameterValueDelegate.Equals(dd1.VoidParameterValueDelegate))
            {
                TestLibrary.TestFramework.LogError("009", "two delegate with same number of invoke list which is initialized with same multi-type functions are not equal");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Determine whether two delegate with different function from same type are not equal");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(TestValueParameterVoidCallback);
            DelegateDefinitions dd1 = new DelegateDefinitions();
            dd1.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(TestValueParameterVoidCallback1);

            if (dd.ValueParameterVoidDelegate.Equals(dd1.ValueParameterVoidDelegate))
            {
                TestLibrary.TestFramework.LogError("101", "Two delegate with different function from same type are equal");
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

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Determine whether two delegate with same name function from different type are not equal");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(dd.TestValueParameterVoidCallback);
            DelegateDefinitions dd1 = new DelegateDefinitions();
            dd1.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(TestValueParameterVoidCallback);

            if (dd.ValueParameterVoidDelegate.Equals(dd1.ValueParameterVoidDelegate))
            {
                TestLibrary.TestFramework.LogError("103", "Two delegate with the same name function from different type are equal");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    [SecuritySafeCritical]
    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: Determine whether two delegate with different P/Invoke functions are not equal");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.VoidParameterValueDelegate =
                new VoidParameterValueDelegate(DelegateDefinitions.GetCurrentProcessId);
            DelegateDefinitions dd1 = new DelegateDefinitions();
            dd1.VoidParameterValueDelegate =
                new VoidParameterValueDelegate(DelegateDefinitions.GetCurrentThreadId);

            if (dd.VoidParameterValueDelegate.Equals(dd1.VoidParameterValueDelegate))
            {
                TestLibrary.TestFramework.LogError("105", "Two delegate with the different P/Invoke functions are equal");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: Determine whether two delegate with different number of functions are not equal");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(dd.TestValueParameterVoidCallback);
            dd.ValueParameterVoidDelegate +=
                new ValueParameterVoidDelegate(TestValueParameterVoidCallback);

            DelegateDefinitions dd1 = new DelegateDefinitions();
            dd1.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(dd.TestValueParameterVoidCallback);
            dd1.ValueParameterVoidDelegate +=
                new ValueParameterVoidDelegate(TestValueParameterVoidCallback);
            dd1.ValueParameterVoidDelegate +=
                new ValueParameterVoidDelegate(TestValueParameterVoidCallback);

            if (dd.ValueParameterVoidDelegate.Equals(dd1.ValueParameterVoidDelegate))
            {
                TestLibrary.TestFramework.LogError("107", "Two delegate with the different number of functions are equal");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("108", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    [SecuritySafeCritical]
    public bool NegTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest5: Determine whether two delegate with same number of functions but with different order are not equal");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.VoidParameterValueDelegate =
                new VoidParameterValueDelegate(dd.TestVoidParameterValueCallback);
            dd.VoidParameterValueDelegate +=
                new VoidParameterValueDelegate(DelegateDefinitions.TestVoidParameterValueStaticCallback);
            dd.VoidParameterValueDelegate +=
                new VoidParameterValueDelegate(DelegateDefinitions.GetCurrentProcessId);

            DelegateDefinitions dd1 = new DelegateDefinitions();
            dd1.VoidParameterValueDelegate =
                new VoidParameterValueDelegate(dd.TestVoidParameterValueCallback);
            dd1.VoidParameterValueDelegate +=
                new VoidParameterValueDelegate(DelegateDefinitions.GetCurrentProcessId);
            dd1.VoidParameterValueDelegate +=
                new VoidParameterValueDelegate(DelegateDefinitions.TestVoidParameterValueStaticCallback);

            if (dd.VoidParameterValueDelegate.Equals(dd1.VoidParameterValueDelegate))
            {
                TestLibrary.TestFramework.LogError("109", "two delegate with same number of functions but with different order are equal");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("110", "Unexpected exception: " + e);
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
        MulticastDelegateEquals mde = new MulticastDelegateEquals();

        TestLibrary.TestFramework.BeginTestCase("MulticastDelegateEquals");

        if (mde.RunTests())
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

    #region Private Methods
    private void TestValueParameterVoidCallback(int val)
    {
        if (val != DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER)
            VerificationAgent.ThrowVerificationException(
                "Input value parameter is not expected",
                val,
                DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
    }

    private void TestValueParameterVoidCallback1(int val)
    {
    }
    #endregion
}
