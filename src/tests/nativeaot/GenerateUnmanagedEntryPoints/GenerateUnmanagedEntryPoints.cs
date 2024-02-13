// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace GenerateUnmanagedEntryPoints
{
    public unsafe class Program
    {
        [UnmanagedCallersOnly(EntryPoint = "MainAssemblyMethod")]
        static void MainAssemblyMethod() => Console.WriteLine($"Hello from {nameof(MainAssemblyMethod)}");

        [Fact]
        public static int TestEntryPoint()
        {
            IntPtr methodAddress = IntPtr.Zero;
            IntPtr programHandle = IntPtr.Zero;
            
            programHandle = NativeLibrary.GetMainProgramHandle();
            if (programHandle == IntPtr.Zero)
            {
                return 1;
            }

            if (NativeLibrary.TryGetExport(programHandle, "MainAssemblyMethod", out methodAddress))
            {
                var MainAssemblyMethodPtr = (delegate* unmanaged <void>) methodAddress;
                MainAssemblyMethodPtr();
            }
            else
            {
                return 2;
            }

            if (NativeLibrary.TryGetExport(programHandle, "ReferencedAssembly1Method", out methodAddress))
            {
                var ReferencedAssembly1MethodPtr = (delegate* unmanaged <void>) methodAddress;
                ReferencedAssembly1MethodPtr();
            }
            else
            {
                return 3;
            }

            if (NativeLibrary.TryGetExport(programHandle, "ReferencedAssembly2Method", out methodAddress))
            {
                // must not be exposed from ReferencedAssembly2 assembly
                return 4;
            }

            return 100;
        }
    }
}
