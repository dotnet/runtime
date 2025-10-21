// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace GenerateUnmanagedEntryPoints
{
    unsafe class Tests : IDisposable
    {
        [UnmanagedCallersOnly(EntryPoint = "MainAssemblyMethod")]
        static void MainAssemblyMethod() => Console.WriteLine($"Hello from {nameof(MainAssemblyMethod)}");

        private IntPtr programHandle;

        public Tests()
        {
            programHandle = NativeLibrary.GetMainProgramHandle();
            Assert.NotEqual(IntPtr.Zero, programHandle);
        }

        [Fact]
        public void ExportFromMainAssembly_IsExported()
        {
            IntPtr methodAddress = IntPtr.Zero;
            bool found = NativeLibrary.TryGetExport(programHandle, "MainAssemblyMethod", out methodAddress);
            Assert.True(found);
            Assert.NotEqual(IntPtr.Zero, methodAddress);
            var MainAssemblyMethodPtr = (delegate* unmanaged<void>)methodAddress;
            MainAssemblyMethodPtr();
        }

        [Fact]
        public void ExportFromUnmanagedEntryPointsAssembly_IsExported()
        {
            IntPtr methodAddress = IntPtr.Zero;
            bool found = NativeLibrary.TryGetExport(programHandle, "ReferencedAssembly1Method", out methodAddress);
            Assert.True(found);
            Assert.NotEqual(IntPtr.Zero, methodAddress);
            var ReferencedAssembly1MethodPtr = (delegate* unmanaged<void>)methodAddress;
            ReferencedAssembly1MethodPtr();
        }

        [Fact]
        public void ExportFromOtherAssembly_IsNotExported()
        {
            IntPtr methodAddress = IntPtr.Zero;
            bool found = NativeLibrary.TryGetExport(programHandle, "ReferencedAssembly2Method", out methodAddress);
            Assert.False(found);
            Assert.Equal(IntPtr.Zero, methodAddress);
        }

        public void Dispose()
        {
        }
    }
}
