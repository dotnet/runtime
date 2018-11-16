// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using SafeHandlesTests;
using TestLibrary;

public abstract class AllMySafeHandles : SafeHandle
{
    private static readonly IntPtr _invalidHandleValue = new IntPtr(-1);

    //0 or -1 considered invalid
    public override bool IsInvalid
    {
        get { return handle == IntPtr.Zero || handle == _invalidHandleValue; }
    }

    protected AllMySafeHandles(bool ownsHandle) : base(IntPtr.Zero, ownsHandle) { }
}

public sealed class MySafeEventHandle : AllMySafeHandles
{
    private MySafeEventHandle() : base(true) { }

    [DllImport("api-ms-win-core-synch-l1-2-0", SetLastError = true, EntryPoint = "CreateEventW")]
    internal static extern MySafeEventHandle CreateEvent(IntPtr mustBeZero, bool isManualReset, bool initialState, String name);

    [DllImport("api-ms-win-core-synch-l1-2-0", SetLastError = true)]
    internal static extern bool SetEvent(MySafeEventHandle handle);

    [DllImport("api-ms-win-core-handle-l1-1-0")]
    private static extern bool CloseHandle(IntPtr handle);

    override protected bool ReleaseHandle()
    {
        return CloseHandle(handle);
    }
}

public class AbstractDerivedSHTester
{
    private static void ClassHierarchyTest()
    {
        Console.WriteLine("Class hierarchy test...");

        //create an event
        Console.WriteLine("\tCreate new event");
        MySafeEventHandle sh = MySafeEventHandle.CreateEvent(IntPtr.Zero, true, false, null);
        Assert.IsFalse(sh.IsInvalid, "CreateEvent returned an invalid SafeHandle!");
        Assert.IsTrue(MySafeEventHandle.SetEvent(sh), "SetEvent failed on a SafeHandle!");

        Console.WriteLine("\tFirst test: Call dispose on sh");
        sh.Dispose();
        Console.WriteLine("\tCall succeeded.\n");

        // Now create another event and force the critical finalizer to run.
        Console.WriteLine("\tCreate new event");
        sh = MySafeEventHandle.CreateEvent(IntPtr.Zero, false, true, null);
        Assert.IsFalse(sh.IsInvalid, "CreateEvent returned an invalid SafeHandle!");

        Console.WriteLine("\tSecond test: Force critical finalizer to run");
        sh = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine("\tSucceeded.\n");
    }

    private static int Main()
    {
        try
        {
            ClassHierarchyTest();

            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
    }
}