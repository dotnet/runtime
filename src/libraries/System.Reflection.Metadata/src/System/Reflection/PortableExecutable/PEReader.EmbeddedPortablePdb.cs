// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.Internal;
using System.Reflection.Metadata;
using System.Runtime.ExceptionServices;
using System.Threading;

using System.IO.Compression;

namespace System.Reflection.PortableExecutable
{
    /// <summary>
    /// Portable Executable format reader.
    /// </summary>
    /// <remarks>
    /// The implementation is thread-safe, that is multiple threads can read data from the reader in parallel.
    /// Disposal of the reader is not thread-safe (see <see cref="Dispose"/>).
    /// </remarks>
    public sealed partial class PEReader : IDisposable
    {
        /// <summary>
        /// Reads the data pointed to by the specified Debug Directory entry and interprets them as Embedded Portable PDB blob.
        /// </summary>
        /// <returns>
        /// Provider of a metadata reader reading Portable PDB image.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="entry"/> is not a <see cref="DebugDirectoryEntryType.EmbeddedPortablePdb"/> entry.</exception>
        /// <exception cref="BadImageFormatException">Bad format of the data.</exception>
        /// <exception cref="InvalidOperationException">PE image not available.</exception>
        public MetadataReaderProvider ReadEmbeddedPortablePdbDebugDirectoryData(DebugDirectoryEntry entry)
        {
            if (entry.Type != DebugDirectoryEntryType.EmbeddedPortablePdb)
            {
                Throw.InvalidArgument(SR.Format(SR.UnexpectedDebugDirectoryType, nameof(DebugDirectoryEntryType.EmbeddedPortablePdb)), nameof(entry));
            }

            ValidateEmbeddedPortablePdbVersion(entry);

            using (var block = GetDebugDirectoryEntryDataBlock(entry))
            {
                var pdbImage = DecodeEmbeddedPortablePdbDebugDirectoryData(block);
                return MetadataReaderProvider.FromPortablePdbImage(pdbImage);
            }
        }

        // internal for testing
        internal static void ValidateEmbeddedPortablePdbVersion(DebugDirectoryEntry entry)
        {
            // Major version encodes the version of Portable PDB format itself.
            // Minor version encodes the version of Embedded Portable PDB blob.
            // Accept any version of Portable PDB >= 1.0,
            // but only accept version 1.* of the Embedded Portable PDB blob.
            // Any breaking change in the format should rev major version of the embedded blob.
            ushort formatVersion = entry.MajorVersion;
            if (formatVersion < PortablePdbVersions.MinFormatVersion)
            {
                throw new BadImageFormatException(SR.Format(SR.UnsupportedFormatVersion, PortablePdbVersions.Format(formatVersion)));
            }

            ushort embeddedBlobVersion = entry.MinorVersion;
            if (embeddedBlobVersion != PortablePdbVersions.DefaultEmbeddedVersion)
            {
                throw new BadImageFormatException(SR.Format(SR.UnsupportedFormatVersion, PortablePdbVersions.Format(embeddedBlobVersion)));
            }
        }

        // internal for testing
        internal static unsafe ImmutableArray<byte> DecodeEmbeddedPortablePdbDebugDirectoryData(AbstractMemoryBlock block)
        {
            byte[]? decompressed;

            var headerReader = block.GetReader();
            if (headerReader.ReadUInt32() != PortablePdbVersions.DebugDirectoryEmbeddedSignature)
            {
                throw new BadImageFormatException(SR.UnexpectedEmbeddedPortablePdbDataSignature);
            }

            int decompressedSize = headerReader.ReadInt32();

            try
            {
                decompressed = new byte[decompressedSize];
            }
            catch
            {
                throw new BadImageFormatException(SR.DataTooBig);
            }

            var compressed = new ReadOnlyUnmanagedMemoryStream(headerReader.CurrentPointer, headerReader.RemainingBytes);
            var deflate = new DeflateStream(compressed, CompressionMode.Decompress, leaveOpen: true);

            if (decompressedSize > 0)
            {
                int actualLength;

                try
                {
                    actualLength = deflate.TryReadAll(decompressed, 0, decompressed.Length);
                }
                catch (InvalidDataException e)
                {
                    throw new BadImageFormatException(e.Message, e.InnerException);
                }

                if (actualLength != decompressed.Length)
                {
                    throw new BadImageFormatException(SR.SizeMismatch);
                }
            }

            // Check that there is no more compressed data left,
            // in case the decompressed size specified in the header is smaller
            // than the actual decompressed size of the data.
            if (deflate.ReadByte() != -1)
            {
                throw new BadImageFormatException(SR.SizeMismatch);
            }

            return ImmutableByteArrayInterop.DangerousCreateFromUnderlyingArray(ref decompressed);
        }

        partial void TryOpenEmbeddedPortablePdb(DebugDirectoryEntry embeddedPdbEntry, ref bool openedEmbeddedPdb, ref MetadataReaderProvider? provider, ref Exception? errorToReport)
        {
            provider = null;
            MetadataReaderProvider? candidate = null;

            try
            {
                candidate = ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdbEntry);

                // throws if headers are invalid:
                candidate.GetMetadataReader();

                provider = candidate;
                openedEmbeddedPdb = true;
                return;
            }
            catch (Exception e) when (e is BadImageFormatException || e is IOException)
            {
                errorToReport = errorToReport ?? e;
                openedEmbeddedPdb = false;
            }
            finally
            {
                if (provider == null)
                {
                    candidate?.Dispose();
                }
            }
        }
    }
}
