// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ReferencedAssembly1
{
    public class ClassLibrary
    {
        [UnmanagedCallersOnly(EntryPoint = "ReferencedAssembly1Method")]
        public static void ReferencedAssembly1Method() => Console.WriteLine($"Hello from {nameof(ReferencedAssembly1Method)}");
    }
}