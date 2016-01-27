// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Represents a document referenced by a symbol store. A document is
** defined by a URL and a document type GUID. Document source can
** optionally be stored in the symbol store.
**
** 
===========================================================*/
namespace System.Diagnostics.SymbolStore {
    
    using System;
    
    // Interface does not need to be marked with the serializable attribute
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface ISymbolDocumentWriter
    {
        // SetSource will store the raw source for a document into the
        // symbol store. An array of unsigned bytes is used instead of
        // character data to accommodate a wider variety of "source".
        void SetSource(byte[] source);
    
        // Check sum support.
        void SetCheckSum(Guid algorithmId, byte[] checkSum);
    }
}
