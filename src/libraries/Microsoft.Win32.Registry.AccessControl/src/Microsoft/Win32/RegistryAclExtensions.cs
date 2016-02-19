// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                throw new ArgumentNullException(nameof(registrySecurity));
            }

            registrySecurity.Persist(key.Handle, key.Name);
        }
    }
}
