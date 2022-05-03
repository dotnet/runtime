// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    // CLRConfig is mainly reading the config switch values. this is used when we cannot use the AppContext class.
    // In general AppContext should be used instead of CLRConfig if there is no reason prevent that.
    internal static partial class CLRConfig
    {
        internal static bool GetBoolValue(string switchName, out bool exist)
        {
            return GetConfigBoolValue(switchName, out exist);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ClrConfig_GetConfigBoolValue", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetConfigBoolValue(string configSwitchName, [MarshalAs(UnmanagedType.Bool)] out bool exist);
    }
}
