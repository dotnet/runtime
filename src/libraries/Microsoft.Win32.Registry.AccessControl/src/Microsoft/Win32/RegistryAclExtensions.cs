// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.AccessControl;

namespace Microsoft.Win32
{
    public static class RegistryAclExtensions
    {
        public static RegistrySecurity GetAccessControl(RegistryKey key)
        {
            return key.GetAccessControl();
        }

        public static RegistrySecurity GetAccessControl(RegistryKey key, AccessControlSections includeSections)
        {
            return key.GetAccessControl(includeSections);
        }

        public static void SetAccessControl(RegistryKey key, RegistrySecurity registrySecurity)
        {
            key.SetAccessControl(registrySecurity);
        }
    }
}
