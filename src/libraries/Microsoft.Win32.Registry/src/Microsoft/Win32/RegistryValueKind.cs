// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Win32
{
    public enum RegistryValueKind
    {
        String = Interop.mincore.RegistryValues.REG_SZ,
        ExpandString = Interop.mincore.RegistryValues.REG_EXPAND_SZ,
        Binary = Interop.mincore.RegistryValues.REG_BINARY,
        DWord = Interop.mincore.RegistryValues.REG_DWORD,
        MultiString = Interop.mincore.RegistryValues.REG_MULTI_SZ,
        QWord = Interop.mincore.RegistryValues.REG_QWORD,
        Unknown = 0,                          // REG_NONE is defined as zero but BCL
        None = unchecked((int)0xFFFFFFFF), //  mistakenly overrode this value.  
    }   // Now instead of using Interop.mincore.RegistryValues.REG_NONE we use "-1".
}

