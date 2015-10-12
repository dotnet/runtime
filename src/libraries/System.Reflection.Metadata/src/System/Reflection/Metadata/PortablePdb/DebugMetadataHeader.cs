// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public sealed class DebugMetadataHeader
    {
        public readonly ImmutableArray<byte> Id;
        public readonly MethodDefinitionHandle EntryPoint;

        internal DebugMetadataHeader(ImmutableArray<byte> id, MethodDefinitionHandle entryPoint)
        {
            Id = id;
            EntryPoint = entryPoint;
        }
    }
}
