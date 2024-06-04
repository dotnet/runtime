// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "RaiseFailFastException")]
        [DoesNotReturn]
        public static unsafe partial void RaiseFailFastException(IntPtr pExceptionRecord, IntPtr pContextRecord, uint dwFlags);
    }
}
