// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Build.Utilities;

#nullable enable

internal sealed class CompilerCache
{
    public CompilerCache() => FileHashes = new();
    public CompilerCache(IDictionary<string, string> oldHashes)
        => FileHashes = new(oldHashes);

    [JsonPropertyName("file_hashes")]
    public ConcurrentDictionary<string, string> FileHashes { get; set; }
}
