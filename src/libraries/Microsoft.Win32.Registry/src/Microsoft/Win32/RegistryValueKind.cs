// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Win32
{
    public enum RegistryValueKind
    {
        String = Interop.Advapi32.RegistryValues.REG_SZ,
        ExpandString = Interop.Advapi32.RegistryValues.REG_EXPAND_SZ,
        Binary = Interop.Advapi32.RegistryValues.REG_BINARY,
        DWord = Interop.Advapi32.RegistryValues.REG_DWORD,
        MultiString = Interop.Advapi32.RegistryValues.REG_MULTI_SZ,
        QWord = Interop.Advapi32.RegistryValues.REG_QWORD,
        Unknown = 0,                          // REG_NONE is defined as zero but BCL
        None = unchecked((int)0xFFFFFFFF), //  mistakenly overrode this value.  
    }   // Now instead of using Interop.Kernel32.RegistryValues.REG_NONE we use "-1".
}
