// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;

namespace System.Security.AccessControl
{
    // We derived this enum from the definitions of KEY_READ and such from
    // winnt.h and from MSDN, plus some experimental validation with regedit.
    // https://docs.microsoft.com/en-us/windows/desktop/SysInfo/registry-key-security-and-access-rights
    [Flags]
    public enum RegistryRights
    {
        // No None field - An ACE with the value 0 cannot grant nor deny.
        QueryValues = Interop.Advapi32.RegistryOperations.KEY_QUERY_VALUE,          // 0x0001 query the values of a registry key
        SetValue = Interop.Advapi32.RegistryOperations.KEY_SET_VALUE,            // 0x0002 create, delete, or set a registry value
        CreateSubKey = Interop.Advapi32.RegistryOperations.KEY_CREATE_SUB_KEY,       // 0x0004 required to create a subkey of a specific key
        EnumerateSubKeys = Interop.Advapi32.RegistryOperations.KEY_ENUMERATE_SUB_KEYS,   // 0x0008 required to enumerate sub keys of a key
        Notify = Interop.Advapi32.RegistryOperations.KEY_NOTIFY,               // 0x0010 needed to request change notifications
        CreateLink = Interop.Advapi32.RegistryOperations.KEY_CREATE_LINK,          // 0x0020 reserved for system use
        ///
        /// The Windows Kernel team agrees that it was a bad design to expose the WOW64_n options as permissions.
        /// in the .NET Framework these options are exposed via the RegistryView enum
        ///
        ///        Reg64             = Interop.Advapi32.RegistryOptions.KEY_WOW64_64KEY,          // 0x0100 operate on the 64-bit registry view
        ///        Reg32             = Interop.Advapi32.RegistryOptions.KEY_WOW64_32KEY,          // 0x0200 operate on the 32-bit registry view
        ExecuteKey = ReadKey,
        ReadKey = Interop.Advapi32.RegistryOperations.STANDARD_RIGHTS_READ | QueryValues | EnumerateSubKeys | Notify,
        WriteKey = Interop.Advapi32.RegistryOperations.STANDARD_RIGHTS_WRITE | SetValue | CreateSubKey,
        Delete = 0x10000,
        ReadPermissions = 0x20000,
        ChangePermissions = 0x40000,
        TakeOwnership = 0x80000,
        FullControl = 0xF003F | Interop.Advapi32.RegistryOperations.STANDARD_RIGHTS_READ | Interop.Advapi32.RegistryOperations.STANDARD_RIGHTS_WRITE
    }
}
