// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Libraries.Advapi32)]
        internal static unsafe partial int EventSetInformation(
            long registrationHandle,
            EVENT_INFO_CLASS informationClass,
            void* eventInformation,
            uint informationLength);
    }
}
