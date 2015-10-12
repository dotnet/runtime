// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public sealed class DebugMetadataHeader
    {
        public ImmutableArray<byte> Id { get; private set; }
        public MethodDefinitionHandle EntryPoint { get; private set; }

        internal DebugMetadataHeader(ImmutableArray<byte> id, MethodDefinitionHandle entryPoint)
        {
            Id = id;
            EntryPoint = entryPoint;
        }
    }
}
