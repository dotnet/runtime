// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** Purpose: File attribute flags corresponding to NT's flags.
**
** 
===========================================================*/
using System;

namespace System.IO {
    // File attributes for use with the FileEnumerator class.
    // These constants correspond to the constants in WinNT.h.
    // 
[Serializable]
    [Flags]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum FileAttributes
    {
        // From WinNT.h (FILE_ATTRIBUTE_XXX)
        ReadOnly = 0x1,
        Hidden = 0x2,
        System = 0x4,
        Directory = 0x10,
        Archive = 0x20,
        Device = 0x40,
        Normal = 0x80,
        Temporary = 0x100,
        SparseFile = 0x200,
        ReparsePoint = 0x400,
        Compressed = 0x800,
        Offline = 0x1000,
        NotContentIndexed = 0x2000,
        Encrypted = 0x4000,

#if !FEATURE_CORECLR
#if FEATURE_COMINTEROP
        [System.Runtime.InteropServices.ComVisible(false)]        
#endif // FEATURE_COMINTEROP
        IntegrityStream = 0x8000,
        
#if FEATURE_COMINTEROP
        [System.Runtime.InteropServices.ComVisible(false)]        
#endif // FEATURE_COMINTEROP
        NoScrubData = 0x20000,
#endif
    }
}
