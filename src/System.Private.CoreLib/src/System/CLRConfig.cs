// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32;

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

        internal static bool GetBoolValueWithFallbacks(string switchName, string environmentName, bool defaultValue)
        {
            bool value = GetBoolValue(switchName, out bool exists);

            if (exists)
                return value;

            // Calls to this API can be very early- we want to avoid using higher-level
            // abstractions where reasonably possible.

            Span<char> buffer = stackalloc char[32];
            int length = Interop.Kernel32.GetEnvironmentVariable(environmentName, buffer);
            switch (length)
            {
                case 1:
                    if (buffer[0] == '0')
                        return false;
                    if (buffer[0] == '1')
                        return true;
                    break;
                case 4:
                    if (bool.IsTrueStringIgnoreCase(buffer.Slice(0, 4)))
                        return true;
                    break;
                case 5:
                    if (bool.IsFalseStringIgnoreCase(buffer.Slice(0, 5)))
                        return false;
                    break;
            }

            return defaultValue;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern bool GetConfigBoolValue(string configSwitchName, out bool exist);
    }
}
