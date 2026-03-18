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
    // Blittable objc_msgSend declarations — these should be precompiled by R2R
    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_2(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend_stret")]
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

        // Search for objc_msgSend P/Invoke stub entries in the map
        string[] objcLines = mapLines.Where(l => l.Contains("objc_msgSend")).ToArray();

        Console.WriteLine($"Found {objcLines.Length} lines containing objc_msgSend:");
        foreach (string line in objcLines)
        {
            Console.WriteLine($"  {line}");
        }

        bool foundMsgSend = objcLines.Any(l => l.Contains("objc_msgSend"));
        if (!foundMsgSend)
        {
            Console.WriteLine("FAILED: objc_msgSend P/Invoke stub not found in R2R map.");
            Console.WriteLine("This means the P/Invoke was not precompiled by crossgen2.");
            return 1;
        }

        Console.WriteLine("PASSED: objc_msgSend P/Invoke stubs found in R2R map.");
        return 100;
    }
}
