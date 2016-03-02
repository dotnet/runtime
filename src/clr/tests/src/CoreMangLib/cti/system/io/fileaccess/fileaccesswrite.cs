// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;

/// <summary>
/// System.IO.FileAccess.Write
/// </summary>
public class FileAccessWrite
{
    static public int Main()
    {
        FileAccessWrite test = new FileAccessWrite();

        TestLibrary.TestFramework.BeginTestCase("System.IO.FileAccess.Write");

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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {

        bool retVal = true;

        const string c_TEST_DESC = "PosTest1:check the FileAccess.Write value is 2...";
        const string c_TEST_ID = "P001";
        FileAccess FLAG_VALUE = (FileAccess)2;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (FileAccess.Write != FLAG_VALUE)
            {
                string errorDesc = "value is not " + FLAG_VALUE.ToString() + " as expected: Actual is " + FileAccess.Write.ToString();
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }


}