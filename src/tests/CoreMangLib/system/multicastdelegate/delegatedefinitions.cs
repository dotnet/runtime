// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.InteropServices; // for DllImportAttribute

#region Delegates Definitions
public delegate void ValueParameterVoidDelegate(int val);

public delegate void ReferenceParameterVoidDelegate(object val);

public delegate void VoidParameterVoidDelegate();

public delegate int ValueParameterValueDelegate(int val);

public delegate object ReferenceParameterReferenceDelegate(object val);

public delegate object ValueParameterReferenceDelegate(int val);

public delegate int ReferenceParameterValueDelegate(object val);

public delegate int VoidParameterValueDelegate();

public delegate object VoidParameterReferenceDelegate();

public delegate void TwoValueParameterVoidDelegate(int val1, int val2);

public delegate void ValueReferenceParameterVoidDelegate(int val1, object val2);

public delegate void ReferenceValueParameterVoidDelegate(object val1, int val2);

public delegate object ReferenceValueParameterReferenceDelegate(object val1, int val2);
#endregion

public class DelegateDefinitions
{
    #region Public Constaints
    public const int c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER = 1;
    public const int c_DELEGATE_TEST_ADDITIONAL_VALUE_PARAMETER = 2;
    public const string c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER = "abcdefg";
    #endregion

    #region Public Properties
    public ValueParameterVoidDelegate ValueParameterVoidDelegate
    {
        get
        {
            return m_ValueParameterVoidDelegate;
        }
        set
        {
            m_ValueParameterVoidDelegate = value;
        }
    }

    public ReferenceParameterVoidDelegate ReferenceParameterVoidDelegate
    {
        get
        {
            return m_ReferenceParameterVoidDelegate;
        }
        set
        {
            m_ReferenceParameterVoidDelegate = value;
        }
    }

    public VoidParameterVoidDelegate VoidParameterVoidDelegate
    {
        get
        {
            return m_VoidParameterVoidDelegate;
        }
        set
        {
            m_VoidParameterVoidDelegate = value;
        }
    }

    public ValueParameterValueDelegate ValueParameterValueDelegate
    {
        get
        {
            return m_ValueParameterValueDelegate;
        }
        set
        {
            m_ValueParameterValueDelegate = value;
        }
    }

    public ReferenceParameterReferenceDelegate ReferenceParameterReferenceDelegate
    {
        get
        {
            return m_ReferenceParameterReferenceDelegate;
        }
        set
        {
            m_ReferenceParameterReferenceDelegate = value;
        }
    }

    public ValueParameterReferenceDelegate ValueParameterReferenceDelegate
    {
        get
        {
            return m_ValueParameterReferenceDelegate;
        }
        set
        {
            m_ValueParameterReferenceDelegate = value;
        }
    }

    public ReferenceParameterValueDelegate ReferenceParameterValueDelegate
    {
        get
        {
            return m_ReferenceParameterValueDelegate;
        }
        set
        {
            m_ReferenceParameterValueDelegate = value;
        }
    }

    public VoidParameterValueDelegate VoidParameterValueDelegate
    {
        get
        {
            return m_VoidParameterValueDelegate;
        }
        set
        {
            m_VoidParameterValueDelegate = value;
        }
    }

    public VoidParameterReferenceDelegate VoidParameterReferenceDelegate
    {
        get
        {
            return m_VoidParameterReferenceDelegate;
        }
        set
        {
            m_VoidParameterReferenceDelegate = value;
        }
    }

    public TwoValueParameterVoidDelegate TwoValueParameterVoidDelegate
    {
        get
        {
            return m_TwoValueParameterVoidDelegate;
        }
        set
        {
            m_TwoValueParameterVoidDelegate = value;
        }
    }

    public ValueReferenceParameterVoidDelegate ValueReferenceParameterVoidDelegate
    {
        get
        {
            return m_ValueReferenceParameterVoidDelegate;
        }
        set
        {
            m_ValueReferenceParameterVoidDelegate = value;
        }
    }

    public ReferenceValueParameterVoidDelegate ReferenceValueParameterVoidDelegate
    {
        get
        {
            return m_ReferenceValueParameterVoidDelegate;
        }
        set
        {
            m_ReferenceValueParameterVoidDelegate = value;
        }
    }

    public ReferenceValueParameterReferenceDelegate ReferenceValueParameterReferenceDelegate
    {
        get
        {
            return m_ReferenceValueParameterReferenceDelegate;
        }
        set
        {
            m_ReferenceValueParameterReferenceDelegate = value;
        }
    }

    public int VoidParameterVoidDelegateTestValue
    {
        get
        {
            return m_VoidParameterVoidDelegateTestValue;
        }
    }
    #endregion

    #region Private Properties
    private ValueParameterVoidDelegate m_ValueParameterVoidDelegate;
    private ReferenceParameterVoidDelegate m_ReferenceParameterVoidDelegate;
    private VoidParameterVoidDelegate m_VoidParameterVoidDelegate;
    private ValueParameterValueDelegate m_ValueParameterValueDelegate;
    private ReferenceParameterReferenceDelegate m_ReferenceParameterReferenceDelegate;
    private ValueParameterReferenceDelegate m_ValueParameterReferenceDelegate;
    private ReferenceParameterValueDelegate m_ReferenceParameterValueDelegate;
    private VoidParameterValueDelegate m_VoidParameterValueDelegate;
    private VoidParameterReferenceDelegate m_VoidParameterReferenceDelegate;
    private TwoValueParameterVoidDelegate m_TwoValueParameterVoidDelegate;
    private ValueReferenceParameterVoidDelegate m_ValueReferenceParameterVoidDelegate;
    private ReferenceValueParameterVoidDelegate m_ReferenceValueParameterVoidDelegate;
    private ReferenceValueParameterReferenceDelegate m_ReferenceValueParameterReferenceDelegate;

    private int m_VoidParameterVoidDelegateTestValue = c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER;
    #endregion

    #region Public Static Properties
    public static int VoidParameterVoidDelegateStaticTestValue = c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER;
    #endregion

    #region Public Static Methods
    // Need P/Invoke be tested?
    [DllImport("kernel32.dll")]
    public static extern int GetCurrentProcessId();

    [DllImport("kernel32.dll")]
    public static extern int GetCurrentThreadId();

    public static void TestValueParameterVoidStaticCallback(int val)
    {
        if (val != c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER)
            VerificationAgent.ThrowVerificationException(
                "Input value parameter is not expected",
                val,
                c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
    }

    public static void TestReferenceParameterVoidStaticCallback(object val)
    {
        if (!val.Equals(c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER))
            VerificationAgent.ThrowVerificationException(
                "Input reference parameter is not expected",
                val,
                c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER);
    }

    public static void TestVoidParameterVoidStaticCallback()
    {
        // increment a global value to ensure this call back function is called.
        VoidParameterVoidDelegateStaticTestValue++;
    }

    public static int TestVoidParameterValueStaticCallback()
    {
        return c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER;
    }

    public static int TestValueParameterValueStaticCallback(int val)
    {
        if (val != c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER)
            VerificationAgent.ThrowVerificationException(
                "Input value parameter is not expected",
                val,
                c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);

        return c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER;
    }

    public static object TestReferenceValueParameterReferenceStaticCallback(object val1, int val2)
    {
        if (!val1.Equals(c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER))
            VerificationAgent.ThrowVerificationException(
                "First input reference parameter is not expected",
                val1,
                c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER);

        if (c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER != val2)
            VerificationAgent.ThrowVerificationException(
                "Second input value parameter is not expected",
                val2,
                c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);

        return c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER;
    }
    #endregion

    #region Public Methods
    #region Positive Test Callback Methods
    public void TestValueParameterVoidCallback(int val)
    {
        if (val != c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER)
            VerificationAgent.ThrowVerificationException(
                "Input value parameter is not expected",
                val,
                c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
    }

    public void TestReferenceParameterVoidCallback(object val)
    {
        if (!val.Equals(c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER))
            VerificationAgent.ThrowVerificationException(
                "Input reference parameter is not expected",
                val,
                c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER);
    }

    public void TestVoidParameterVoidCallback()
    {
        // increment a global value to ensure this call back function is called.
        m_VoidParameterVoidDelegateTestValue++;
    }

    public int TestValueParameterValueCallback(int val)
    {
        if (val != c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER)
            VerificationAgent.ThrowVerificationException(
                "Input value parameter is not expected",
                val,
                c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);

        return c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER;
    }

    public object TestReferenceParameterReferenceCallback(object val)
    {
        if (!val.Equals(c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER))
            VerificationAgent.ThrowVerificationException(
                "Input reference parameter is not expected",
                val,
                c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER);

        return c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER;
    }

    public object TestValueParameterReferenceCallback(int val)
    {
        if (c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER != val)
            VerificationAgent.ThrowVerificationException(
                "Input value parameter is not expected",
                val,
                c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);

        return c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER;
    }

    public int TestReferenceParameterValueCallback(object val)
    {
        if (!val.Equals(c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER))
            VerificationAgent.ThrowVerificationException(
                "Input reference parameter is not expected",
                val,
                c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER);

        return c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER;
    }

    public int TestVoidParameterValueCallback()
    {
        return c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER;
    }

    public object TestVoidParameterReferenceCallback()
    {
        return c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER;
    }

    public void TestTwoValueParameterVoidCallback(int val1, int val2)
    {
        if (c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER != val1)
            VerificationAgent.ThrowVerificationException(
                "First input value parameter is not expected",
                val1,
                c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);

        if (c_DELEGATE_TEST_ADDITIONAL_VALUE_PARAMETER != val2)
            VerificationAgent.ThrowVerificationException(
                "Second input value parameter is not expected",
                val2,
                c_DELEGATE_TEST_ADDITIONAL_VALUE_PARAMETER);
    }

    public void TestValueReferenceParameterVoidCallback(int val1, object val2)
    {
        if (c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER != val1)
            VerificationAgent.ThrowVerificationException(
                "First input value parameter is not expected",
                val1,
                c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);

        if (!val2.Equals(c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER))
            VerificationAgent.ThrowVerificationException(
                "Second input reference parameter is not expected",
                val2,
                c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER);
    }

    public void TestReferenceValueParameterVoidCallback(object val1, int val2)
    {
        if (!val1.Equals(c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER))
            VerificationAgent.ThrowVerificationException(
                "First input reference parameter is not expected",
                val1,
                c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER);

        if (c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER != val2)
            VerificationAgent.ThrowVerificationException(
                "Second input value parameter is not expected",
                val2,
                c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
    }

    public object TestReferenceValueParameterReferenceCallback(object val1, int val2)
    {
        if (!val1.Equals(c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER))
            VerificationAgent.ThrowVerificationException(
                "First input reference parameter is not expected",
                val1,
                c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER);

        if (c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER != val2)
            VerificationAgent.ThrowVerificationException(
                "Second input value parameter is not expected",
                val2,
                c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);

        return c_DELEGATE_TEST_DEFAULT_REFERENCE_PARAMETER;
    }
    #endregion

    #region Nagative Test Callback Methods
    public void TestReferenceParameterVoidCallbackWithNullValue(object val)
    {
        if (null != val)
            VerificationAgent.ThrowVerificationException(
                "Input reference parameter is not expected",
                val,
                "null");
    }

    public object TestReferenceParameterReferenceCallbackWithNullValue(object val)
    {
        if (null != val)
            VerificationAgent.ThrowVerificationException(
                "Input reference parameter is not expected",
                val,
                "null");

        return null;
    }

    public void TestValueReferenceParameterVoidCallbackWithNullValue(int val1, object val2)
    {
        if (c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER != val1)
            VerificationAgent.ThrowVerificationException(
                "First input value parameter is not expected",
                val1,
                c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);

        if (null != val2)
            VerificationAgent.ThrowVerificationException(
                "Second input reference parameter is not expected",
                val2,
                "null");
    }

    public void TestReferenceValueParameterVoidCallbackWithNullValue(object val1, int val2)
    {
        if (null != val1)
            VerificationAgent.ThrowVerificationException(
                "First input reference parameter is not expected",
                val1,
                "null");

        if (c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER != val2)
            VerificationAgent.ThrowVerificationException(
                "Second input value parameter is not expected",
                val2,
                c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);
    }

    public object TestReferenceValueParameterReferenceCallbackWithNullValue(object val1, int val2)
    {
        if (null != val1)
            VerificationAgent.ThrowVerificationException(
                "First input reference parameter is not expected",
                val1,
                "null");

        if (c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER != val2)
            VerificationAgent.ThrowVerificationException(
                "Second input value parameter is not expected",
                val2,
                c_DELEGATE_TEST_DEFAULT_VALUE_PARAMETER);

        return null;
    }
    #endregion
    #endregion
}
