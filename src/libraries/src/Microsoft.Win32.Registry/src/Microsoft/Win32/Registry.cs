// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Win32
{
    /**
     * Registry encapsulation. Contains members representing all top level system
     * keys.
     *
     * @security(checkClassLinking=on)
     */
    //This class contains only static members and does not need to be serializable.
    public static class Registry
    {
        /**
         * Current User Key.
         * 
         * This key should be used as the root for all user specific settings.
         */
        public static readonly RegistryKey CurrentUser = RegistryKey.GetBaseKey(RegistryKey.HKEY_CURRENT_USER);

        /**
         * Local Machine Key.
         * 
         * This key should be used as the root for all machine specific settings.
         */
        public static readonly RegistryKey LocalMachine = RegistryKey.GetBaseKey(RegistryKey.HKEY_LOCAL_MACHINE);

        /**
         * Classes Root Key.
         * 
         * This is the root key of class information.
         */
        public static readonly RegistryKey ClassesRoot = RegistryKey.GetBaseKey(RegistryKey.HKEY_CLASSES_ROOT);

        /**
         * Users Root Key.
         * 
         * This is the root of users.
         */
        public static readonly RegistryKey Users = RegistryKey.GetBaseKey(RegistryKey.HKEY_USERS);

        /**
         * Performance Root Key.
         * 
         * This is where dynamic performance data is stored on NT.
         */
        public static readonly RegistryKey PerformanceData = RegistryKey.GetBaseKey(RegistryKey.HKEY_PERFORMANCE_DATA);

        /**
         * Current Config Root Key.
         * 
         * This is where current configuration information is stored.
         */
        public static readonly RegistryKey CurrentConfig = RegistryKey.GetBaseKey(RegistryKey.HKEY_CURRENT_CONFIG);

        //
        // Following function will parse a keyName and returns the basekey for it.
        // It will also store the subkey name in the out parameter.
        // If the keyName is not valid, we will throw ArgumentException.
        // The return value shouldn't be null. 
        //
        [System.Security.SecurityCritical]  
        private static RegistryKey GetBaseKeyFromKeyName(string keyName, out string subKeyName)
        {
            if (keyName == null)
            {
                throw new ArgumentNullException(nameof(keyName));
            }

            string basekeyName;
            int i = keyName.IndexOf('\\');
            if (i != -1)
            {
                basekeyName = keyName.Substring(0, i).ToUpperInvariant();
            }
            else
            {
                basekeyName = keyName.ToUpperInvariant();
            }
            RegistryKey basekey = null;

            switch (basekeyName)
            {
                case "HKEY_CURRENT_USER":
                    basekey = Registry.CurrentUser;
                    break;
                case "HKEY_LOCAL_MACHINE":
                    basekey = Registry.LocalMachine;
                    break;
                case "HKEY_CLASSES_ROOT":
                    basekey = Registry.ClassesRoot;
                    break;
                case "HKEY_USERS":
                    basekey = Registry.Users;
                    break;
                case "HKEY_PERFORMANCE_DATA":
                    basekey = Registry.PerformanceData;
                    break;
                case "HKEY_CURRENT_CONFIG":
                    basekey = Registry.CurrentConfig;
                    break;
                default:
                    throw new ArgumentException(SR.Format(SR.Arg_RegInvalidKeyName, "keyName"));
            }
            if (i == -1 || i == keyName.Length)
            {
                subKeyName = string.Empty;
            }
            else
            {
                subKeyName = keyName.Substring(i + 1, keyName.Length - i - 1);
            }
            return basekey;
        }

        [System.Security.SecuritySafeCritical]  
        public static object GetValue(string keyName, string valueName, object defaultValue)
        {
            string subKeyName;
            RegistryKey basekey = GetBaseKeyFromKeyName(keyName, out subKeyName);
            Debug.Assert(basekey != null, "basekey can't be null.");
            RegistryKey key = basekey.OpenSubKey(subKeyName);
            if (key == null)
            { // if the key doesn't exist, do nothing
                return null;
            }
            try
            {
                return key.GetValue(valueName, defaultValue);
            }
            finally
            {
                key.Close();
            }
        }

        public static void SetValue(string keyName, string valueName, object value)
        {
            SetValue(keyName, valueName, value, RegistryValueKind.Unknown);
        }

        [System.Security.SecuritySafeCritical]  
        public static void SetValue(string keyName, string valueName, object value, RegistryValueKind valueKind)
        {
            string subKeyName;
            RegistryKey basekey = GetBaseKeyFromKeyName(keyName, out subKeyName);
            Debug.Assert(basekey != null, "basekey can't be null!");
            RegistryKey key = basekey.CreateSubKey(subKeyName);
            Debug.Assert(key != null, "An exception should be thrown if failed!");
            try
            {
                key.SetValue(valueName, value, valueKind);
            }
            finally
            {
                key.Close();
            }
        }
    }
}
