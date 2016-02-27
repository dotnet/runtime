// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

public class TestGenericComparer<T> : Comparer<T>
{
    public TestGenericComparer()
        : base()
    {
    }

    public override int Compare(T x, T y)
    {
        throw new Exception("The method or operation is not implemented.");
    }
}

/// <summary>
/// ctor
/// </summary>
public class ComparerCtor
{
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call protected ctor to create a new instance of sub class");

        try
        {
            TestGenericComparer<int> comparer1 = new TestGenericComparer<int>();

            if (comparer1 == null)
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling protected ctor to create a new instance of sub class returns null reference");
                retVal = false;
            }

            TestGenericComparer<string> comparer2 = new TestGenericComparer<string>();

            if (comparer2 == null)
            {
                TestLibrary.TestFramework.LogError("001.2", "Calling protected ctor to create a new instance of sub class returns null reference");
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
        ComparerCtor test = new ComparerCtor();

        TestLibrary.TestFramework.BeginTestCase("ComparerCtor");

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
