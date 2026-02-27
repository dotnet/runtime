// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the BuiltInCOM contract.
/// Creates a managed object and crashes, allowing the TraverseRCWCleanupList
/// API to be validated (the cleanup list will be empty for a non-COM program).
/// </summary>
internal static class Program
{
    private static void Main()
    {
        // Allocate some objects to ensure the runtime is initialized
        object[] objects = new object[10];
        for (int i = 0; i < objects.Length; i++)
            objects[i] = new object();
        GC.KeepAlive(objects);

        Environment.FailFast("cDAC dump test: BuiltInCOM debuggee intentional crash");
    }
}
