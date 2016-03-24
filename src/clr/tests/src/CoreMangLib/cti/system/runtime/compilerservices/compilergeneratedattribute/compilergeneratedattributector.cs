// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime;

/// <summary>
/// System.Runtime.CompilerServices.CompilerGeneratedAttribute.Ctor()
/// </summary>
public class CompilerGeneratedAttributeCtor
{

    public static int Main()
    {
        CompilerGeneratedAttributeCtor cgaCtor = new CompilerGeneratedAttributeCtor();
        TestLibrary.TestFramework.BeginTestCase("Testing for Method: System.Runtime.CompilerServices.CompilerGeneratedAttribute.Ctor()...");

        if (cgaCtor.RunTests())
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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Create a new CompilerGeneratedAttribute instance ... ";
        const string c_TEST_ID = "P001";

        

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            System.Runtime.CompilerServices.CompilerGeneratedAttribute cga = new System.Runtime.CompilerServices.CompilerGeneratedAttribute();
            if (cga == null)
            {
                string errorDesc = "the CompilerGeneratedAttribute ctor error occurred.)";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexcpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
