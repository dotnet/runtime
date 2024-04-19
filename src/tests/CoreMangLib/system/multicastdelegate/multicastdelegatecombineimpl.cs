// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

public class MulticastDelegateCombineImpl
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
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }

    public void TestValueParameterVoidCallback(int val)
    {
        if (val != DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER)
            VerificationAgent.ThrowVerificationException(
                "Input value parameter is not expected",
                val,
                DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;
        
        TestLibrary.TestFramework.BeginScenario("PosTest1: Combine two function from different type to a delegate");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(dd.TestValueParameterVoidCallback);
            dd.ValueParameterVoidDelegate +=
                new ValueParameterVoidDelegate(TestValueParameterVoidCallback);

            dd.ValueParameterVoidDelegate(DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Combine two function from same type to a delegate");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(TestValueParameterVoidCallback1);
            dd.ValueParameterVoidDelegate +=
                new ValueParameterVoidDelegate(TestValueParameterVoidCallback);

            dd.ValueParameterVoidDelegate(DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Combine a instance method and a class method from same type to a delegate");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(DelegateDefinitions.TestValueParameterVoidStaticCallback);
            dd.ValueParameterVoidDelegate +=
                new ValueParameterVoidDelegate(dd.TestValueParameterVoidCallback);

            dd.ValueParameterVoidDelegate(DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Combine a public instance method and a public class method from different type to a delegate");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(DelegateDefinitions.TestValueParameterVoidStaticCallback);
            dd.ValueParameterVoidDelegate +=
                new ValueParameterVoidDelegate(TestValueParameterVoidCallback);

            dd.ValueParameterVoidDelegate(DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Combine a private instance method and a private class method from the same type to a delegate");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(MulticastDelegateCombineImpl.TestValueParameterVoidStaticCallback);
            dd.ValueParameterVoidDelegate +=
                new ValueParameterVoidDelegate(TestValueParameterVoidCallback1);

            dd.ValueParameterVoidDelegate(DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Combine two static methods from different class");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(MulticastDelegateCombineImpl.TestValueParameterVoidStaticCallback);
            dd.ValueParameterVoidDelegate +=
                new ValueParameterVoidDelegate(DelegateDefinitions.TestValueParameterVoidStaticCallback);

            dd.ValueParameterVoidDelegate(DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest7: Combine two anonymous methods");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            int i = DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER;    

            dd.ValueParameterValueDelegate = delegate(int val)
            {
                if (val != DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER)
                    VerificationAgent.ThrowVerificationException(
                        "Input value parameter is not expected",
                        val,
                        DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);

                // Test the call order of multicast delegate
                i++;

                return DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER;
            };
            dd.ValueParameterValueDelegate += delegate(int val)
            {
                if (val != DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER)
                    VerificationAgent.ThrowVerificationException(
                        "Input value parameter is not expected",
                        val,
                        DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);

                if (i != DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER + 1)
                    VerificationAgent.ThrowVerificationException(
                        "Value of out variable of anonymous method is not expected",
                        DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER + 1,
                        i);

                return DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER;
            };

            int returnObject = dd.ValueParameterValueDelegate(DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
            if (returnObject != DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER)
            {
                TestLibrary.TestFramework.LogError("007", "Incorrect delegate return value: " + returnObject);
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

    public bool PosTest8()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest8: Combine two multifunctions delegates");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.VoidParameterVoidDelegate =
                new VoidParameterVoidDelegate(DelegateDefinitions.TestVoidParameterVoidStaticCallback);
            dd.VoidParameterVoidDelegate +=
                new VoidParameterVoidDelegate(dd.TestVoidParameterVoidCallback);

            DelegateDefinitions dd1 = new DelegateDefinitions();
            dd1.VoidParameterVoidDelegate =
                new VoidParameterVoidDelegate(DelegateDefinitions.TestVoidParameterVoidStaticCallback);
            dd1.VoidParameterVoidDelegate +=
                new VoidParameterVoidDelegate(dd1.TestVoidParameterVoidCallback);

            dd.VoidParameterVoidDelegate = (VoidParameterVoidDelegate)MulticastDelegate.Combine(dd.VoidParameterVoidDelegate, dd1.VoidParameterVoidDelegate);

            dd.VoidParameterVoidDelegate();

            if (dd.VoidParameterVoidDelegateTestValue != DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER + 1)
            {
                TestLibrary.TestFramework.LogError("009", "Combined delegate does not work: " + dd.VoidParameterVoidDelegateTestValue);
                retVal = false;
            }

            if (dd1.VoidParameterVoidDelegateTestValue != DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER + 1)
            {
                TestLibrary.TestFramework.LogError("010", "Combined delegate does not work: " + dd1.VoidParameterVoidDelegateTestValue);
                retVal = false;
            }

            if (DelegateDefinitions.VoidParameterVoidDelegateStaticTestValue != DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER + 2)
            {
                TestLibrary.TestFramework.LogError("011", "Combined delegate does not work: " + DelegateDefinitions.VoidParameterVoidDelegateStaticTestValue);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        finally
        {
            // reset the value of static variable
            DelegateDefinitions.VoidParameterVoidDelegateStaticTestValue = 0;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Combine the same method twice");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(dd.TestValueParameterVoidCallback);
            dd.ValueParameterVoidDelegate +=
                new ValueParameterVoidDelegate(dd.TestValueParameterVoidCallback);

            dd.ValueParameterVoidDelegate(DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Combine the null value");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(dd.TestValueParameterVoidCallback);
            dd.ValueParameterVoidDelegate += null;
            dd.ValueParameterVoidDelegate +=
                new ValueParameterVoidDelegate(TestValueParameterVoidCallback);

            dd.ValueParameterVoidDelegate(DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: Return value of second method is different with first method");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.VoidParameterValueDelegate = delegate
            {
                return DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER;
            };
            dd.VoidParameterValueDelegate += delegate
            {
                return DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER + 1;
            };

            int returnObject = dd.VoidParameterValueDelegate();
            if (returnObject != DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER + 1)
            {
                TestLibrary.TestFramework.LogError("103", "Incorrect delegate return value: " + returnObject);
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
    #endregion
    #endregion

    #region Private Methods
    private void TestValueParameterVoidCallback1(int val)
    {
        if (val != DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER)
            VerificationAgent.ThrowVerificationException(
                "Input value parameter is not expected",
                val,
                DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
    }
    #endregion

    #region Private Static Methods
    private static void TestValueParameterVoidStaticCallback(int val)
    {
        if (val != DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER)
            VerificationAgent.ThrowVerificationException(
                "Input value parameter is not expected",
                val,
                DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
    }
    #endregion

    [Fact]
    public static int TestEntryPoint()
    {
        MulticastDelegateCombineImpl mdci = new MulticastDelegateCombineImpl();

        TestLibrary.TestFramework.BeginTestCase("MulticastDelegateCombineImpl");

        if (mdci.RunTests())
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
