// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Compression;

namespace System.IO.Packaging
{
    /// <summary>
    /// This class represents a Part within a Zip container.
    /// This is a part of the Packaging Layer APIs.
    /// This implementation is specific to the Zip file format.
    /// </summary>
    public sealed class ZipPackagePart : PackagePart
    {
        #region Public Methods

        /// <summary>
        /// Custom Implementation for the GetStream Method
        /// </summary>
        /// <param name="streamFileMode">Mode in which the stream should be opened</param>
        /// <param name="streamFileAccess">Access with which the stream should be opened</param>
        /// <returns>Stream Corresponding to this part</returns>
        protected override Stream? GetStreamCore(FileMode streamFileMode, FileAccess streamFileAccess)
        {
            if (_zipArchiveEntry != null)
            {
                // Reset the stream when FileMode.Create is specified. When the backing archive is in
                // Create mode, ZipArchiveEntry only ever supports opening once, so there is nothing to
                // reset. When the archive is in Update mode, the existing content must be discarded.
                if (streamFileMode == FileMode.Create && _zipArchiveEntry.Archive.Mode != ZipArchiveMode.Create)
                {
#if NET11_0_OR_GREATER
                    // Opening with discardExistingContent skips decompressing and loading the existing
                    // content into memory only to truncate it.
                    return _zipStreamManager.Open(_zipArchiveEntry, streamFileAccess, discardExistingContent: true);
#else
                    using (var tempStream = _zipStreamManager.Open(_zipArchiveEntry, streamFileAccess))
                    {
                        tempStream.SetLength(0);
                    }
#endif
                }

                var stream = _zipStreamManager.Open(_zipArchiveEntry, streamFileAccess);
                return stream;
            }
            else if (_pieces.Count > 0)
            {
                return new InterleavedZipPackagePartStream(this, _zipStreamManager, streamFileAccess);
            }
            return null;
        }

        #endregion Public Methods

        #region Internal Constructors

        /// <summary>
        /// Constructs a ZipPackagePart for an atomic (i.e. non-interleaved) part.
        /// This is called from the ZipPackage class as a result of GetPartCore,
        /// GetPartsCore or CreatePartCore methods
        /// </summary>
        internal ZipPackagePart(ZipPackage zipPackage,
            ZipArchive zipArchive,
            ZipArchiveEntry zipArchiveEntry,
            ZipStreamManager zipStreamManager,
            PackUriHelper.ValidatedPartUri partUri,
            string contentType,
            CompressionOption compressionOption)
            : base(zipPackage, partUri, contentType, compressionOption)
        {
            _zipPackage = zipPackage;
            _zipArchive = zipArchive;
            _zipStreamManager = zipStreamManager;
            _zipArchiveEntry = zipArchiveEntry;
            _pieces = [];
        }

        /// <summary>
        /// Constructs a ZipPackagePart for an interleaved part. This is called outside of streaming
        /// production when an interleaved part is encountered in the package.
        /// </summary>
        internal ZipPackagePart(ZipPackage zipPackage,
            ZipArchive zipArchive,
            ZipStreamManager zipStreamManager,
            List<ZipPackagePartPiece> pieces,
            PackUriHelper.ValidatedPartUri partUri,
            string contentType,
            CompressionOption compressionOption)
            : base(zipPackage, partUri, contentType, compressionOption)
        {
            _zipPackage = zipPackage;
            _zipArchive = zipArchive;
            _zipStreamManager = zipStreamManager;
            _pieces = pieces;
        }

        #endregion Internal Constructors

        #region Internal Properties

        /// <summary>
        /// Obtain the sorted list of piece descriptors for an interleaved part.
        /// </summary>
        internal List<ZipPackagePartPiece> PieceDescriptors => _pieces;

        /// <summary>
        /// Obtain the ZipFileInfo descriptor of an atomic part.
        /// </summary>
        internal ZipArchiveEntry? ZipArchiveEntry => _zipArchiveEntry;

        #endregion Internal Properties

        #region Private Variables

        private readonly ZipPackage _zipPackage;
        private readonly ZipArchiveEntry? _zipArchiveEntry;
        private readonly ZipArchive _zipArchive;
        private readonly ZipStreamManager _zipStreamManager;
        private readonly List<ZipPackagePartPiece> _pieces;

        #endregion Private Variables
    }
}
