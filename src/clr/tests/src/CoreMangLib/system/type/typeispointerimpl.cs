// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public struct TestGenericType2
{
    public void TestMethod<T>()
    {
    }
}

/// <summary>
/// IsPointerImpl
/// </summary>
public class TypeIsPointerImpl
{
    #region Private Fields
    private const int c_MIN_STRING_LENGTH = 1;
    private const int c_MAX_STRING_LENGTH = 128;
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: IsPointerImpl should return false for value type");

        try
        {
            int obj = TestLibrary.Generator.GetInt32(-55);
            Type type = obj.GetType();

            if (type.IsPointer)
            {
                TestLibrary.TestFramework.LogError("001", "IsPointerImpl returns true for value type");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: IsPointerImpl should return false for reference type");

        try
        {
            String obj = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            Type type = obj.GetType();

            if (type.IsPointer)
            {
                TestLibrary.TestFramework.LogError("003", "IsPointerImpl returns true for reference type");
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

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: IsPointerImpl should return false for IntPtr");

        try
        {
            Type type = typeof(IntPtr);

            if (type.IsPointer)
            {
                TestLibrary.TestFramework.LogError("005", "IsPointerImpl returns true for IntPtr");
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: IsPointerImpl should return true for a pointer type");

        try
        {
            Type type = typeof(int*);

            if (!type.IsPointer)
            {
                TestLibrary.TestFramework.LogError("007", "IsPointerImpl returns false for a pointer type");
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: IsPointerImpl should return true for type contains generic member");

        try
        {
            Type type = typeof(TestGenericType2*);
            if (!type.IsPointer)
            {
                TestLibrary.TestFramework.LogError("101", "IsPointerImpl returns false for type contains generic member");
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

    public static int Main()
    {
        TypeIsPointerImpl test = new TypeIsPointerImpl();

        TestLibrary.TestFramework.BeginTestCase("TypeIsPointerImpl");

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
