// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class TestEmptyClass
{
}

public abstract class TestAbstractClass
{
    public TestAbstractClass()
    {
        x = 1;
        x--;
    }

    public abstract void TestAbstractMethod();

    private int x;
}

public class TestClass : TestAbstractClass
{
    public override void TestAbstractMethod()
    {
    }

    public void TestMethod()
    {
    }
}

public struct TestStruct1
{
    public TestStruct1(int value)
    {
        m_value = value;
    }

    int m_value;
}

public struct TestStruct2
{
    public TestStruct2(int value)
    {
        m_value = value;
    }

    int m_value;
}

public enum TestEnum1
{
    DEFAULT
}

public enum TestEnum2
{
    DEFAULT
}

/// <summary>
/// GetHashCode
/// </summary>
public class RuntimeTypeHandleGetHashCode
{
    #region Private Fields
    private int m_ErrorNo = 0;
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: GetHashCode should return valid hash code");

        try
        {
            retVal = retVal && VerifyHashCode(new TestEmptyClass());
            retVal = retVal && VerifyHashCode(new TestClass());
            retVal = retVal && VerifyHashCode(new TestStruct1());
            retVal = retVal && VerifyHashCode(TestEnum1.DEFAULT);
        }
        catch (Exception e)
        {
            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: GetHashCode should return same hash code for instances from same class");

        try
        {
            retVal = retVal && VerifyHashCode(new TestEmptyClass(), new TestEmptyClass(), true);
            retVal = retVal && VerifyHashCode(new TestClass(), new TestClass(), true);
            retVal = retVal && VerifyHashCode(new TestStruct1(), new TestStruct1(), true);
            retVal = retVal && VerifyHashCode(TestEnum1.DEFAULT, TestEnum1.DEFAULT, true);
        }
        catch (Exception e)
        {
            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: GetHashCode should return different hash code for instances from different classes");

        try
        {
            retVal = retVal && VerifyHashCode(new TestEmptyClass(), new TestClass(), false);
            retVal = retVal && VerifyHashCode(new TestClass(), new TestStruct1(), false);
            retVal = retVal && VerifyHashCode(new TestStruct1(), new TestStruct2(), false);
            retVal = retVal && VerifyHashCode(TestEnum1.DEFAULT, TestEnum2.DEFAULT, false);
            retVal = retVal && VerifyHashCode(TestEnum1.DEFAULT, new TestStruct1(), false);
            retVal = retVal && VerifyHashCode(TestEnum1.DEFAULT, new TestClass(), false);
        }
        catch (Exception e)
        {
            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        RuntimeTypeHandleGetHashCode test = new RuntimeTypeHandleGetHashCode();

        TestLibrary.TestFramework.BeginTestCase("RuntimeTypeHandleGetHashCode");

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

    #region Private Method
    private bool VerifyHashCode(Object o)
    {
        bool retVal = true;
        RuntimeTypeHandle handle = o.GetType().TypeHandle;
        int value1 = handle.GetHashCode();
        int value2 = handle.GetHashCode();

        m_ErrorNo++;
        if (value1 == 0)
        {
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "RuntimeTypeHandle.GetHashCode returns 0");
            retVal = false;
        }

        m_ErrorNo++;
        if (value1 != value2)
        {
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Call RuntimeTypeHandle.GetHashCode twice returns wrong hash code");
            retVal = false;
        }

        return retVal;
    }

    private bool VerifyHashCode(Object o1, Object o2, bool desiredValue)
    {
        bool retVal = true;
        RuntimeTypeHandle handle1 = o1.GetType().TypeHandle;
        RuntimeTypeHandle handle2 = o2.GetType().TypeHandle;
        int value1 = handle1.GetHashCode();
        int value2 = handle2.GetHashCode();

        bool actualValue = value1 == value2;

        m_ErrorNo++;
        if (actualValue != desiredValue)
        {
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Compare the hash code for handle1 with handle2 returns ACTUAL: " + actualValue.ToString() + ", DESIRED: " + desiredValue.ToString());
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
