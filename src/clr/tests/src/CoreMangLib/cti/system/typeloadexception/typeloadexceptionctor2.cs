// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

///<summary>
///System.TypeLoadException.Ctor(String) [v-zuolan]
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
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        return retVal;
    }


    #region Positive Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        String errMessage = TestLibrary.Generator.GetString(-55, false, 1, 255);

        TestLibrary.TestFramework.BeginScenario("PosTest1:Create a instance of TypeLoadException");
        try
        {
            TypeLoadException tLE = new TypeLoadException(errMessage);

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

    public bool PosTest2()
    {
        bool retVal = true;

        String errMessage = TestLibrary.Generator.GetString(-55, false, 1, 255);

        TestLibrary.TestFramework.BeginScenario("PosTest2:Determine the error message is changed or not when the exception is thown out");
        try
        {

            TypeLoadException tLE = new TypeLoadException(errMessage);

            if (tLE == null)
            {
                TestLibrary.TestFramework.LogError("003", "ExpectedValue(not null) !=ActualValue(null)");
                retVal = false;
            }
            else
            {
                retVal = false;
                throw tLE;
            }
        }
        catch (TypeLoadException eE)
        {
            retVal = true;
            if (!eE.Message.Equals(errMessage))
            {
                TestLibrary.TestFramework.LogError("004", "ExpectedValue(Message Not Be Changed) !=ActualValue(Message Be Changed)");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        String errMessage = String.Empty;

        TestLibrary.TestFramework.BeginScenario("PosTest3:Determine the method with Empty String");
        try
        {

            TypeLoadException tLE = new TypeLoadException(errMessage);

            if (tLE == null)
            {
                TestLibrary.TestFramework.LogError("006", "ExpectedValue(not null) !=ActualValue(null)");
                retVal = false;
            }
            else
            {
                retVal = false;
                throw tLE;
            }
        }
        catch (TypeLoadException eE)
        {
            retVal = true;
            if (!eE.Message.Equals(errMessage))
            {
                TestLibrary.TestFramework.LogError("007", "ExpectedValue(Message Not Be Changed) !=ActualValue(Message Be Changed)");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        String errMessage = TestLibrary.Generator.GetString(-55, false, 256, 512);

        TestLibrary.TestFramework.BeginScenario("PosTest4:Determine the method with Long String");
        try
        {

            TypeLoadException tLE = new TypeLoadException(errMessage);

            if (tLE == null)
            {
                TestLibrary.TestFramework.LogError("009", "ExpectedValue(not null) !=ActualValue(null)");
                retVal = false;
            }
            else
            {
                retVal = false;
                throw tLE;
            }
        }
        catch (TypeLoadException eE)
        {
            retVal = true;
            if (!eE.Message.Equals(errMessage))
            {
                TestLibrary.TestFramework.LogError("010", "ExpectedValue(Message Not Be Changed) !=ActualValue(Message Be Changed)");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("011", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
