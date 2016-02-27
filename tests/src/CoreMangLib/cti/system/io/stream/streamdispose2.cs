// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;
using System;
using System.IO; // For Stream

[SecuritySafeCritical]
public class TestStream : Stream
{
    public void DisposeWrapper(bool disposing)
    {
        Dispose(disposing);
    }

    public override bool CanRead
    {
        [SecuritySafeCritical]
        get { throw new Exception("The method or operation is not implemented."); }
    }

    public override bool CanSeek
    {
        [SecuritySafeCritical]
        get { throw new Exception("The method or operation is not implemented."); }
    }

    public override bool CanWrite
    {
        [SecuritySafeCritical]
        get { throw new Exception("The method or operation is not implemented."); }
    }

    [SecuritySafeCritical]
    public override void Flush()
    {
        throw new Exception("The method or operation is not implemented.");
    }

    public override long Length
    {
        [SecuritySafeCritical]
        get { throw new Exception("The method or operation is not implemented."); }
    }

    public override long Position
    {
        [SecuritySafeCritical]
        get
        {
            throw new Exception("The method or operation is not implemented.");
        }
        [SecuritySafeCritical]
        set
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

    [SecuritySafeCritical]
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new Exception("The method or operation is not implemented.");
    }

    [SecuritySafeCritical]
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new Exception("The method or operation is not implemented.");
    }

    [SecuritySafeCritical]
    public override void SetLength(long value)
    {
        throw new Exception("The method or operation is not implemented.");
    }

    [SecuritySafeCritical]
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new Exception("The method or operation is not implemented.");
    }
}

[SecuritySafeCritical]
public class StreamDispose2
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Dispose with disposing set to true");

        try
        {
            TestStream ts = new TestStream();

            ts.DisposeWrapper(true);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call Dispose twice with disposing set to true");

        try
        {
            TestStream ts = new TestStream();

            ts.DisposeWrapper(true);
            ts.DisposeWrapper(true);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call Dispose with disposing set to false");

        try
        {
            TestStream ts = new TestStream();

            ts.DisposeWrapper(false);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call Dispose twice with disposing set to false");

        try
        {
            TestStream ts = new TestStream();

            ts.DisposeWrapper(false);
            ts.DisposeWrapper(false);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Call Dispose twice with disposing set to false once and another time set to true");

        try
        {
            TestStream ts = new TestStream();

            ts.DisposeWrapper(false);
            ts.DisposeWrapper(true);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Call Dispose twice with disposing set to true once and another time set to false");

        try
        {
            TestStream ts = new TestStream();

            ts.DisposeWrapper(true);
            ts.DisposeWrapper(false);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        StreamDispose2 test = new StreamDispose2();

        TestLibrary.TestFramework.BeginTestCase("StreamDispose2");

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
