using System;
using System.Security;

public class MulticastDelegateCtor
{
    #region Public Method
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
        retVal = PosTest9() && retVal;
        retVal = PosTest10() && retVal;
        retVal = PosTest11() && retVal;
        retVal = PosTest12() && retVal;
        retVal = PosTest13() && retVal;
        retVal = PosTest14() && retVal;
        retVal = PosTest15() && retVal;
        retVal = PosTest16() && retVal;
        retVal = PosTest17() && retVal;
        retVal = PosTest18() && retVal;
        
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
        
        TestLibrary.TestFramework.BeginScenario("PosTest1: Return type is void, parameter is value");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(dd.TestValueParameterVoidCallback);

            if (null == dd.ValueParameterVoidDelegate)
            {
                TestLibrary.TestFramework.LogError("001", "Failed to assign a call back function to a delegate");
                retVal = false;
            }

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

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Return type is void, parameter is a reference value");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ReferenceParameterVoidDelegate =
                new ReferenceParameterVoidDelegate(dd.TestReferenceParameterVoidCallback);
            if (null == dd.ReferenceParameterVoidDelegate)
            {
                TestLibrary.TestFramework.LogError("003", "Failed to assign a call back function to a delegate");
                retVal = false;
            }

            dd.ReferenceParameterVoidDelegate(DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }        

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        
        TestLibrary.TestFramework.BeginScenario("PosTest3: Return type is void, parameter is void");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.VoidParameterVoidDelegate =
                new VoidParameterVoidDelegate(dd.TestVoidParameterVoidCallback);
            if (null == dd.VoidParameterVoidDelegate)
            {
                TestLibrary.TestFramework.LogError("005", "Failed to assign a call back function to a delegate");
                retVal = false;
            }

            dd.VoidParameterVoidDelegate();

            if ((DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER + 1)
                != dd.VoidParameterVoidDelegateTestValue)
            {
                TestLibrary.TestFramework.LogError("006", "Failed to call a delegate");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        
        TestLibrary.TestFramework.BeginScenario("PosTest4: Return type is a value type, parameter is a value type");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterValueDelegate =
                new ValueParameterValueDelegate(dd.TestValueParameterValueCallback);

            if (null == dd.ValueParameterValueDelegate)
            {
                TestLibrary.TestFramework.LogError("008", "Failed to assign a call back function to a delegate");
                retVal = false;
            }

            int returnObject = dd.ValueParameterValueDelegate(DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
            if (DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER != returnObject)
            {
                TestLibrary.TestFramework.LogError("009", "Incorrect delegate return value: " + returnObject);
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

    public bool PosTest5()
    {
        bool retVal = true;
        
        TestLibrary.TestFramework.BeginScenario("PosTest5: Return type is reference type, with a reference parameter");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ReferenceParameterReferenceDelegate =
                new ReferenceParameterReferenceDelegate(dd.TestReferenceParameterReferenceCallback);
            if (null == dd.ReferenceParameterReferenceDelegate)
            {
                TestLibrary.TestFramework.LogError("011", "Failed to assign a call back function to a delegate");
                retVal = false;
            }

            object returnObject =
                dd.ReferenceParameterReferenceDelegate(DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER);
            if ( !returnObject.Equals(DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER) )
            {
                TestLibrary.TestFramework.LogError("012", "Incorrect delegate return value: " + returnObject);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("013", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Return type is reference type, with a value type parameter");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterReferenceDelegate =
                new ValueParameterReferenceDelegate(dd.TestValueParameterReferenceCallback);
            if (null == dd.ValueParameterReferenceDelegate)
            {
                TestLibrary.TestFramework.LogError("014", "Failed to assign a call back function to a delegate");
                retVal = false;
            }

            object returnObject =
                dd.ValueParameterReferenceDelegate(DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
            if ( !returnObject.Equals(DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER) )
            {
                TestLibrary.TestFramework.LogError("015", "Incorrect delegate return value: " + returnObject);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest7:  Return type is a value type, with a reference parameter");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ReferenceParameterValueDelegate =
                new ReferenceParameterValueDelegate(dd.TestReferenceParameterValueCallback);
            if (null == dd.ReferenceParameterValueDelegate)
            {
                TestLibrary.TestFramework.LogError("017", "Failed to assign a call back function to a delegate");
                retVal = false;
            }

            int returnObject =
                dd.ReferenceParameterValueDelegate(DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER);
            if (DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER != returnObject)
            {
                TestLibrary.TestFramework.LogError("018", "Incorrect delegate return value: " + returnObject);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("019", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest8: Return value is a value type, without parameter");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.VoidParameterValueDelegate =
                new VoidParameterValueDelegate(dd.TestVoidParameterValueCallback);
            if (null == dd.VoidParameterValueDelegate)
            {
                TestLibrary.TestFramework.LogError("020", "Failed to assign a call back function to a delegate");
                retVal = false;
            }

            int returnObject =
                dd.VoidParameterValueDelegate();
            if (DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER != returnObject)
            {
                TestLibrary.TestFramework.LogError("021", "Incorrect delegate return value: " + returnObject);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("022", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest9()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest9: Return value is a reference type, without parameter");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.VoidParameterReferenceDelegate =
                new VoidParameterReferenceDelegate(dd.TestVoidParameterReferenceCallback);
            if (null == dd.VoidParameterReferenceDelegate)
            {
                TestLibrary.TestFramework.LogError("023", "Failed to assign a call back function to a delegate");
                retVal = false;
            }

            object returnObject = dd.VoidParameterReferenceDelegate();
            if (!returnObject.Equals(DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER))
            {
                TestLibrary.TestFramework.LogError("024", "Incorrect delegate return value: " + returnObject);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("025", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest10()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest10: Return value is void, with 2 value parameters");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.TwoValueParameterVoidDelegate =
                new TwoValueParameterVoidDelegate(dd.TestTwoValueParameterVoidCallback);
            if (null == dd.TwoValueParameterVoidDelegate)
            {
                TestLibrary.TestFramework.LogError("026", "Failed to assign a call back function to a delegate");
                retVal = false;
            }

            dd.TwoValueParameterVoidDelegate(
                DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER,
                DelegateDefinitions.c_DELEGATE_TEST_ADDITIONAL_VALUE_PARAMETER);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("027", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest11()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest11: Return value is void, with 1 value parameter and 1 reference parameter");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueReferenceParameterVoidDelegate =
                new ValueReferenceParameterVoidDelegate(dd.TestValueReferenceParameterVoidCallback);
            if (null == dd.ValueReferenceParameterVoidDelegate)
            {
                TestLibrary.TestFramework.LogError("028", "Failed to assign a call back function to a delegate");
                retVal = false;
            }

            dd.ValueReferenceParameterVoidDelegate(
                DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER,
                DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("029", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest12()
    {
        bool retVal = true;
        
        TestLibrary.TestFramework.BeginScenario("PosTest12: Return value is void, with 1 reference parameter and 1 value parameter");
        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ReferenceValueParameterVoidDelegate =
                new ReferenceValueParameterVoidDelegate(dd.TestReferenceValueParameterVoidCallback);
            if (null == dd.ReferenceValueParameterVoidDelegate)
            {
                TestLibrary.TestFramework.LogError("030", "Failed to assign a call back function to a delegate");
                retVal = false;
            }

            dd.ReferenceValueParameterVoidDelegate(
                DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER,
                DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("031", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest13()
    {
        bool retVal = true;      
        TestLibrary.TestFramework.BeginScenario("PosTest13: Return value is a reference type, with 1 reference parameter and 1 value parameter");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ReferenceValueParameterReferenceDelegate =
                new ReferenceValueParameterReferenceDelegate(dd.TestReferenceValueParameterReferenceCallback);
            if (null == dd.ReferenceValueParameterReferenceDelegate)
            {
                TestLibrary.TestFramework.LogError("031", "Failed to assign a call back function to a delegate");
                retVal = false;
            }

            object returnObject = dd.ReferenceValueParameterReferenceDelegate(
                DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER,
                DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);

            if (!returnObject.Equals(DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER))
            {
                TestLibrary.TestFramework.LogError("032", "Incorrect delegate return value: " + returnObject);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("033", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest14()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest14: Return value is void, with 1 value parameter and then set a static function");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueParameterVoidDelegate =
                new ValueParameterVoidDelegate(DelegateDefinitions.TestValueParameterVoidStaticCallback);
            if (null == dd.ValueParameterVoidDelegate)
            {
                TestLibrary.TestFramework.LogError("034", "Failed to assign a call back function to a delegate");
                retVal = false;
            }

            dd.ValueParameterVoidDelegate(
                DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("035", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest15()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest15: Return value is a reference type, with 1 reference type parameter and 1 value type parameter, then set a static function");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ReferenceValueParameterReferenceDelegate =
                new ReferenceValueParameterReferenceDelegate(DelegateDefinitions.TestReferenceValueParameterReferenceStaticCallback);
            if (null == dd.ReferenceValueParameterReferenceDelegate)
            {
                TestLibrary.TestFramework.LogError("036", "Failed to assign a call back function to a delegate");
                retVal = false;
            }

            object returnObject = dd.ReferenceValueParameterReferenceDelegate(
                DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER,
                DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);

            if (!returnObject.Equals(DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER))
            {
                TestLibrary.TestFramework.LogError("037", "Incorrect delegate return value: " + returnObject);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("038", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest16()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest16: Initialize a delegate with an anonymous function");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ReferenceValueParameterReferenceDelegate = delegate(object val1, int val2)
            {
                if (!val1.Equals(DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER))
                    VerificationAgent.ThrowVerificationException(
                        "First input reference parameter is not expected",
                        val1,
                        DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER);

                if (DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER != val2)
                    VerificationAgent.ThrowVerificationException(
                        "Second input value parameter is not expected",
                        val2,
                        DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);

                return DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER;
            };

            if (null == dd.ReferenceValueParameterReferenceDelegate)
            {
                TestLibrary.TestFramework.LogError("039", "Failed to assign a call back function to a delegate");
                retVal = false;
            }

            object returnObject = dd.ReferenceValueParameterReferenceDelegate(
                DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER,
                DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);

            if (!returnObject.Equals(DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER))
            {
                TestLibrary.TestFramework.LogError("040", "Incorrect delegate return value: " + returnObject);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("041", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest17()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest17: Initialize a delegate with an private instance method");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ReferenceValueParameterReferenceDelegate =
                new ReferenceValueParameterReferenceDelegate(TestReferenceValueParameterReferencePrivateCallback);

            if (null == dd.ReferenceValueParameterReferenceDelegate)
            {
                TestLibrary.TestFramework.LogError("042", "Failed to assign a private call back function to a delegate");
                retVal = false;
            }

            object returnObject = dd.ReferenceValueParameterReferenceDelegate(
                DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER,
                DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);

            if (!returnObject.Equals(DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER))
            {
                TestLibrary.TestFramework.LogError("043", "Incorrect delegate return value: " + returnObject);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("044", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    [SecuritySafeCritical]
    public bool PosTest18()
    {
        bool retVal = true;
		if (TestLibrary.Utilities.IsWindows)
		{
        TestLibrary.TestFramework.BeginScenario("PosTest18: Initialize a delegate with an P/Invoke method");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.VoidParameterValueDelegate =
                new VoidParameterValueDelegate(DelegateDefinitions.GetCurrentProcessId);

            if (null == dd.VoidParameterValueDelegate)
            {
                TestLibrary.TestFramework.LogError("045", "Failed to assign a P/Invoke call back function to a delegate");
                retVal = false;
            }

            int returnObject = dd.VoidParameterValueDelegate();

            // Suppress unused variable compiler warning.
            if (returnObject == 0)
            {
                TestLibrary.TestFramework.LogError("046", "Incorrect delegate return value: " + returnObject);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("047", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
		}
        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Return type is void, parameter is a reference value");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ReferenceParameterVoidDelegate =
                new ReferenceParameterVoidDelegate(dd.TestReferenceParameterVoidCallbackWithNullValue);

            dd.ReferenceParameterVoidDelegate(null);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("048", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Return type is reference, parameter is a reference value");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ReferenceParameterReferenceDelegate =
                new ReferenceParameterReferenceDelegate(dd.TestReferenceParameterReferenceCallbackWithNullValue);

            dd.ReferenceParameterReferenceDelegate(null);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("049", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: Return type is void with 1 value type parameter and one reference type parameter");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ValueReferenceParameterVoidDelegate =
                new ValueReferenceParameterVoidDelegate(dd.TestValueReferenceParameterVoidCallbackWithNullValue);

            dd.ValueReferenceParameterVoidDelegate(DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER, null);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("050", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;
        
        TestLibrary.TestFramework.BeginScenario("NegTest4: Return type is void with 1 reference type parameter and 1 value  type parameter");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ReferenceValueParameterVoidDelegate =
                new ReferenceValueParameterVoidDelegate(dd.TestReferenceValueParameterVoidCallbackWithNullValue);

            dd.ReferenceValueParameterVoidDelegate(null, DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("051", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;
        
        TestLibrary.TestFramework.BeginScenario("NegTest5: Return type is a reference type with 1 reference type parameter and 1 value  type parameter");

        try
        {
            DelegateDefinitions dd = new DelegateDefinitions();
            dd.ReferenceValueParameterReferenceDelegate =
                new ReferenceValueParameterReferenceDelegate(dd.TestReferenceValueParameterReferenceCallbackWithNullValue);

            dd.ReferenceValueParameterReferenceDelegate(null, DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("052", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        MulticastDelegateCtor mdc = new MulticastDelegateCtor();

        TestLibrary.TestFramework.BeginTestCase("MulticastDelegateCtor");

        if (mdc.RunTests())
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
    private object TestReferenceValueParameterReferencePrivateCallback(object val1, int val2)
    {
        if (!val1.Equals(DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER))
            VerificationAgent.ThrowVerificationException(
                "First input reference parameter is not expected",
                val1,
                DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER);

        if (DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER != val2)
            VerificationAgent.ThrowVerificationException(
                "Second input value parameter is not expected",
                val2,
                DelegateDefinitions.c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);

        return DelegateDefinitions.c_DELEGATE_TEST_DEFAUTL_REFERENCE_PARAMETER;
    }
    #endregion
}
