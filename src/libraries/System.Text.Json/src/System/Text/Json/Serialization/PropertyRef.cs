// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    internal readonly struct PropertyRef
    {
        public PropertyRef(ulong key, JsonPropertyInfo info)
        {
            Key = key;
            Info = info;
        }

        // The first 6 bytes are the first part of the name and last 2 bytes are the name's length.
        public readonly ulong Key;

        public readonly JsonPropertyInfo Info;
    }
}
