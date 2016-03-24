// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class TestClass
{
    private const int c_SIZE_OF_ARRAY = 1024;
    private byte[] m_Bytes;

    public TestClass()
    {
        m_Bytes = new byte[c_SIZE_OF_ARRAY];
        TestLibrary.Generator.GetBytes(-55, m_Bytes);
    }
}

/// <summary>
/// Collect
/// </summary>
public class GCCollect
{
    #region Private Fields
    private const int c_MAX_GARBAGE_COUNT = 1000;
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Collect to reclaim memories");

        try
        {
            MakeSomeGarbage();
            long beforeCollect = GC.GetTotalMemory(false);

            GC.Collect();
            long afterCollect = GC.GetTotalMemory(true);

            if (beforeCollect <= afterCollect)
            {
                TestLibrary.TestFramework.LogError("001.1", "GC.Collect does not work");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] beforeCollect = " + beforeCollect + ", afterCollect = " + afterCollect);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        GCCollect test = new GCCollect();

        TestLibrary.TestFramework.BeginTestCase("GCCollect");

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
    private static void MakeSomeGarbage()
    {
        for (int i = 0; i < c_MAX_GARBAGE_COUNT; ++i)
        {
            TestClass c = new TestClass();
        }
    }
    #endregion
}
