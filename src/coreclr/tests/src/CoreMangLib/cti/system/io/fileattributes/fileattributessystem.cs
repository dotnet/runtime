// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;

/// <summary>
/// FileAttributes.System
/// </summary>
public class FileAttributesTest
{
    [System.Runtime.InteropServices.ComVisible(true)]
    internal enum MyFileAttributes
    {
        ReadOnly = 0x1,
        Hidden = 0x2,
        System = 0x4,
        Directory = 0x10,
        Archive = 0x20,
        Device = 0x40,
        Normal = 0x80,
        Temporary = 0x100,
        SparseFile = 0x200,
        ReparsePoint = 0x400,
        Compressed = 0x800,
        Offline = 0x1000,
        NotContentIndexed = 0x2000,
        Encrypted = 0x4000,
    }

    public static int Main()
    {
        FileAttributesTest testObj = new FileAttributesTest();

        TestLibrary.TestFramework.BeginTestCase("for enumeration: FileAttributes.System");
        if(testObj.RunTests())
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

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: File attributes is System";
        string errorDesc;

        int expectedValue;
        int actualValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            expectedValue = (int)MyFileAttributes.System;
            actualValue = (int)FileAttributes.System;
            if (actualValue != expectedValue)
            {
                errorDesc = "System value of file attributes is not " + expectedValue +
                                 "as expected: actual(" + actualValue + ")";
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
    #endregion
}

