// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;
using System;
using System.IO; // For Stream

[SecuritySafeCritical]
public class StreamDispose1
{
    #region Private Fields
    private const string c_READ_FILE_NAME = "EmptyFile.txt";
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Make sure Dispose can be called");

        try
        {
            Stream stream = new FileStream(c_READ_FILE_NAME, FileMode.OpenOrCreate);

            stream.Dispose();

            try
            {
                stream.ReadByte();

                TestLibrary.TestFramework.LogError("001.1", "Call Dispose takes no effect");
                retVal = false;
            }
            catch (ObjectDisposedException)
            { 
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Make sure Dispose can be called twice times");

        try
        {
            Stream stream = new FileStream(c_READ_FILE_NAME, FileMode.OpenOrCreate);

            stream.Dispose();
            stream.Dispose();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Make sure Dispose can be called through IDisposable interface");

        try
        {
            Stream stream = new FileStream(c_READ_FILE_NAME, FileMode.OpenOrCreate);
            IDisposable idisp = stream as IDisposable;

            idisp.Dispose();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        StreamDispose1 test = new StreamDispose1();

        TestLibrary.TestFramework.BeginTestCase("StreamDispose1");

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
}
