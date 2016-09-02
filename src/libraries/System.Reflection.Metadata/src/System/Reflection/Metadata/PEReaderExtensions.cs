// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;

namespace System.Reflection.Metadata
{
    // EditorBrowsable(Never) so that we don't clutter completion list with this type because a user that only has System.Reflection.Metadata 
    // imported and has type PE is likely looking to resolve PEReader from the System.Reflection.PortableExecutable and not looking to invoke
    // these extensions as regular statics.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class PEReaderExtensions
    {
        /// <summary>
        /// Returns a body block of a method with specified Relative Virtual Address (RVA);
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="peReader"/> is null.</exception>
        /// <exception cref="BadImageFormatException">The body is not found in the metadata or is invalid.</exception>
        /// <exception cref="InvalidOperationException">Section where the method is stored is not available.</exception>
        /// <exception cref="IOException">IO error while reading from the underlying stream.</exception>
        public static MethodBodyBlock GetMethodBody(this PEReader peReader, int relativeVirtualAddress)
        {
            if (peReader == null)
            {
                throw new ArgumentNullException(nameof(peReader));
            }

            var block = peReader.GetSectionData(relativeVirtualAddress);
            if (block.Length == 0)
            {
                throw new BadImageFormatException(SR.Format(SR.InvalidMethodRva, relativeVirtualAddress));
            }

            // Call to validating public BlobReader constructor is by design -- we need to throw PlatformNotSupported on big-endian architecture.
            return MethodBodyBlock.Create(block.GetReader());
        }

        /// <summary>
        /// Gets a <see cref="MetadataReader"/> from a <see cref="PEReader"/>.
        /// </summary>
        /// <remarks>
        /// The caller must keep the <see cref="PEReader"/> alive and undisposed throughout the lifetime of the metadata reader.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="peReader"/> is null</exception>
        /// <exception cref="PlatformNotSupportedException">The current platform is big-endian.</exception>
        /// <exception cref="IOException">IO error while reading from the underlying stream.</exception>
        public static MetadataReader GetMetadataReader(this PEReader peReader)
        {
            return GetMetadataReader(peReader, MetadataReaderOptions.ApplyWindowsRuntimeProjections, null);
        }

        /// <summary>
        /// Gets a <see cref="MetadataReader"/> from a <see cref="PEReader"/>.
        /// </summary>
        /// <remarks>
        /// The caller must keep the <see cref="PEReader"/> alive and undisposed throughout the lifetime of the metadata reader.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="peReader"/> is null</exception>
        /// <exception cref="PlatformNotSupportedException">The current platform is big-endian.</exception>
        /// <exception cref="IOException">IO error while reading from the underlying stream.</exception>
        public static MetadataReader GetMetadataReader(this PEReader peReader, MetadataReaderOptions options)
        {
            return GetMetadataReader(peReader, options, null);
        }

        /// <summary>
        /// Gets a <see cref="MetadataReader"/> from a <see cref="PEReader"/>.
        /// </summary>
        /// <remarks>
        /// The caller must keep the <see cref="PEReader"/> alive and undisposed throughout the lifetime of the metadata reader.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="peReader"/> is null</exception>
        /// <exception cref="ArgumentException">The encoding of <paramref name="utf8Decoder"/> is not <see cref="UTF8Encoding"/>.</exception>
        /// <exception cref="PlatformNotSupportedException">The current platform is big-endian.</exception>
        /// <exception cref="IOException">IO error while reading from the underlying stream.</exception>
        public static unsafe MetadataReader GetMetadataReader(this PEReader peReader, MetadataReaderOptions options, MetadataStringDecoder utf8Decoder)
        {
            if (peReader == null)
            {
                throw new ArgumentNullException(nameof(peReader));
            }

            var metadata = peReader.GetMetadata();
            return new MetadataReader(metadata.Pointer, metadata.Length, options, utf8Decoder);
        }
    }
}
