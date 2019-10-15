// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Win32
{
    public static partial class RegistryAclExtensions
    {
        public static System.Security.AccessControl.RegistrySecurity GetAccessControl(this Microsoft.Win32.RegistryKey key) { throw null; }
        public static System.Security.AccessControl.RegistrySecurity GetAccessControl(this Microsoft.Win32.RegistryKey key, System.Security.AccessControl.AccessControlSections includeSections) { throw null; }
        public static void SetAccessControl(this Microsoft.Win32.RegistryKey key, System.Security.AccessControl.RegistrySecurity registrySecurity) { }
    }
}
