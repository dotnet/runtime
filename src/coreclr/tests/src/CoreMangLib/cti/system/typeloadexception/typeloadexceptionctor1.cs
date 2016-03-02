// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

///<summary>
///System.TypeLoadException.Ctor() [v-zuolan]
///</summary>

public class TypeLoadExceptionCtor
{

    public static int Main()
    {
        TypeLoadExceptionCtor testObj = new TypeLoadExceptionCtor();
        TestLibrary.TestFramework.BeginTestCase("for constructor of System.TypeLoadException");
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
        TestLibrary.TestFramework.LogInformation("Positive");
        retVal = PosTest1() && retVal;
        return retVal;
    }


    #region Test Logic

    public bool PosTest1()
    {
        bool retVal = true;

        String errMessage = TestLibrary.Generator.GetString(-55, false, 1, 255);

        TestLibrary.TestFramework.BeginScenario("PosTest1:Create a instance of TypeLoadException");
        try
        {
            TypeLoadException tLE = new TypeLoadException();

            if (tLE == null)
            {
                TestLibrary.TestFramework.LogError("001", "ExpectedValue(not null) !=ActualValue(null)");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }


    #endregion
}
