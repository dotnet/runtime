// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Pipe, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false, EntryPoint = "CreateNamedPipeW")]
        internal static extern SafePipeHandle CreateNamedPipe(
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
