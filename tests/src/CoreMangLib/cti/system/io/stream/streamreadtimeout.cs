// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;
using System;
using System.Collections;
using System.IO;

[SecuritySafeCritical]
public class StreamReadTimeOut
{
    public static int Main(string[] args)
    {
        StreamReadTimeOut readTimeOut = new StreamReadTimeOut();
        TestLibrary.TestFramework.BeginScenario("Testing System.IO.Stream.ReadTimeOut property...");

        if (readTimeOut.RunTests())
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
        //retVal = PosTest1() && retVal;
        //retVal = PosTest2() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negtive]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal; 

        return retVal;
    }

    public bool NegTest1() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify InvalidOperationException is thrown when get ReadTimeOut property...");

        try
        {
            Stream s = new MemoryStream();
            for (int i = 0; i < 100; i++)
                s.WriteByte((byte)i);
            s.Position = 0;

            int len = s.ReadTimeout;

            TestLibrary.TestFramework.LogError("001","No exception occurs!");
            retVal = false;
        }
        catch (InvalidOperationException)
        { 
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify InvalidOperationException is thrown when set ReadTimeOut property...");

        try
        {
            Stream s = new MemoryStream();
            for (int i = 0; i < 100; i++)
                s.WriteByte((byte)i);
            s.Position = 0;

            s.ReadTimeout = 10;

            TestLibrary.TestFramework.LogError("001", "No exception occurs!");
            retVal = false;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
