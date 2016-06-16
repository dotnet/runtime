// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Security;
using System.Security.AccessControl;

namespace Microsoft.Win32
{
    public sealed partial class RegistryKey : IDisposable
    {
        [SecuritySafeCritical]
        private void ClosePerfDataKey()
        {
            // TODO: Implement this
            throw new PlatformNotSupportedException();
        }

        [SecuritySafeCritical]
        private void FlushCore()
        {
            // TODO: Implement this
            throw new PlatformNotSupportedException();
        }

        [SecuritySafeCritical]
        private RegistryKey CreateSubKeyInternalCore(string subkey, bool writable, RegistryOptions registryOptions)
        {
            // TODO: Implement this
            throw new PlatformNotSupportedException();
        }

        [SecuritySafeCritical]
        private void DeleteSubKeyCore(string subkey, bool throwOnMissingSubKey)
        {
            // TODO: Implement this
            throw new PlatformNotSupportedException();
        }

        [SecuritySafeCritical]
        private void DeleteSubKeyTreeCore(string subkey)
        {
            // TODO: Implement this
            throw new PlatformNotSupportedException();
        }

        [SecuritySafeCritical]
        private void DeleteValueCore(string name, bool throwOnMissingValue)
        {
            // TODO: Implement this
            throw new PlatformNotSupportedException();
        }

        private static RegistryKey OpenBaseKeyCore(RegistryHive hKey, RegistryView view)
        {
            // TODO: Implement this
            throw new PlatformNotSupportedException();
        }

        [SecuritySafeCritical]
        private static RegistryKey OpenRemoteBaseKeyCore(RegistryHive hKey, string machineName, RegistryView view)
        {
            throw new PlatformNotSupportedException(SR.Security_RegistryPermission); // remote stores not supported on Unix
        }

        [SecurityCritical]
        private RegistryKey InternalOpenSubKeyCore(string name, RegistryRights rights, bool throwOnPermissionFailure)
        {
            // TODO: Implement this
            throw new PlatformNotSupportedException();
        }

        private SafeRegistryHandle SystemKeyHandle
        {
            [SecurityCritical]
            get
            {
                // TODO: Implement this
                throw new PlatformNotSupportedException();
            }
        }

        [SecurityCritical]
        private int InternalSubKeyCountCore()
        {
            // TODO: Implement this
            throw new PlatformNotSupportedException();
        }

        [SecurityCritical]
        private string[] InternalGetSubKeyNamesCore(int subkeys)
        {
            // TODO: Implement this
            throw new PlatformNotSupportedException();
        }

        [SecurityCritical]
        private int InternalValueCountCore()
        {
            // TODO: Implement this
            throw new PlatformNotSupportedException();
        }

        [SecuritySafeCritical]
        private string[] GetValueNamesCore(int values)
        {
            // TODO: Implement this
            throw new PlatformNotSupportedException();
        }

        [SecurityCritical]
        private Object InternalGetValueCore(string name, Object defaultValue, bool doNotExpand)
        {
            // TODO: Implement this
            throw new PlatformNotSupportedException();
        }

        [SecuritySafeCritical]
        private RegistryValueKind GetValueKindCore(string name)
        {
            // TODO: Implement this
            throw new PlatformNotSupportedException();
        }

        [SecuritySafeCritical]
        private void SetValueCore(string name, Object value, RegistryValueKind valueKind)
        {
            try
            { 
                // TODO: Implement this
                throw new PlatformNotSupportedException();
            }
            catch (Exception exc) when (exc is OverflowException || exc is InvalidOperationException || exc is FormatException || exc is InvalidCastException)
            {
                ThrowHelper.ThrowArgumentException(SR.Arg_RegSetMismatchedKind);
            }
        }
    }
}
