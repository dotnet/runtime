// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class SspiCli
    {
        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.SspiCli)]
        internal static partial int LsaDeregisterLogonProcess(IntPtr LsaHandle);
    }
}
