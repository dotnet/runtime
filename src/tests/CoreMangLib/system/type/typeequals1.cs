// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

/// <summary>
/// System.Type.Equals(System.Object)
/// </summary>
public class TypeEquals1
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Compare two Types of the same class");

        try
        {
            Type t1 = typeof(TypeEquals1);
            Type t2 = typeof(TypeEquals1);

            if (!t1.Equals((Object)t2))
            {
                TestLibrary.TestFramework.LogError("001", "Two Types of the same class are not equals");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Compare Type and instance are not equal");

        try
        {
            Type t = typeof(Object);
            Object o = new Object();

            if (t.Equals(o))
            {
                TestLibrary.TestFramework.LogError("003", "Compare Type and instance are equal");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Compare Type and different instance are not equal");

        try
        {
            Type t = typeof(TypeEquals1);
            Object o = new Object();

            if (t.Equals(o))
            {
                TestLibrary.TestFramework.LogError("005", "Type and different instance are equal");
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Compare different types");

        try
        {
            Type t1 = typeof(TypeEquals1);
            Type t2 = typeof(Object);

            if (t1.Equals(t2))
            {
                TestLibrary.TestFramework.LogError("007", "Type.Equals returns true for different types");
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Compare a type with itself");

        try
        {
            Type t1 = typeof(TypeEquals1);

            if (!t1.Equals((Object)t1))
            {
                TestLibrary.TestFramework.LogError("009", "Type.Equals returns false when comparing a type with itself");
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: Compare type with null reference");

        try
        {
            if (typeof(Object).Equals(null))
            {
                TestLibrary.TestFramework.LogError("101", "Compare type with null reference");
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

    [Fact]
    public static int TestEntryPoint()
    {
        TypeEquals1 test = new TypeEquals1();

        TestLibrary.TestFramework.BeginTestCase("TypeEquals1");

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
