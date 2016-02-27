// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;

/// <summary>
/// InternalsVisibleToAttribute.Ctor(System.String)
/// </summary>
public class InternalsVisibleToAttributeCtor
{
    private const string c_ASSEMBLY_NAME = "AssemblyName";

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call the constructor directly");

        try
        {
            InternalsVisibleToAttribute internalsVisibleToAttribute = new InternalsVisibleToAttribute(c_ASSEMBLY_NAME);
            if (internalsVisibleToAttribute == null)
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
        InternalsVisibleToAttributeCtor test = new InternalsVisibleToAttributeCtor();

        TestLibrary.TestFramework.BeginTestCase("InternalsVisibleToAttributeCtor");

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
