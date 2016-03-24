// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Reflection;

public class FileNotFoundExceptionGetMessage
{
    public static int Main()
    {
        FileNotFoundExceptionGetMessage ac = new FileNotFoundExceptionGetMessage();

        TestLibrary.TestFramework.BeginTestCase("FileNotFoundExceptionGetMessage");

        if (ac.RunTests())
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
        FileNotFoundException fnfe;

        TestLibrary.TestFramework.BeginScenario("PosTest1: FileNotFoundException.get_Message");

        try
        {
            fnfe = (FileNotFoundException)TestLibrary.Generator.GetType(typeof(FileNotFoundException));

            if (null == fnfe.Message)
            {
                TestLibrary.TestFramework.LogError("001", "Message is null!");
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
}

