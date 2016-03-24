// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;
using System;
using System.IO;

[SecuritySafeCritical]
public class TestSubBinaryWriter : BinaryWriter
{
    public Stream GetOutStream
    {
        get
        {
            return OutStream;
        }
    }

    public TestSubBinaryWriter(Stream output) : base(output)
    {
    }

    public TestSubBinaryWriter() : base()
    {
    }
}

[SecuritySafeCritical]
public class TestBinaryWriterOutStreamStream : Stream
{
    public const int c_DEFAULT_VALUE = 0;
    public int m_Value = c_DEFAULT_VALUE;

    public override bool CanRead
    {
        [SecuritySafeCritical]
        get { return true; }
    }

    public override bool CanSeek
    {
        [SecuritySafeCritical]
        get { return true; }
    }

    public override bool CanWrite
    {
        [SecuritySafeCritical]
        get { return true; }
    }

    [SecuritySafeCritical]
    public override void Flush()
    {
    }

    public override long Length
    {
        [SecuritySafeCritical]
        get { return 0L; }
    }

    public override long Position
    {
        [SecuritySafeCritical]
        get
        {
            return 0L;
        }
        [SecuritySafeCritical]
        set
        {
        }
    }

    [SecuritySafeCritical]
    public override int Read(byte[] buffer, int offset, int count)
    {
        return count;
    }

    [SecuritySafeCritical]
    public override long Seek(long offset, SeekOrigin origin)
    {
        return 0L;
    }

    [SecuritySafeCritical]
    public override void SetLength(long value)
    {
    }

    [SecuritySafeCritical]
    public override void Write(byte[] buffer, int offset, int count)
    {
        m_Value++;
    }
}

/// <summary>
/// OutStream
/// </summary>
[SecuritySafeCritical]
public class BinaryWriterOutStream
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check to see whether OutStream is set correctly");

        try
        {
            TestSubBinaryWriter writer = new TestSubBinaryWriter(Stream.Null);

            if (writer.GetOutStream != Stream.Null)
            {
                TestLibrary.TestFramework.LogError("001.1", "OutStream is not set correctly");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Check to see whether OutStream works");

        try
        {
            TestBinaryWriterOutStreamStream stream = new TestBinaryWriterOutStreamStream();
            TestSubBinaryWriter writer = new TestSubBinaryWriter(stream);
            writer.Write(TestLibrary.Generator.GetInt32(-55));

            if (stream.m_Value != TestBinaryWriterOutStreamStream.c_DEFAULT_VALUE + 1)
            {
                TestLibrary.TestFramework.LogError("002.1", "OutStream does not work");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        BinaryWriterOutStream test = new BinaryWriterOutStream();

        TestLibrary.TestFramework.BeginTestCase("BinaryWriterOutStream");

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
