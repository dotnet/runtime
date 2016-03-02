// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;

public class TestObject
{
    #region Private Members
    private const int c_MIN_STRING_LENGTH = 0;
    private const int c_MAX_STRING_LENGTH = 1024;

    private string m_Eat_Memory1 = null;
    private int m_Eat_Memory2 = 0;
    #endregion

    #region Constructors
    public TestObject()
    {
        m_Eat_Memory1 = TestLibrary.Generator.GetString(-55, 
            false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
        m_Eat_Memory2 = TestLibrary.Generator.GetInt32(-55);
    }
    #endregion

    #region Public Method
    public void DoSomething()
    {
        int x = 4;
		x += 5;
    }
    #endregion

    ~TestObject()
    {
        ObjectFinalize.m_STATIC_VARIABLE++;
    }
}

public class ObjectFinalize
{
    #region Public Static Member
    public const int c_DEFAULT_INT_VALUE = 1;

    public static volatile int m_STATIC_VARIABLE = c_DEFAULT_INT_VALUE;
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Test destroying an object");

        try
        {
            // Create an unreferenced object
            UseTestObject();

            // Force runtime to do Garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (ObjectFinalize.m_STATIC_VARIABLE != ObjectFinalize.c_DEFAULT_INT_VALUE + 1)
            {
                TestLibrary.TestFramework.LogError("001", "Call Object.Finalize failed: Execpted("+(ObjectFinalize.c_DEFAULT_INT_VALUE + 1)+") Actual("+ObjectFinalize.m_STATIC_VARIABLE+")");
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
    #endregion
    #endregion

    public static int Main()
    {
        ObjectFinalize test = new ObjectFinalize();

        TestLibrary.TestFramework.BeginTestCase("ObjectFinalize");

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

    #region Private Methods
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private void UseTestObject()
    {
        TestObject testObject = new TestObject();

        // Avoid used variable compiler warning.
        testObject.DoSomething();
    }
    #endregion
}
