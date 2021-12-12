// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [GeneratedDllImport(Libraries.Advapi32, EntryPoint = "ChangeServiceConfig2W", ExactSpelling = true, SetLastError = true)]
        public static partial bool ChangeServiceConfig2(SafeServiceHandle serviceHandle, uint infoLevel, ref SERVICE_DESCRIPTION serviceDesc);

#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we support non-blittable types.
        [DllImport(Libraries.Advapi32, EntryPoint = "ChangeServiceConfig2W", ExactSpelling = true, SetLastError = true)]
        public static extern bool ChangeServiceConfig2(SafeServiceHandle serviceHandle, uint infoLevel, ref SERVICE_DELAYED_AUTOSTART_INFO serviceDesc);
#pragma warning restore DLLIMPORTGENANALYZER015
    }
}
