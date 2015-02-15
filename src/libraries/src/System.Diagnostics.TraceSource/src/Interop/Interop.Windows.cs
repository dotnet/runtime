// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    private const string WinCoreProcessThreads = "api-ms-win-core-processthreads-l1-1-0.dll";

    [DllImport(WinCoreProcessThreads)]
    internal extern static uint GetCurrentProcessId();
}
