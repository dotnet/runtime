// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Reflection;

public class FileNotFoundExceptionCtor2
{
    private const int c_MIN_STRLEN = 64;
    private const int c_MAX_STRLEN = 2048;

    public static int Main()
    {
        FileNotFoundExceptionCtor2 ac = new FileNotFoundExceptionCtor2();

        TestLibrary.TestFramework.BeginTestCase("FileNotFoundExceptionCtor2");

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
        string str;

        TestLibrary.TestFramework.BeginScenario("PosTest1: FileNotFoundException.Ctor(string)");

        try
        {
            str = TestLibrary.Generator.GetString(-55, false, c_MIN_STRLEN, c_MAX_STRLEN);
            fnfe = new FileNotFoundException(str);

            if (!fnfe.Message.Equals(str))
            {
                TestLibrary.TestFramework.LogError("001", "Message is not expected: Expected("+str+") Actual("+fnfe.Message+")");
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

