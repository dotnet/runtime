// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Ported from RegistryKey.cs and made extension methods (or renamed statics, where necessary) to allow 
// extending the class without Microsoft.Win32.Registry needing to rely on System.Security.AccessControl

using System;
using System.Security;
using System.Security.AccessControl;

namespace Microsoft.Win32
{
    public static class RegistryAclExtensions
    {
        public static RegistrySecurity GetAccessControl(this RegistryKey key)
        {
            return GetAccessControl(key, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        [SecuritySafeCritical]
        public static RegistrySecurity GetAccessControl(this RegistryKey key, AccessControlSections includeSections)
        {
            if (key.Handle == null)
            {
                throw new ObjectDisposedException(key.Name, SR.ObjectDisposed_RegKeyClosed);
            }

            return new RegistrySecurity(key.Handle, key.Name, includeSections);
        }

        [SecuritySafeCritical]
        public static void SetAccessControl(this RegistryKey key, RegistrySecurity registrySecurity)
        {
            if (registrySecurity == null)
            {
                throw new ArgumentNullException("registrySecurity");
            }

            registrySecurity.Persist(key.Handle, key.Name);
        }
    }
}
