// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization;

/// <summary>
/// Determines dictionary key filter to ignore metadata keys starting with $, such as `$schema`.
/// </summary>
internal sealed class JsonIgnoreMetadataNamesDictionaryKeyFilter : JsonDictionaryKeyFilter
{

    /// <summary>
    /// Ignores any metadata keys starting with $, such as `$schema`.
    /// </summary>
    public override bool IgnoreKey(ReadOnlySpan<byte> utf8Key) => utf8Key.StartsWith("$"u8);
}
