// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;

/// <summary>
/// InternalsVisibleToAttribute.AssemblyName [v-yaduoj]
/// </summary>
public class InternalsVisibleToAttributeAssemblyName
{
    public static int Main()
    {
        InternalsVisibleToAttributeAssemblyName testObj = new InternalsVisibleToAttributeAssemblyName();

        TestLibrary.TestFramework.BeginTestCase("for property: InternalsVisibleToAttribute.AssemblyName");
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
        retVal = PosTest4() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        string c_TEST_DESC = "PosTest1: Get a normal friend assembly name.";
        string errorDesc;

        string assemblyName;
        string actualFriendAssemblyName;
        assemblyName = "myTestCase.dll";
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            InternalsVisibleToAttribute internalIsVisibleTo = new InternalsVisibleToAttribute(assemblyName);
            actualFriendAssemblyName = internalIsVisibleTo.AssemblyName;
            if (actualFriendAssemblyName != assemblyName)
            {
                errorDesc = "The friend assembly name is not the value \"" + assemblyName + 
                            "\" as expected, Actually\"" + actualFriendAssemblyName + "\"";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        string c_TEST_DESC = "PosTest2: Get a friend assembly name that is an emtpy string.";
        string errorDesc;

        string assemblyName;
        string actualFriendAssemblyName;
        assemblyName = string.Empty;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            InternalsVisibleToAttribute internalIsVisibleTo = new InternalsVisibleToAttribute(assemblyName);
            actualFriendAssemblyName = internalIsVisibleTo.AssemblyName;
            if (actualFriendAssemblyName != assemblyName)
            {
                errorDesc = "The friend assembly name is not an empty string as expected, Actually\"" +
                            actualFriendAssemblyName + "\"";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "P003";
        string c_TEST_DESC = "PosTest3: Get a friend assembly name that is a null reference.";
        string errorDesc;

        string assemblyName;
        string actualFriendAssemblyName;
        assemblyName = null;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            InternalsVisibleToAttribute internalIsVisibleTo = new InternalsVisibleToAttribute(assemblyName);
            actualFriendAssemblyName = internalIsVisibleTo.AssemblyName;
            if (actualFriendAssemblyName != assemblyName)
            {
                errorDesc = "The friend assembly name is not a null reference as expected, Actually\"" + 
                            actualFriendAssemblyName + "\"";
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_ID = "P004";
        string c_TEST_DESC = "PosTest4: Get a friend assembly name containing special characters.";
        string errorDesc;

        string assemblyName;
        string actualFriendAssemblyName;
        assemblyName = "::B:" + System.IO.Path.DirectorySeparatorChar + "\n\v\r\t\0myTestCase.dll";
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            InternalsVisibleToAttribute internalIsVisibleTo = new InternalsVisibleToAttribute(assemblyName);
            actualFriendAssemblyName = internalIsVisibleTo.AssemblyName;
            if (actualFriendAssemblyName != assemblyName)
            {
                errorDesc = "The friend assembly name is not the value \"" + assemblyName +
                            "\" as expected, Actually\"" + actualFriendAssemblyName + "\"";
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
