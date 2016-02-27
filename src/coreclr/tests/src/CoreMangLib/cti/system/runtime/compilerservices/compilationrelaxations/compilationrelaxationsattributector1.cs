// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;

///<summary>
///System.Runtime.CompilerServices.CompilationRelaxationsAttribute.Ctor(int) [v-zuolan]
///</summary>

public class CompilationRelaxationsAttributeCtor
{

    public static int Main()
    {
        CompilationRelaxationsAttributeCtor testObj = new CompilationRelaxationsAttributeCtor();
        TestLibrary.TestFramework.BeginTestCase("for constructor of System.Runtime.CompilerServices.CompilationRelaxationsAttribute");
        if (testObj.RunTests())
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
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }


    #region Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        int expectedValue = TestLibrary.Generator.GetInt32(-55);
        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Create a instance with random parameter.");
        try
        {
            CompilationRelaxationsAttribute cRA = new CompilationRelaxationsAttribute(expectedValue);
            actualValue = cRA.CompilationRelaxations;

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("001", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e + "when expectedValue is:" + expectedValue);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        int expectedValue = int.MaxValue;
        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2:Create a instance with MaxValue.");
        try
        {
            CompilationRelaxationsAttribute cRA = new CompilationRelaxationsAttribute(expectedValue);
            actualValue = cRA.CompilationRelaxations;

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("003", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }


    public bool PosTest3()
    {
        bool retVal = true;

        int expectedValue = int.MinValue;
        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest3:Create a instance with MinValue.");
        try
        {
            CompilationRelaxationsAttribute cRA = new CompilationRelaxationsAttribute(expectedValue);
            actualValue = cRA.CompilationRelaxations;

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("005", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    #endregion
}
