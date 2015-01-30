// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.Win32 {
[System.Runtime.InteropServices.ComVisible(true)]
    public enum RegistryValueKind {
        String =        Win32Native.REG_SZ,
        ExpandString =  Win32Native.REG_EXPAND_SZ,
        Binary =        Win32Native.REG_BINARY,
        DWord =         Win32Native.REG_DWORD,
        MultiString =   Win32Native.REG_MULTI_SZ,
        QWord =         Win32Native.REG_QWORD,
        Unknown =       0,                          // REG_NONE is defined as zero but BCL
        [System.Runtime.InteropServices.ComVisible(false)]
        None =          unchecked((int)0xFFFFFFFF), //  mistakingly overrode this value.  
    }   // Now instead of using Win32Native.REG_NONE we use "-1" and play games internally.
}

