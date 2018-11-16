// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using SafeHandlesTests;
using TestLibrary;

class SafeFileHandle : SafeHandle //SafeHandle subclass
{
    private static readonly IntPtr _invalidHandleValue = new IntPtr(-1);

    //0 or -1 considered invalid
    public override bool IsInvalid
    {
        get { return handle == IntPtr.Zero || handle == _invalidHandleValue; }
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    [DllImport("PInvoke_SafeHandle_ReleaseHandle")]
    private static extern bool MyResourceReleaseMethod(IntPtr handle);

    //default constructor which just calls the base class constructor
    public SafeFileHandle()
        : base(IntPtr.Zero, true)
    {
    }

    override protected bool ReleaseHandle()
    {
        //	this method will not actually call any resource releasing API method
        //	since the out/ref SFH param is not actually initialized to an OS allocated
        //	HANDLE---instead the unmanaged side just initializes/changes it to some integer;
        //	If a resource releasing API method like CloseHandle were called then
        //	it would return false and an unhandled exception would be thrown by the
        //	runtime indicating that the release method failed
        MyResourceReleaseMethod(handle);
        return true;
    }

} //end of SafeFileHandle class

class Foo
{
    int FooMethod(int x, int y) { return x + y; }
}

class Bar
{
    void BarMethod() { }
}

internal class SHReleasingTester
{
    [DllImport("PInvoke_SafeHandle_ReleaseHandle")]
    private static extern void SHReleasing_OutParams(
        [MarshalAs(UnmanagedType.Interface)]Foo foo,
        out SafeFileHandle sh,
        [MarshalAs(UnmanagedType.Interface)]out Bar bar, out int x);

    [DllImport("PInvoke_SafeHandle_ReleaseHandle")]
    [return:MarshalAs(UnmanagedType.I1)]private static extern bool GetMyResourceReleaseMethodCalled();

    [DllImport("PInvoke_SafeHandle_ReleaseHandle")]
    private static extern void ResetMyResourceReleaseMethodCalled();

    public static int Main()
    {
        try{
            Console.WriteLine("SHReleasing_OutParams");
            SafeFileHandle sh;
            Foo foo = new Foo();
            Bar bar;
            int x;

            ResetMyResourceReleaseMethodCalled();

            //this unmanaged method will try to set the out Bar parameter to a Foo type
            //this should cause an InvalidCastException on the way back from unmanaged
            Assert.Throws<InvalidCastException>(() => SHReleasing_OutParams(foo, out sh, out bar, out x), "SHReleasing_OutParams");

            //force the finalizer for the SFH param to run
            Console.WriteLine("\tForcing finalizer for the SFH param to run...");
            sh = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.IsTrue(GetMyResourceReleaseMethodCalled(), "MyResourceReleaseMethod was NOT called");

            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
    }
} 

