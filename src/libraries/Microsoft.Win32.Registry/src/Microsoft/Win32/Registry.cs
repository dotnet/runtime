// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.Win32
{
    /// <summary>Registry encapsulation. Contains members representing all top level system keys.</summary>
#if REGISTRY_ASSEMBLY
    public
#else
    internal
#endif
    static class Registry
    {
        /// <summary>Current User Key. This key should be used as the root for all user specific settings.</summary>
        public static readonly RegistryKey CurrentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        /// <summary>Local Machine key. This key should be used as the root for all machine specific settings.</summary>
        public static readonly RegistryKey LocalMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
        
        /// <summary>Classes Root Key. This is the root key of class information.</summary>
        public static readonly RegistryKey ClassesRoot = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default);

        /// <summary>Users Root Key. This is the root of users.</summary>
        public static readonly RegistryKey Users = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default);

        /// <summary>Performance Root Key. This is where dynamic performance data is stored on NT.</summary>
        public static readonly RegistryKey PerformanceData = RegistryKey.OpenBaseKey(RegistryHive.PerformanceData, RegistryView.Default);

        /// <summary>Current Config Root Key. This is where current configuration information is stored.</summary>
        public static readonly RegistryKey CurrentConfig = RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, RegistryView.Default);

        public static object GetValue(string keyName, string valueName, object defaultValue)
        {
            string subKeyName;
            RegistryKey basekey = GetBaseKeyFromKeyName(keyName, out subKeyName);

            using (RegistryKey key = basekey.OpenSubKey(subKeyName))
            {
                return key?.GetValue(valueName, defaultValue);
            }
        }

        public static void SetValue(string keyName, string valueName, object value)
        {
            SetValue(keyName, valueName, value, RegistryValueKind.Unknown);
        }

        public static void SetValue(string keyName, string valueName, object value, RegistryValueKind valueKind)
        {
            string subKeyName;
            RegistryKey basekey = GetBaseKeyFromKeyName(keyName, out subKeyName);

            using (RegistryKey key = basekey.CreateSubKey(subKeyName))
            {
                Debug.Assert(key != null, "An exception should be thrown if failed!");
                key.SetValue(valueName, value, valueKind);
            }
        }

        /// <summary>
        /// Parse a keyName and returns the basekey for it.
        /// It will also store the subkey name in the out parameter.
        /// If the keyName is not valid, we will throw ArgumentException.
        /// The return value shouldn't be null. 
        /// </summary>
        private static RegistryKey GetBaseKeyFromKeyName(string keyName, out string subKeyName)
        {
            if (keyName == null)
            {
                throw new ArgumentNullException(nameof(keyName));
            }

            int i = keyName.IndexOf('\\');
            string basekeyName = i != -1 ?
                keyName.Substring(0, i).ToUpperInvariant() :
                keyName.ToUpperInvariant();

            RegistryKey basekey = null;
            switch (basekeyName)
            {
                case "HKEY_CURRENT_USER":
                    basekey = CurrentUser;
                    break;
                case "HKEY_LOCAL_MACHINE":
                    basekey = LocalMachine;
                    break;
                case "HKEY_CLASSES_ROOT":
                    basekey = ClassesRoot;
                    break;
                case "HKEY_USERS":
                    basekey = Users;
                    break;
                case "HKEY_PERFORMANCE_DATA":
                    basekey = PerformanceData;
                    break;
                case "HKEY_CURRENT_CONFIG":
                    basekey = CurrentConfig;
                    break;
                default:
                    throw new ArgumentException(SR.Format(SR.Arg_RegInvalidKeyName, nameof(keyName)), nameof(keyName));
            }

            subKeyName = (i == -1 || i == keyName.Length) ?
                string.Empty :
                keyName.Substring(i + 1, keyName.Length - i - 1);

            return basekey;
        }
    }
}
