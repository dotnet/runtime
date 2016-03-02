// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Array.Initialize()
/// </summary>
public class ArrayInitialize
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Array of Int32, Initialize ");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55);
            int[] i1 = new int[length];
            for (int i = 0; i < length; i++)
            {
                i1[i] = i;
            }
            i1.Initialize(); // The type of int32 does not have constructors, so the value is not changed
            for (int i = 0; i < length; i++)
            {
                if (i1[i] != i)
                {
                    TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Try to Initialize a customized structure type");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55);
            A[] i1 = new A[length];
            for (int i = 0; i < length; i++)
            {
                i1[i] = new A(i);
            }
            i1.Initialize();
            for (int i = 0; i < length; i++)
            {
                if (i1[i].a != i)
                {
                    TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
                    retVal = false;
                }
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Initialize a reference-type array ");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55);
            string[] i1 = new string[length];
            for (int i = 0; i < length; i++)
            {
                i1[i] = i.ToString();
            }
            i1.Initialize(); // The type of int32 does not have constructors, so the value is not changed
            for (int i = 0; i < length; i++)
            {
                if (i1[i] != i.ToString())
                {
                    TestLibrary.TestFramework.LogError("005", "The result is not the value as expected");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Try to Initialize a customized class type");

        try
        {
            int length = TestLibrary.Generator.GetInt16(-55);
            B[] i1 = new B[length];
            for (int i = 0; i < length; i++)
            {
                i1[i] = new B(i);
            }
            i1.Initialize();
            for (int i = 0; i < length; i++)
            {
                if (i1[i].b_value != i)
                {
                    TestLibrary.TestFramework.LogError("007", "The result is not the value as expected");
                    retVal = false;
                }
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases

    #endregion
    #endregion

    public static int Main()
    {
        ArrayInitialize test = new ArrayInitialize();

        TestLibrary.TestFramework.BeginTestCase("ArrayInitialize");

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
struct A 
{
    public A(int a)
    {
        this.a = a;
    }
    public int a;
}

class B
{
    public B()
    {
        this.b = 0;
    }
    public B(int b)
    {
        this.b = b;
    }
    public int b_value
    {
        get
        {
            return this.b;
        }

    }
    private int b;

}

