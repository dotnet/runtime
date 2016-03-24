// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// MakeByRefType()
/// </summary>
public class TypeMakeByRefType
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call MakeByRefType for reference type");

        try
        {
            Type type = typeof(String);
            Type refType = type.MakeByRefType();

            if (!refType.IsByRef)
            {
                TestLibrary.TestFramework.LogError("001", "Call MakeByRefType for reference type does not make a byref type");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call MakeByRefType for value type");

        try
        {
            Type type = typeof(int);
            Type refType = type.MakeByRefType();

            if (!refType.IsByRef)
            {
                TestLibrary.TestFramework.LogError("003", "Call MakeByRefType for value type does not make a byref type");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call MakeByRefType for value array type");

        try
        {
            Type type = typeof(int[]);
            Type refType = type.MakeByRefType();

            if (!refType.IsByRef)
            {
                TestLibrary.TestFramework.LogError("005", "Call MakeByRefType for value array type does not make a byref type");
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call MakeByRefType for reference array type");

        try
        {
            Type type = typeof(String[]);
            Type refType = type.MakeByRefType();

            if (!refType.IsByRef)
            {
                TestLibrary.TestFramework.LogError("007", "Call MakeByRefType for reference array type does not make a byref type");
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Call MakeByRefType for pointer type");

        try
        {
            Type type = typeof(char *);
            Type refType = type.MakeByRefType();

            if (!refType.IsByRef)
            {
                TestLibrary.TestFramework.LogError("009", "Call MakeByRefType for pointer type does not make a byref type");
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: TypeLoadException will be thrown when calling MakeByRefType for ByRef type");

        try
        {
            Type type = typeof(Object);
            type = type.MakeByRefType();
            Type refType = type.MakeByRefType();

            TestLibrary.TestFramework.LogError("101", "TypeLoadException is not thrown when calling MakeByRefType for ByRef type");
            retVal = false;
        }
        catch (TypeLoadException)
        {
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
        TypeMakeByRefType test = new TypeMakeByRefType();

        TestLibrary.TestFramework.BeginTestCase("TypeMakeByRefType");

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
