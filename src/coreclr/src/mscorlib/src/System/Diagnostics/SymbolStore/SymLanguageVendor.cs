// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
**
[System.Runtime.InteropServices.ComVisible(true)]
** A class to hold public guids for language vendors.
**
** 
===========================================================*/
namespace System.Diagnostics.SymbolStore {
    // Only statics, does not need to be marked with the serializable attribute
    using System;

[System.Runtime.InteropServices.ComVisible(true)]
    public class SymLanguageVendor
    {
        public static readonly Guid Microsoft = new Guid(unchecked((int)0x994b45c4), unchecked((short) 0xe6e9), 0x11d2, 0x90, 0x3f, 0x00, 0xc0, 0x4f, 0xa3, 0x02, 0xa1);
    }
}
