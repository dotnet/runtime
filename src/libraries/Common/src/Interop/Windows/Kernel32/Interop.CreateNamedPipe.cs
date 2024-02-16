// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "CreateNamedPipeW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial SafePipeHandle CreateNamedPipe(
            string pipeName,
            int openMode,
            int pipeMode,
            int maxInstances,
            int outBufferSize,
            int inBufferSize,
            int defaultTimeout,
            ref SECURITY_ATTRIBUTES securityAttributes);
    }
}
