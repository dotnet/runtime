// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
**
[System.Runtime.InteropServices.ComVisible(true)]
** A class to hold public guids for document types to be used with the
** symbol store.
**
** 
===========================================================*/
namespace System.Diagnostics.SymbolStore {
    // Only statics does not need to be marked with the serializable attribute
    using System;

[System.Runtime.InteropServices.ComVisible(true)]
    public class SymDocumentType
    {
        public static readonly Guid Text = new Guid(0x5a869d0b, 0x6611, 0x11d3, 0xbd, 0x2a, 0x0, 0x0, 0xf8, 0x8, 0x49, 0xbd);
    }
}
