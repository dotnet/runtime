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
            using (var deflate = new DeflateStream(new BlobBuilderStream(builder), CompressionLevel.Optimal, leaveOpen: true))
            {
                debugMetadata.WriteContentTo(deflate);
            }

            return builder.Count - start;
        }

        /// <summary>
        /// Provides a <see cref="Stream"/> interface to write to a <see cref="BlobBuilder"/>.
        /// </summary>
        /// <param name="builder">The blob builder to write data to.</param>
        private sealed class BlobBuilderStream(BlobBuilder builder) : Stream
        {
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => builder.WriteBytes(buffer, offset, count);
#if NET
            public override void Write(ReadOnlySpan<byte> buffer) => builder.WriteBytes(buffer);
#endif
        }
    }
}
