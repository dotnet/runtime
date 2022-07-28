// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Win32
{
    public sealed partial class RegistryKey : MarshalByRefObject, IDisposable
    {
        private static void ClosePerfDataKey()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        private static void FlushCore()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        private static RegistryKey CreateSubKeyInternalCore(string subkey, RegistryKeyPermissionCheck permissionCheck, RegistryOptions registryOptions)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        private static void DeleteSubKeyCore(string subkey, bool throwOnMissingSubKey)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        private static void DeleteSubKeyTreeCore(string subkey)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        private static void DeleteValueCore(string name, bool throwOnMissingValue)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        private static RegistryKey OpenBaseKeyCore(RegistryHive hKey, RegistryView view)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        private static RegistryKey OpenRemoteBaseKeyCore(RegistryHive hKey, string machineName, RegistryView view)
        {
            throw new PlatformNotSupportedException(SR.Security_RegistryPermission); // remote stores not supported on Unix
        }

        private static RegistryKey InternalOpenSubKeyCore(string name, RegistryKeyPermissionCheck permissionCheck, int rights)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        private static RegistryKey InternalOpenSubKeyCore(string name, bool writable)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        internal static RegistryKey InternalOpenSubKeyWithoutSecurityChecksCore(string name, bool writable)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        private static SafeRegistryHandle SystemKeyHandle
        {
            get
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
            }
        }

        private static int InternalSubKeyCountCore()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        private static string[] InternalGetSubKeyNamesCore(int subkeys)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        private static int InternalValueCountCore()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        private static string[] GetValueNamesCore(int values)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        [return: NotNullIfNotNull(nameof(defaultValue))]
        private static object InternalGetValueCore(string? name, object? defaultValue, bool doNotExpand)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        private static RegistryValueKind GetValueKindCore(string? name)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        private static void SetValueCore(string? name, object value, RegistryValueKind valueKind)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        private static int GetRegistryKeyAccess(bool isWritable)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }

        private static int GetRegistryKeyAccess(RegistryKeyPermissionCheck mode)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Registry);
        }
    }
}
