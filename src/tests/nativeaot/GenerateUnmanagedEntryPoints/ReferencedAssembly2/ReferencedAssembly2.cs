// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ReferencedAssembly2
{
    public class ClassLibrary
    {
        [UnmanagedCallersOnly(EntryPoint = "ReferencedAssembly2Method")]
        public static void ReferencedAssembly2Method() => Console.WriteLine($"Hello from {nameof(ReferencedAssembly2Method)}");
    }
}