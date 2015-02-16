// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Win32
{
    public enum RegistryValueKind
    {
        String = Interop.REG_SZ,
        ExpandString = Interop.REG_EXPAND_SZ,
        Binary = Interop.REG_BINARY,
        DWord = Interop.REG_DWORD,
        MultiString = Interop.REG_MULTI_SZ,
        QWord = Interop.REG_QWORD,
        Unknown = 0,                          // REG_NONE is defined as zero but BCL
        None = unchecked((int)0xFFFFFFFF), //  mistakingly overrode this value.  
    }   // Now instead of using Interop.REG_NONE we use "-1".
}

