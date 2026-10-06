// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace ILAssembler;

/// <summary>
/// The source documents of a compilation's Portable PDB, in the order they were first defined.
/// </summary>
/// <remarks>
/// <para>
/// As in native ilasm, a document is defined when the assembler first meets its name: each input file when its
/// parsing begins, and each file named by a <c>.line</c> or <c>#line</c> directive when the directive is applied,
/// whether or not a sequence point ever refers to it. A document is identified by its name alone; defining it
/// again returns the existing document, which keeps the language it was first defined with.
/// </para>
/// <para>
/// Sequence points refer to a document by its index in this table
/// (<see cref="EntityRegistry.SequencePoint.DocumentIndex"/>). The PDB's Document table lists the documents in
/// this order.
/// </para>
/// </remarks>
internal sealed class PdbDocumentTable
{
    /// <summary>
    /// The language of a document defined before any <c>.language</c> directive: IL assembly
    /// (<c>CorSym_LanguageType_ILAssembly</c> in src/coreclr/inc/corsym.idl), as in native ilasm.
    /// </summary>
    public static readonly Guid ILAssemblyLanguage = new("af046cd3-d0e1-11d2-977c-00a0c9b4d50c");

    private readonly List<PdbDocument> _documents = new();
    private readonly Dictionary<string, int> _indices = new(StringComparer.Ordinal);

    /// <summary>Gets the documents in the order they were first defined.</summary>
    public IReadOnlyList<PdbDocument> Documents => _documents;

    /// <summary>
    /// Gets the index of the document with this name, first adding it at the end of the table with this
    /// language if no document has the name yet.
    /// </summary>
    public int GetOrAdd(string name, Guid language)
    {
        if (!_indices.TryGetValue(name, out int index))
        {
            index = _documents.Count;
            _documents.Add(new PdbDocument(name, language));
            _indices.Add(name, index);
        }

        return index;
    }
}

/// <summary>
/// A source document of the Portable PDB: its name and the GUID of the language current when it was first defined
/// (<see cref="PdbDocumentTable.ILAssemblyLanguage"/> unless a <c>.language</c> directive set another; it is
/// <see cref="Guid.Empty"/> only if such a directive gave the empty GUID).
/// </summary>
internal readonly record struct PdbDocument(string Name, Guid Language);
