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
            ArgumentNullException.ThrowIfNull(key);

            return key.GetAccessControl();
        }

        public static RegistrySecurity GetAccessControl(this RegistryKey key, AccessControlSections includeSections)
        {
            ArgumentNullException.ThrowIfNull(key);

            return key.GetAccessControl(includeSections);
        }

        public static void SetAccessControl(this RegistryKey key, RegistrySecurity registrySecurity)
        {
            ArgumentNullException.ThrowIfNull(key);

            key.SetAccessControl(registrySecurity);
        }
    }
}
