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
/// Equals(System.Object)
/// </summary>
public class RuntimeTypeHandleEquals
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Equals should return true when comparing with itself");

        try
        {
            RuntimeTypeHandle handle = typeof(TestEmptyClass).TypeHandle;

            if (!handle.Equals(handle))
            {
                TestLibrary.TestFramework.LogError("001.1", "Equals returns false when comparing with itself");
                retVal = false;
            }
            handle = typeof(TestStruct1).TypeHandle;
            if (!handle.Equals(handle))
            {
                TestLibrary.TestFramework.LogError("001.2", "Equals returns false when comparing with itself");
                retVal = false;
            }

            handle = typeof(TestEnum1).TypeHandle;
            if (!handle.Equals(handle))
            {
                TestLibrary.TestFramework.LogError("001.3", "Equals returns false when comparing with itself");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.4", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Equals should return true when comparing with instance of same classes");

        try
        {
            TestEmptyClass ec1 = new TestEmptyClass();
            TestEmptyClass ec2 = new TestEmptyClass();
            RuntimeTypeHandle handle1 = ec1.GetType().TypeHandle;
            RuntimeTypeHandle handle2 = ec2.GetType().TypeHandle;

            if (!handle1.Equals(handle2))
            {
                TestLibrary.TestFramework.LogError("002.1", "Equals returns false when comparing with instance of same classe");
                retVal = false;
            }

            TestStruct1 ts1 = new TestStruct1();
            TestStruct1 ts2 = new TestStruct1();
            handle1 = ts1.GetType().TypeHandle;
            handle2 = ts2.GetType().TypeHandle;
            if (!handle1.Equals(handle2))
            {
                TestLibrary.TestFramework.LogError("002.2", "Equals returns false when comparing with instance of same structs");
                retVal = false;
            }

            TestEnum1 te1 = TestEnum1.DEFAULT;
            TestEnum1 te2 = TestEnum1.DEFAULT;
            handle1 = te1.GetType().TypeHandle;
            handle2 = te2.GetType().TypeHandle;
            if (!handle1.Equals(handle2))
            {
                TestLibrary.TestFramework.LogError("002.3", "Equals returns false when comparing with instance of same enums");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.4", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Equals should return false when comparing with instance of different classes");

        try
        {
            TestEmptyClass classInstance1 = new TestEmptyClass();
            TestClass classInstance2 = new TestClass();
            RuntimeTypeHandle classInstanceHandle1 = classInstance1.GetType().TypeHandle;
            RuntimeTypeHandle classInstanceHandle2 = classInstance2.GetType().TypeHandle;

            if (classInstanceHandle1.Equals(classInstanceHandle2))
            {
                TestLibrary.TestFramework.LogError("003.1", "Equals returns true when comparing with instance of different classe");
                retVal = false;
            }

            TestStruct1 ts1 = new TestStruct1();
            TestStruct2 ts2 = new TestStruct2();
            RuntimeTypeHandle structHandle1 = ts1.GetType().TypeHandle;
            RuntimeTypeHandle structHandle2 = ts2.GetType().TypeHandle;
            if (structHandle1.Equals(structHandle2))
            {
                TestLibrary.TestFramework.LogError("003.2", "Equals returns false when comparing with instance of different structs");
                retVal = false;
            }

            TestEnum1 te1 = TestEnum1.DEFAULT;
            TestEnum2 te2 = TestEnum2.DEFAULT;
            RuntimeTypeHandle enumHandle1 = te1.GetType().TypeHandle;
            RuntimeTypeHandle enumHandle2 = te2.GetType().TypeHandle;
            if (enumHandle1.Equals(enumHandle2))
            {
                TestLibrary.TestFramework.LogError("003.3", "Equals returns false when comparing with instance of different enums");
                retVal = false;
            }

            if (classInstanceHandle1.Equals(structHandle1))
            {
                TestLibrary.TestFramework.LogError("003.4", "Equals returns false when comparing a instance of struct with a instance of class");
                retVal = false;
            }

            if (classInstanceHandle1.Equals(enumHandle1))
            {
                TestLibrary.TestFramework.LogError("003.5", "Equals returns false when comparing a instance of enum with a instance of class");
                retVal = false;
            }

            if (structHandle1.Equals(enumHandle1))
            {
                TestLibrary.TestFramework.LogError("003.6", "Equals returns false when comparing a instance of struct with a instance of enum");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.7", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Equals should return false when comparing with a invalid value");

        try
        {
            RuntimeTypeHandle handle = typeof(TestClass).TypeHandle;
            if (handle.Equals(null))
            {
                TestLibrary.TestFramework.LogError("004.1", "Equals returns true when comparing with a null");
                retVal = false;
            }

            if (handle.Equals(new Object()))
            {
                TestLibrary.TestFramework.LogError("004.2", "Equals returns true when comparing with a instance of object");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.3", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Equals should return false when comparing with RuntimeTypeHandle of base class");

        try
        {
            RuntimeTypeHandle handle1 = typeof(TestClass).TypeHandle;
            RuntimeTypeHandle handle2 = typeof(Object).TypeHandle;
            if (handle1.Equals(handle2))
            {
                TestLibrary.TestFramework.LogError("005.1", "Equals returns true when comparing with RuntimeTypeHandle of base class");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        RuntimeTypeHandleEquals test = new RuntimeTypeHandleEquals();

        TestLibrary.TestFramework.BeginTestCase("RuntimeTypeHandleEquals");

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
}
