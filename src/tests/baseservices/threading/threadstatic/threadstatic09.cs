// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using TestLibrary;

public static class ThreadStaticAlignmentFallback
{
    private const int Pass = 100;
    private const int Fail = -1;
    private const int MaxPaddingCount = 12;

    public static int Main(string[] args)
    {
        if (args.Length == 1)
        {
            int paddingCount = int.Parse(args[0], CultureInfo.InvariantCulture);
            return InitializePadding<object>(paddingCount) && LongStorage.Check() ? Pass : Fail;
        }

        // Each child starts with a fresh direct-TLS allocation budget. Varying the padding
        // covers the boundary where an eight-byte field fits only without alignment padding.
        for (int paddingCount = 0; paddingCount <= MaxPaddingCount; paddingCount++)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(
                Environment.ProcessPath,
                [typeof(ThreadStaticAlignmentFallback).Assembly.Location, paddingCount.ToString(CultureInfo.InvariantCulture)])
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            startInfo.Environment["DOTNET_DbgEnableMiniDump"] = "0";
            startInfo.Environment["DOTNET_EnableCrashReport"] = "0";

            ProcessTextOutput output;
            try
            {
                output = Process.RunAndCaptureText(startInfo, TimeSpan.FromSeconds(60));
            }
            catch (TimeoutException)
            {
                Console.WriteLine($"Thread-static allocation timed out with {paddingCount} padding fields.");
                return Fail;
            }

            if (output.ExitStatus.ExitCode != Pass)
            {
                Console.WriteLine($"Thread-static allocation failed with {paddingCount} padding fields: {output.ExitStatus.ExitCode}");
                Console.WriteLine(output.StandardOutput);
                Console.WriteLine(output.StandardError);
                return Fail;
            }
        }

        return Pass;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool InitializePadding<T>(int count)
    {
        if (count == 0)
        {
            return true;
        }

        if (!Padding<T>.Check())
        {
            return false;
        }

        return InitializePadding<Padding<T>>(count - 1);
    }

    private sealed class Padding<T>
    {
        [ThreadStatic]
        private static int s_value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Check()
        {
            if (s_value != 0)
            {
                return false;
            }

            s_value = 42;
            return s_value == 42;
        }
    }

    private static class LongStorage
    {
        [ThreadStatic]
        private static long s_value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Check()
        {
            if (Interlocked.Increment(ref s_value) != 1)
            {
                return false;
            }

            s_value = 0x1122334455667788;
            return s_value == 0x1122334455667788;
        }
    }
}
