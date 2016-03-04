// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        protected override Stream GetStreamCore(FileMode streamFileMode, FileAccess streamFileAccess)
        {
            if (_zipArchiveEntry != null)
            {
                if (streamFileMode == FileMode.Create)
                {
                    using (var tempStream = _zipStreamManager.Open(_zipArchiveEntry, streamFileMode, streamFileAccess))
                    {
                        tempStream.SetLength(0);
                    }
                }

                var stream = _zipStreamManager.Open(_zipArchiveEntry, streamFileMode, streamFileAccess);
                return stream;
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
        /// <param name="zipPackage"></param>
        /// <param name="zipArchive"></param>
        /// <param name="zipArchiveEntry"></param>
        /// <param name="zipStreamManager"></param>
        /// <param name="partUri"></param>
        /// <param name="compressionOption"></param>
        /// <param name="contentType"></param>
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
        }

        #endregion Internal Constructors

        #region Internal Properties

        /// <summary>
        /// Obtain the ZipFileInfo descriptor of an atomic part.
        /// </summary>
        internal ZipArchiveEntry ZipArchiveEntry
        {
            get
            {
                return _zipArchiveEntry;
            }
        }

        #endregion Internal Properties
        
        #region Private Variables

        private ZipPackage _zipPackage;
        private ZipArchiveEntry _zipArchiveEntry;
        private ZipArchive _zipArchive;
        private ZipStreamManager _zipStreamManager;

        #endregion Private Variables
    }
}
