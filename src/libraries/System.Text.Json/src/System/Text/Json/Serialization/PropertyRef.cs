// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    internal readonly struct PropertyRef
    {
        public PropertyRef(ulong key, JsonPropertyInfo info, byte[] name)
        {
            Key = key;
            Info = info;
            Name = name;
        }

        public readonly ulong Key;
        public readonly JsonPropertyInfo Info;

        // Name may be different than Info.NameAsUtf8 when case insensitive is enabled.
        public readonly byte[] Name;
    }
}
