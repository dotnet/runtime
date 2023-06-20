// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace System.Text.Json.Serialization;

public abstract class JsonDictionaryKeyFilter
{
    public static JsonDictionaryKeyFilter IgnoreMetadataNames { get; } = new JsonIgnoreMetadataNamesDictionaryKeyFilter();

    public abstract bool IgnoreKey(ReadOnlySpan<byte> utf8Key);
}
