// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public sealed class DebugMetadataHeader
    {
        public ImmutableArray<byte> Id { get; }
        public MethodDefinitionHandle EntryPoint { get; }

        internal DebugMetadataHeader(ImmutableArray<byte> id, MethodDefinitionHandle entryPoint)
        {
            Id = id;
            EntryPoint = entryPoint;
        }
    }
}
