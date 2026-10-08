// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Debuggee for cDAC dump tests — exercises stack reference enumeration.
/// Creates objects on the stack that should be reported as GC references,
/// then crashes with them still live. The test walks the stack and verifies
/// the expected references are found.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Marker string that tests can search for in the reported GC references
    /// to verify that stack refs are being enumerated correctly.
    /// </summary>
    public const string MarkerValue = "cDAC-StackRefs-Marker-12345";

    private static void Main()
    {
        MethodWithStackRefs();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MethodWithStackRefs()
    {
        // These locals will be GC-tracked in the JIT's GCInfo.
        // The string has a known value we can find in the dump.
        string marker = MarkerValue;
        int[] array = [1, 2, 3, 4, 5];
        object boxed = 42;

        // Force the JIT to keep them alive at the FailFast call site.
        GC.KeepAlive(marker);
        GC.KeepAlive(array);
        GC.KeepAlive(boxed);

        Environment.FailFast("cDAC dump test: StackRefs debuggee intentional crash");
    }
}
