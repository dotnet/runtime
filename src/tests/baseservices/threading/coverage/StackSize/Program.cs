// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

class NativeAotThreadStackSizeTest
{
    [DllImport("libc", SetLastError = true)]
    private static extern nuint pthread_self();

    [DllImport("libc", SetLastError = true)]
    private static extern int pthread_getattr_np(nuint thread, nint attr);

    [DllImport("libc", SetLastError = true)]
    private static extern int pthread_attr_getstacksize(nint attr, out nuint stacksize);

    [DllImport("libc", SetLastError = true)]
    private static extern int pthread_attr_init(nint attr);

    [DllImport("libc", SetLastError = true)]
    private static extern int pthread_attr_destroy(nint attr);

    private static unsafe long GetCurrentThreadStackSize()
    {
        try
        {
            // Allocate space for pthread_attr_t (typically 56 bytes on x86_64)
            byte* attrBuffer = stackalloc byte[256]; // Generous size
            nint attr = (nint)attrBuffer;

            if (pthread_attr_init(attr) != 0)
                return -1;

            nuint thread = pthread_self();
            if (pthread_getattr_np(thread, attr) != 0)
            {
                pthread_attr_destroy(attr);
                return -1;
            }

            int result = pthread_attr_getstacksize(attr, out nuint stacksize);
            pthread_attr_destroy(attr);

            if (result != 0)
                return -1;

            return (long)stacksize;
        }
        catch
        {
            return -1;
        }
    }

    private static int RunMeasurementInChild(string stackSizeHex, int expectedSizeBytes)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = Environment.ProcessPath!,
            Arguments = "--measure",
            UseShellExecute = false,
            RedirectStandardOutput = true
        };
        
        psi.Environment["DOTNET_Thread_DefaultStackSize"] = stackSizeHex;

        using Process? process = Process.Start(psi);
        if (process == null)
            return 1;

        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 100)
            return process.ExitCode;

        if (!long.TryParse(output.Trim(), out long measuredSize))
            return 3;

        // Check if the measured size matches expected
        return measuredSize == expectedSizeBytes ? 0 : 4;
    }

    private static int MeasureMode()
    {
        // Create a thread with default stack size and measure it
        long measuredSize = 0;
        ManualResetEventSlim ready = new ManualResetEventSlim(false);

        Thread t = new Thread(() =>
        {
            measuredSize = GetCurrentThreadStackSize();
            ready.Set();
        }, 0); // Use default stack size

        t.Start();
        ready.Wait();
        t.Join();

        // Output the measured size
        Console.WriteLine(measuredSize);
        return measuredSize > 0 ? 100 : 1;
    }

    public static int Main(string[] args)
    {
        // Skip test on non-Linux platforms
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return 100;

        // If called with --measure, just measure and output the stack size
        if (args.Length > 0 && args[0] == "--measure")
            return MeasureMode();

        // Test with non-standard stack sizes to avoid coincidental matches
        // Using pthread_getattr_np gives us the exact stack size without any guesswork
        
        // Test with 1.5MB stack (0x180000 hex = 1572864 bytes)
        int result = RunMeasurementInChild("180000", 1572864);
        if (result != 0)
            return result;

        // Test with 3.25MB stack (0x340000 hex = 3407872 bytes)
        result = RunMeasurementInChild("340000", 3407872);
        if (result != 0)
            return result + 10;

        return 100; // Success
    }
}
