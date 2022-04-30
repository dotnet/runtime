// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.AccessControl;

namespace Microsoft.Win32
{
    public static class RegistryAclExtensions
    {
        public static RegistrySecurity GetAccessControl(this RegistryKey key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return key.GetAccessControl();
        }

        public static RegistrySecurity GetAccessControl(this RegistryKey key, AccessControlSections includeSections)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return key.GetAccessControl(includeSections);
        }

        public static void SetAccessControl(this RegistryKey key, RegistrySecurity registrySecurity)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            key.SetAccessControl(registrySecurity);
        }
    }
}
