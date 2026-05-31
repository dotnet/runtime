// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ObjectiveC;

// Validates that blittable objc_msgSend P/Invoke stubs are precompiled into the R2R image
// (via the crossgen2 --map output) and that the emitted stub actually executes the
// pending-exception path at runtime.
public static unsafe class ObjCPInvokeR2RTest
{
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_2(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend_stret")]
    private static extern void objc_msgSend_stret(IntPtr receiver, IntPtr selector);

    private sealed class PendingException : Exception
    {
        public PendingException(string message) : base(message) { }
    }

    [UnmanagedCallersOnly]
    private static IntPtr MsgSendCallback(IntPtr inst, IntPtr sel)
    {
        ObjectiveCMarshal.SetMessageSendPendingException(new PendingException(nameof(MsgSendCallback)));
        return IntPtr.Zero;
    }

    public static int Main()
    {
        if (!ValidateMapFile())
            return 1;

        if (!ValidatePendingExceptionPropagates())
            return 1;

        Console.WriteLine("PASSED: ObjC P/Invoke stubs are precompiled and the pending-exception path executes.");
        return 100;
    }

    private static bool ValidateMapFile()
    {
        string mapFile = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, "map");
        if (!File.Exists(mapFile))
        {
            Console.WriteLine($"FAILED: Map file not found at {mapFile}");
            return false;
        }

        // Only MethodWithGCInfo entries prove the stub was compiled into the image.
        string[] compiledStubs = File.ReadAllLines(mapFile)
            .Where(l => l.Contains("objc_msgSend") && l.Contains("MethodWithGCInfo"))
            .ToArray();

        string[] expectedStubs = new[]
        {
            "__objc_msgSend ",
            "__objc_msgSend_2 ",
            "__objc_msgSend_stret ",
        };

        bool allFound = true;
        foreach (string expected in expectedStubs)
        {
            bool found = compiledStubs.Any(l => l.Contains(expected));
            Console.WriteLine($"  {(found ? "OK" : "MISSING")}: {expected.Trim()}");
            if (!found)
                allFound = false;
        }

        if (!allFound)
            Console.WriteLine("FAILED: Not all objc_msgSend P/Invoke stubs were precompiled into the R2R image.");

        return allFound;
    }

    private static bool ValidatePendingExceptionPropagates()
    {
        IntPtr callback = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr>)&MsgSendCallback;
        ObjectiveCMarshal.SetMessageSendCallback(MessageSendFunction.MsgSend, callback);

        try
        {
            objc_msgSend(IntPtr.Zero, IntPtr.Zero);
        }
        catch (PendingException ex) when (ex.Message == nameof(MsgSendCallback))
        {
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAILED: unexpected exception from objc_msgSend: {ex.GetType()} - {ex.Message}");
            return false;
        }

        Console.WriteLine("FAILED: objc_msgSend returned without throwing the pending exception.");
        return false;
    }
}
