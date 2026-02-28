// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the BuiltInCOM contract.
/// On Windows: creates managed objects with COM callable wrappers (CCWs)
/// using Marshal.GetIUnknownForObject, then crashes via FailFast.
/// On other platforms: crashes immediately without creating COM objects.
/// </summary>
internal static class Program
{
    public const int ObjectCount = 3;

    // Static field keeps the COM interface pointers (and therefore the CCWs) alive
    // until the process crashes, so they are visible in the dump.
    private static readonly IntPtr[] s_comPointers = new IntPtr[ObjectCount];

    private static void Main()
    {
        if (OperatingSystem.IsWindows())
        {
            for (int i = 0; i < ObjectCount; i++)
            {
                var obj = new ComObject();
                s_comPointers[i] = Marshal.GetIUnknownForObject(obj);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Environment.FailFast("cDAC dump test: CCWInterfaces debuggee intentional crash");
    }
}

/// <summary>
/// COM interface exposed by the test object.
/// </summary>
[ComVisible(true)]
[Guid("d9f48a91-5a3c-4d2e-8b0f-1234567890ab")]
internal interface IComTestInterface
{
    int GetValue();
}

/// <summary>
/// Managed class that implements <see cref="IComTestInterface"/> and acts as a CCW source.
/// Named to be easily identifiable when enumerating the heap from the dump test.
/// </summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
internal class ComObject : IComTestInterface
{
    public int GetValue() => 42;
}
