// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.IO;


public class MyResource : IDisposable
{
    public IntPtr handle;
    private bool disposed = false;

    public MyResource(IntPtr handle)
    {
        this.handle = handle;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    [System.Security.SecuritySafeCritical]
    private void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
            }

            handle = IntPtr.Zero;
        }
        disposed = true;
    }

    ~MyResource()
    {
        Dispose(false);
    }
}

/// <summary>
/// System.IDisposable.Dispose()
/// </summary>
public class IDisposableDispose
{
    private const string c_FILE_NAME = "FileStream.txt";
    public static int Main(string[] args)
    {
        IDisposableDispose dispose = new IDisposableDispose();
        TestLibrary.TestFramework.BeginTestCase("Testing System.IDisposable.Dispose()...");

        if (dispose.RunTests())
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
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    [System.Security.SecuritySafeCritical]
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify Dispose interface is implemented by Stream class...");

        try
        {
            Stream stream = new FileStream(c_FILE_NAME,FileMode.OpenOrCreate);
            IDisposable idisp = stream as IDisposable;

            stream.Dispose();

            try
            {
                stream.ReadByte();

                TestLibrary.TestFramework.LogError("001","The stream object is not disposed yet!");
                retVal = false;
            }
            catch (ObjectDisposedException)
            { 
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

   [System.Security.SecuritySafeCritical]
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify dispose method can be called myltiple times without exception occurs...");

        try
        {
            FileStream stream = new FileStream(c_FILE_NAME,FileMode.OpenOrCreate);
            IDisposable idisp = stream as IDisposable;

            idisp.Dispose();
            idisp.Dispose();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify dispose interface is implemented in customer class...");

        try
        {
            int i = TestLibrary.Generator.GetInt32(-55);
            IntPtr ip = new IntPtr(i);
            MyResource myRes = new MyResource(ip);

            myRes.Dispose();

            if (myRes.handle != IntPtr.Zero)
            {
                TestLibrary.TestFramework.LogError("004","The handle should be IntPtr.zero!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
