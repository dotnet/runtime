// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// MakePointerType
/// </summary>
public class TypeMakePointerType
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call MakePointerType for a value type");

        try
        {
            Type type = typeof(int);
            Type pointerType = type.MakePointerType();

            if (!pointerType.IsPointer)
            {
                TestLibrary.TestFramework.LogError("001", "Call MakePointerType for a value type does not make a pointer type");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call MakePointerType for a reference type");

        try
        {
            Type type = typeof(String);
            Type pointerType = type.MakePointerType();

            if (!pointerType.IsPointer)
            {
                TestLibrary.TestFramework.LogError("003", "Call MakePointerType for a reference type does not make a pointer type");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call MakePointerType for a value array type");

        try
        {
            Type type = typeof(int[]);
            Type pointerType = type.MakePointerType();

            if (!pointerType.IsPointer)
            {
                TestLibrary.TestFramework.LogError("005", "Call MakePointerType for a value array type does not make a pointer type");
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call MakePointerType for a reference array type");

        try
        {
            Type type = typeof(String[]);
            Type pointerType = type.MakePointerType();

            if (!pointerType.IsPointer)
            {
                TestLibrary.TestFramework.LogError("007", "Call MakePointerType for a reference array type does not make a pointer type");
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Call MakePointerType for a pointer type");

        try
        {
            Type type = typeof(char *);
            Type pointerType = type.MakePointerType();

            if (!pointerType.IsPointer)
            {
                TestLibrary.TestFramework.LogError("009", "Call MakePointerType for a pointer type does not make a pointer type");
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
    #endregion

    public static int Main()
    {
        TypeMakePointerType test = new TypeMakePointerType();

        TestLibrary.TestFramework.BeginTestCase("TypeMakePointerType");

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
