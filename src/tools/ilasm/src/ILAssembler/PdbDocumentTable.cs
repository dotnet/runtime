// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace ILAssembler;

/// <summary>
/// The source documents of a compilation's Portable PDB, in the order they were added.
/// </summary>
/// <remarks>
/// Sequence points refer to a document by its index in this table
/// (<see cref="EntityRegistry.SequencePoint.DocumentIndex"/>). The PDB's Document table lists the documents in
/// this order.
/// </remarks>
internal sealed class PdbDocumentTable
{
    private readonly List<PdbDocument> _documents = new();
    private readonly Dictionary<(string Name, Guid Language), int> _indices = new();

    /// <summary>Gets the documents in the order they were added.</summary>
    public IReadOnlyList<PdbDocument> Documents => _documents;

    /// <summary>
    /// Gets the index of the document with this name and language, adding the document at the end of the table
    /// if it is not there yet.
    /// </summary>
    public int GetOrAdd(string name, Guid language)
    {
        if (!_indices.TryGetValue((name, language), out int index))
        {
            index = _documents.Count;
            _documents.Add(new PdbDocument(name, language));
            _indices.Add((name, language), index);
        }

        return index;
    }
}

/// <summary>
/// A source document of the Portable PDB: its name and the GUID of its language, or <see cref="Guid.Empty"/>
/// when the language is not known.
/// </summary>
internal readonly record struct PdbDocument(string Name, Guid Language);
