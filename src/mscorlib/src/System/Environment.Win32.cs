// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    public static partial class Environment
    {
        private static string GetEnvironmentVariableCore(string variable)
        {
            if (AppDomain.IsAppXModel() && !AppDomain.IsAppXDesignMode())
            {
                // Environment variable accessors are not approved modern API.
                // Behave as if the variable was not found in this case.
                return null;
            }

            StringBuilder sb = StringBuilderCache.Acquire(128); // A somewhat reasonable default size
            int requiredSize = Win32Native.GetEnvironmentVariable(variable, sb, sb.Capacity);

            if (requiredSize == 0 && Marshal.GetLastWin32Error() == Win32Native.ERROR_ENVVAR_NOT_FOUND)
            {
                StringBuilderCache.Release(sb);
                return null;
            }

            while (requiredSize > sb.Capacity)
            {
                sb.Capacity = requiredSize;
                sb.Length = 0;
                requiredSize = Win32Native.GetEnvironmentVariable(variable, sb, sb.Capacity);
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        private static string GetEnvironmentVariableCore(string variable, EnvironmentVariableTarget target)
        {
            if (target == EnvironmentVariableTarget.Process)
                return GetEnvironmentVariableCore(variable);

#if FEATURE_WIN32_REGISTRY
            RegistryKey baseKey;
            string keyName;

            if (target == EnvironmentVariableTarget.Machine)
            {
                baseKey = Registry.LocalMachine;
                keyName = @"System\CurrentControlSet\Control\Session Manager\Environment";
            }
            else if (target == EnvironmentVariableTarget.User)
            {
                Debug.Assert(target == EnvironmentVariableTarget.User);
                baseKey = Registry.CurrentUser;
                keyName = "Environment";
            }
            else
            {
                throw new ArgumentException(GetResourceString("Arg_EnumIllegalVal", (int)target));
            }

            using (RegistryKey environmentKey = baseKey.OpenSubKey(keyName, writable: false))
            {
                return environmentKey?.GetValue(variable) as string;
            }
#else
            throw new ArgumentException(GetResourceString("Arg_EnumIllegalVal", (int)target));
#endif
        }

        private static IDictionary GetEnvironmentVariablesCore()
        {
            if (AppDomain.IsAppXModel() && !AppDomain.IsAppXDesignMode())
            {
                // Environment variable accessors are not approved modern API.
                // Behave as if no environment variables are defined in this case.
                return new Hashtable(0);
            }

            return GetRawEnvironmentVariables();
        }

        private static IDictionary GetEnvironmentVariablesCore(EnvironmentVariableTarget target)
        {
            if (target == EnvironmentVariableTarget.Process)
                return GetEnvironmentVariablesCore();

#if FEATURE_WIN32_REGISTRY
            RegistryKey baseKey;
            string keyName;
            if (target == EnvironmentVariableTarget.Machine)
            {
                baseKey = Registry.LocalMachine;
                keyName = @"System\CurrentControlSet\Control\Session Manager\Environment";
            }
            else if (target == EnvironmentVariableTarget.User)
            {
                Debug.Assert(target == EnvironmentVariableTarget.User);
                baseKey = Registry.CurrentUser;
                keyName = @"Environment";
            }
            else
            {
                throw new ArgumentException(GetResourceString("Arg_EnumIllegalVal", (int)target));
            }

            using (RegistryKey environmentKey = baseKey.OpenSubKey(keyName, writable: false))
            {
                var table = new Dictionary<string, string>();
                if (environmentKey != null)
                {
                    foreach (string name in environmentKey.GetValueNames())
                    {
                        table.Add(name, environmentKey.GetValue(name, "").ToString());
                    }
                }
                return table;
            }
#endif // FEATURE_WIN32_REGISTRY

            throw new ArgumentException(GetResourceString("Arg_EnumIllegalVal", (int)target));
        }

        private static void SetEnvironmentVariableCore(string variable, string value)
        {
            // explicitly null out value if is the empty string.
            if (string.IsNullOrEmpty(value) || value[0] == '\0')
                value = null;

            if (AppDomain.IsAppXModel() && !AppDomain.IsAppXDesignMode())
            {
                // Environment variable accessors are not approved modern API.
                // so we throw PlatformNotSupportedException.
                throw new PlatformNotSupportedException();
            }

            if (!Win32Native.SetEnvironmentVariable(variable, value))
            {
                int errorCode = Marshal.GetLastWin32Error();

                switch (errorCode)
                {
                    case Win32Native.ERROR_ENVVAR_NOT_FOUND:
                        // Allow user to try to clear a environment variable
                        return;
                    case Win32Native.ERROR_FILENAME_EXCED_RANGE:
                        // The error message from Win32 is "The filename or extension is too long",
                        // which is not accurate.
                        throw new ArgumentException(GetResourceString("Argument_LongEnvVarValue"));
                    default:
                        throw new ArgumentException(Win32Native.GetMessage(errorCode));
                }
            }
        }

        private static void SetEnvironmentVariableCore(string variable, string value, EnvironmentVariableTarget target)
        {
            if (target == EnvironmentVariableTarget.Process)
            {
                SetEnvironmentVariableCore(variable, value);
                return;
            }

            // explicitly null out value if is the empty string.
            if (string.IsNullOrEmpty(value) || value[0] == '\0')
                value = null;

#if FEATURE_WIN32_REGISTRY
            RegistryKey baseKey;
            string keyName;

            if (target == EnvironmentVariableTarget.Machine)
            {
                baseKey = Registry.LocalMachine;
                keyName = @"System\CurrentControlSet\Control\Session Manager\Environment";
            }
            else if (target == EnvironmentVariableTarget.User)
            {
                Debug.Assert(target == EnvironmentVariableTarget.User);

                // User-wide environment variables stored in the registry are limited to 255 chars for the environment variable name.
                const int MaxUserEnvVariableLength = 255;
                if (variable.Length >= MaxUserEnvVariableLength)
                {
                    throw new ArgumentException(GetResourceString("Argument_LongEnvVarValue"), nameof(variable));
                }

                baseKey = Registry.CurrentUser;
                keyName = "Environment";
            }
            else
            {
                throw new ArgumentException(GetResourceString("Arg_EnumIllegalVal", (int)target));
            }

            using (RegistryKey environmentKey = baseKey.OpenSubKey(keyName, writable: true))
            {
                if (environmentKey != null)
                {
                    if (value == null)
                    {
                        environmentKey.DeleteValue(variable, throwOnMissingValue: false);
                    }
                    else
                    {
                        environmentKey.SetValue(variable, value);
                    }
                }
            }

            // send a WM_SETTINGCHANGE message to all windows
            IntPtr r = Win32Native.SendMessageTimeout(new IntPtr(Win32Native.HWND_BROADCAST), Win32Native.WM_SETTINGCHANGE, IntPtr.Zero, "Environment", 0, 1000, IntPtr.Zero);
            if (r == IntPtr.Zero) Debug.Assert(false, "SetEnvironmentVariable failed: " + Marshal.GetLastWin32Error());

#else // FEATURE_WIN32_REGISTRY
            throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)target));
#endif
        }
    }
}