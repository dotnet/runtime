// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Metadata
{
    internal readonly struct PropertyRef
    {
        public PropertyRef(ulong key, JsonPropertyInfo info, byte[] nameFromJson)
        {
            Key = key;
            Info = info;
            NameFromJson = nameFromJson;
        }

        public readonly ulong Key;
        public readonly JsonPropertyInfo Info;

        // NameFromJson may be different than Info.NameAsUtf8Bytes when case insensitive is enabled.
        public readonly byte[] NameFromJson;
    }
}
