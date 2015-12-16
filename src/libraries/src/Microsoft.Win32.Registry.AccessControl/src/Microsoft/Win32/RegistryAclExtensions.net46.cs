// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
