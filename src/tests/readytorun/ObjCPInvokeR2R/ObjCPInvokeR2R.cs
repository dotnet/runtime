// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

/// <summary>
/// Validates that blittable objc_msgSend P/Invoke stubs are precompiled
/// into the ReadyToRun image by checking the crossgen2 map file output.
/// </summary>
public static class ObjCPInvokeR2RTest
{
    // Blittable objc_msgSend declarations — these should be precompiled by R2R.
    // The module path must be "/usr/lib/libobjc.dylib" to match the ObjC detection
    // in ShouldCheckForPendingException (MarshalHelpers.cs).
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_2(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend_stret")]
    private static extern void objc_msgSend_stret(IntPtr receiver, IntPtr selector);

    // This method references the P/Invoke declarations to ensure crossgen2 processes them
    private static void EnsurePInvokesReferenced()
    {
        // These calls are never actually executed — they just ensure crossgen2
        // sees the P/Invoke declarations and attempts to generate stubs.
        // The test validates stubs exist in the map, not runtime behavior.
        if (Environment.GetEnvironmentVariable("NEVER_SET_THIS_VARIABLE_12345") != null)
        {
            objc_msgSend(IntPtr.Zero, IntPtr.Zero);
            objc_msgSend_2(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            objc_msgSend_stret(IntPtr.Zero, IntPtr.Zero);
        }
    }

    public static int Main()
    {
        EnsurePInvokesReferenced();

        string assemblyPath = Assembly.GetExecutingAssembly().Location;
        string mapFile = Path.ChangeExtension(assemblyPath, "map");

        if (!File.Exists(mapFile))
        {
            Console.WriteLine($"FAILED: Map file not found at {mapFile}");
            return 1;
        }

        string[] mapLines = File.ReadAllLines(mapFile);
        Console.WriteLine($"Map file has {mapLines.Length} lines");

        // Search for objc_msgSend P/Invoke stubs that were actually compiled (MethodWithGCInfo).
        // MethodFixupSignature entries are just metadata references that exist regardless
        // of whether the stub was precompiled — only MethodWithGCInfo entries prove the
        // P/Invoke IL stub was actually generated and compiled into the R2R image.
        string[] compiledStubs = mapLines
            .Where(l => l.Contains("objc_msgSend") && l.Contains("MethodWithGCInfo"))
            .ToArray();

        Console.WriteLine($"Found {compiledStubs.Length} compiled objc_msgSend stubs (MethodWithGCInfo):");
        foreach (string line in compiledStubs)
        {
            Console.WriteLine($"  {line}");
        }

        // Verify all three P/Invoke stubs are precompiled
        string[] expectedStubs = new[]
        {
            "__objc_msgSend ",    // 2-arg variant (trailing space to avoid partial match)
            "__objc_msgSend_2 ",  // 3-arg variant
            "__objc_msgSend_stret " // stret variant
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
        {
            Console.WriteLine("FAILED: Not all objc_msgSend P/Invoke stubs were precompiled.");
            Console.WriteLine("This means R2R did not generate IL stubs for ObjC P/Invokes.");
            return 1;
        }

        Console.WriteLine("PASSED: All objc_msgSend P/Invoke stubs found as compiled methods in R2R map.");
        return 100;
    }
}
