// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.AccessControl;

namespace Microsoft.Win32
{
    public static class RegistryAclExtensions
    {
        public static RegistrySecurity GetAccessControl(this RegistryKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return key.GetAccessControl();
        }

        public static RegistrySecurity GetAccessControl(this RegistryKey key, AccessControlSections includeSections)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return key.GetAccessControl(includeSections);
        }

        public static void SetAccessControl(this RegistryKey key, RegistrySecurity registrySecurity)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            key.SetAccessControl(registrySecurity);
        }
    }
}
