// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;

/// <summary>
/// CustomConstantAttribute.Ctor()
/// </summary>
public class CustomConstantAttributector
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Using a derived class to call the protected Ctor method of base class");

        try
        {
            TestClass testClass = new TestClass();
            if (testClass == null)
            {
                TestLibrary.TestFramework.LogError("001", "The Constructor did not create a new instance as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
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
        CustomConstantAttributector test = new CustomConstantAttributector();

        TestLibrary.TestFramework.BeginTestCase("CustomConstantAttributector");

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
public class TestClass : CustomConstantAttribute
{
    public TestClass()
        : base()
    {
    }
    public override object Value
    {
        get { throw new System.Exception("The method or operation is not implemented."); }
    }
}
