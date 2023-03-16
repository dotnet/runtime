// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GenerateUnmanagedEntryPoints
{
    public class ClassLibrary
    {
        [UnmanagedCallersOnly(EntryPoint = "SharedLibraryAssemblyMethod", CallConvs = new Type[] { typeof(CallConvStdcall) })]
        static void SharedLibraryAssemblyMethod() => Console.WriteLine($"Hello from {nameof(SharedLibraryAssemblyMethod)}");
    }
}