// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Libraries.Advapi32)]
        internal static unsafe partial uint EventRegister(
            Guid* providerId,
            delegate* unmanaged<Guid*, int, byte, long, long, EVENT_FILTER_DESCRIPTOR*, void*, void> enableCallback,
            void* callbackContext,
            long* registrationHandle);
    }
}
