// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;
using System;
using System.IO; 

[SecuritySafeCritical]
public class TestFileStream : FileStream
{
    public TestFileStream(string path, FileMode mode)
        : base(path, mode)
    { 
    }

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
        get { return base.CanSeek; }
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
public class FileStreamDispose
{
    #region Private Fileds
    const string c_DEFAULT_FILE_PATH = "TestFile";
    #endregion

    #region Public Methods
    public bool RunTesfs()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        
        //
        // TODO: Add your negative test cases here
        //
        // TestLibrary.TestFramework.LogInformation("[Negative]");
        // retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Dispose with disposing set to true");

        try
        {
            TestFileStream tfs = new TestFileStream(c_DEFAULT_FILE_PATH,FileMode.Append);
            tfs.DisposeWrapper(true);
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
            TestFileStream tfs = new TestFileStream(c_DEFAULT_FILE_PATH, FileMode.Append);

            tfs.DisposeWrapper(true);
            tfs.DisposeWrapper(true);
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
            TestFileStream tfs = new TestFileStream(c_DEFAULT_FILE_PATH, FileMode.Append);

            tfs.DisposeWrapper(false);
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
            TestFileStream tfs = new TestFileStream(c_DEFAULT_FILE_PATH, FileMode.Append);

            tfs.DisposeWrapper(false);
            tfs.DisposeWrapper(false);
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
            TestFileStream tfs = new TestFileStream(c_DEFAULT_FILE_PATH, FileMode.Append);

            tfs.DisposeWrapper(false);
            tfs.DisposeWrapper(true);
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
            TestFileStream tfs = new TestFileStream(c_DEFAULT_FILE_PATH, FileMode.Append);

            tfs.DisposeWrapper(true);
            tfs.DisposeWrapper(false);
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
        FileStreamDispose test = new FileStreamDispose();

        TestLibrary.TestFramework.BeginTestCase("FileStreamDispose");

        if (test.RunTesfs())
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
