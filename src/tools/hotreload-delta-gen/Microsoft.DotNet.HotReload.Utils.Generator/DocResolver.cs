// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.HotReload.Utils.Generator;

/// Maps a source file path to a DocumentId in a given Project
public class DocResolver {

    private readonly Project project;

    private readonly ImmutableDictionary<string,DocumentId> docMap;
    public DocResolver(Project project) {
        this.project = project;
        this.docMap = BuildDocMap (project.Documents);
    }

    public Project Project { get => project; }

    private static ImmutableDictionary<string, DocumentId> BuildDocMap (IEnumerable<Document> docs)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, DocumentId>();
        foreach (var doc in docs) {
            var key = doc.FilePath;
            var value = doc.Id;
            var kvp = KeyValuePair.Create(key!, value);
            builder.Add(kvp);
        }
        return builder.ToImmutable();
    }

    public bool TryResolveDocumentId (string relativePath, [NotNullWhen(true)] out DocumentId id) {
        var absolutePath = Path.GetFullPath(relativePath);
        return docMap.TryGetValue(absolutePath, out id!);
    }

}

