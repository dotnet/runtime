// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace System.Reflection.Metadata
{
    public sealed class DebugMetadataHeader
    {
        public ImmutableArray<byte> Id { get; }
        public MethodDefinitionHandle EntryPoint { get; }

        /// <summary>
        /// Gets the offset (in bytes) from the start of the metadata blob to the start of the <see cref="Id"/> blob.
        /// </summary>
        public int IdStartOffset { get; }

        internal DebugMetadataHeader(ImmutableArray<byte> id, MethodDefinitionHandle entryPoint, int idStartOffset)
        {
            Id = id;
            EntryPoint = entryPoint;
            IdStartOffset = idStartOffset;
        }
    }
}
