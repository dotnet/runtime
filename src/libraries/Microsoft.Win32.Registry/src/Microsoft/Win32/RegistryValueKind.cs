// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Win32
{
#if REGISTRY_ASSEMBLY
    public
#else
    internal
#endif
    enum RegistryValueKind
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
