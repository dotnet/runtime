// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection.Metadata;

namespace System.Reflection.PortableExecutable
{
    public sealed partial class DebugDirectoryBuilder
    {
        /// <summary>
        /// Adds Embedded Portable PDB entry.
        /// </summary>
        /// <param name="debugMetadata">Portable PDB metadata builder.</param>
        /// <param name="portablePdbVersion">Version of Portable PDB format (e.g. 0x0100 for 1.0).</param>
        /// <exception cref="ArgumentNullException"><paramref name="debugMetadata"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="portablePdbVersion"/> is smaller than 0x0100.</exception>
        public void AddEmbeddedPortablePdbEntry(BlobBuilder debugMetadata, ushort portablePdbVersion)
        {
            if (debugMetadata is null)
            {
                Throw.ArgumentNull(nameof(debugMetadata));
            }

            if (portablePdbVersion < PortablePdbVersions.MinFormatVersion)
            {
                Throw.ArgumentOutOfRange(nameof(portablePdbVersion));
            }

            int dataSize = WriteEmbeddedPortablePdbData(_dataBuilder, debugMetadata);

            AddEntry(
                type: DebugDirectoryEntryType.EmbeddedPortablePdb,
                version: PortablePdbVersions.DebugDirectoryEmbeddedVersion(portablePdbVersion),
                stamp: 0,
                dataSize);
        }

        private static int WriteEmbeddedPortablePdbData(BlobBuilder builder, BlobBuilder debugMetadata)
        {
            int start = builder.Count;

            // header (signature, decompressed size):
            builder.WriteUInt32(PortablePdbVersions.DebugDirectoryEmbeddedSignature);
            builder.WriteInt32(debugMetadata.Count);

            // compressed data:
            var compressed = new MemoryStream();
            using (var deflate = new DeflateStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            {
                foreach (var blob in debugMetadata.GetBlobs())
                {
                    var segment = blob.GetBytes();
                    deflate.Write(segment.Array!, segment.Offset, segment.Count);
                }
            }

            builder.WriteBytes(compressed.GetBuffer(), 0, (int)compressed.Length);

            return builder.Count - start;
        }

    }
}
