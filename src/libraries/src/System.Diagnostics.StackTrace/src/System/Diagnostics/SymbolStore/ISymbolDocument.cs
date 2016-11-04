// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.SymbolStore
{
    public interface ISymbolDocument
    {
        // Properties of the document.
        string URL { get; }
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
