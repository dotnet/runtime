// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Reflection;

public class MemoryStreamCtor
{
    public static int Main()
    {
        MemoryStreamCtor ac = new MemoryStreamCtor();

        TestLibrary.TestFramework.BeginTestCase("MemoryStreamCtor");

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
        MemoryStream mem;

        TestLibrary.TestFramework.BeginScenario("PosTest1: MemoryStream.Ctor");

        try
        {
            mem = new MemoryStream();

            if (null == mem)
            {
                TestLibrary.TestFramework.LogError("001", "MemoryStream failed to load");
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

