// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Security;

namespace System
{
    // CLRConfig is mainly reading the config switch values. this is used when we cannot use the AppContext class
    // one example, is using the context switch in the globalization code which require to read the switch very
    // early even before the appdomain get initialized.
    // In general AppContext should be used instead of CLRConfig if there is no reason prevent that.
    internal class CLRConfig
    {
        internal static bool GetBoolValue(string switchName, out bool exist)
        {
            return GetConfigBoolValue(switchName, out exist);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern bool GetConfigBoolValue(string configSwitchName, out bool exist);
    }
}  // namespace System

// file CLRConfig
