// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

static class Program
{
    static readonly ConditionalWeakTable<object, object> s_weakTable = new();
    static readonly object s_inTableObject = new();
    static readonly object s_notInTableObject = new();

    static volatile bool s_testPass;

    static unsafe int Main()
    {
        s_weakTable.Add(s_inTableObject, new object());

        Console.WriteLine("RhRegisterGcCallout");
        RuntimeImports.RhRegisterGcCallout(RuntimeImports.GcRestrictedCalloutKind.AfterMarkPhase,
            (IntPtr)(delegate* unmanaged<uint, void>)&GcCallback);

        Console.WriteLine("GC.Collect");
        GC.Collect();

        Console.WriteLine("Test passed: " + s_testPass);
        return s_testPass ? 100 : 1;
    }

    [UnmanagedCallersOnly]
    static void GcCallback(uint uiCondemnedGeneration)
    {
        s_testPass = s_weakTable.TryGetValue(s_inTableObject, out object _) &&
                    !s_weakTable.TryGetValue(s_notInTableObject, out object _);
    }
}