// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Represents a document referenced by a symbol store. A document is
** defined by a URL and a document type GUID. Using the document type
** GUID and the URL, one can locate the document however it is
** stored. Document source can optionally be stored in the symbol
** store. This interface also provides access to that source if it is
** present.
**
** 
===========================================================*/
namespace System.Diagnostics.SymbolStore {
    
    using System;
    
    // Interface does not need to be marked with the serializable attribute
[System.Runtime.InteropServices.ComVisible(true)]
    public interface ISymbolDocument
    {
        // Properties of the document.
        String URL { get; }
        Guid DocumentType { get; }
    
        // Language of the document.
        Guid Language { get; }
        Guid LanguageVendor { get; }
    
        // Check sum information.
        Guid CheckSumAlgorithmId { get; }
        byte[] GetCheckSum();
    
        // Given a line in this document that may or may not be a sequence
        // point, return the closest line that is a sequence point.
        int FindClosestLine(int line);
        
        // Access to embedded source.
        bool HasEmbeddedSource { get; }
        int SourceLength { get; }
        byte[] GetSourceRange(int startLine, int startColumn,
                                      int endLine, int endColumn);
    }
}
