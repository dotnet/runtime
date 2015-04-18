// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//-----------------------------------------------------------------------------
//
// Description:
//  This is a subclass for the abstract PackagePart class.
//  This implementation is specific to Zip file format.
//
// History:
//  12/28/2004: SarjanaS: Initial creation. [BruceMac provided some of the
//                                           initial code]
//
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO.Compression;

namespace System.IO.Packaging
{
    /// <summary>
    /// This class represents a Part within a Zip container.
    /// This is a part of the Packaging Layer APIs
    /// </summary>
    public sealed class ZipPackagePart : PackagePart
    {
        //------------------------------------------------------
        //
        //  Public Constructors
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Custom Implementation for the GetStream Method
        /// </summary>
        /// <param name="mode">Mode in which the stream should be opened</param>
        /// <param name="access">Access with which the stream should be opened</param>
        /// <returns>Stream Corresponding to this part</returns>
        protected override Stream GetStreamCore(FileMode mode, FileAccess access)
        {
            if (_zipArchiveEntry != null)
            {
                return _zipArchiveEntry.Open();
            }
            // v-ericwh todo have questions about this
            return null;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Internal Constructors
        //
        //------------------------------------------------------

        #region Internal Constructors

        /// <summary>
        /// Constructs a ZipPackagePart for an atomic (i.e. non-interleaved) part.
        /// This is called from the ZipPackage class as a result of GetPartCore,
        /// GetPartsCore or CreatePartCore methods     
        /// </summary>
        /// <param name="container"></param>
        /// <param name="zipFileInfo"></param>
        /// <param name="partUri"></param>
        /// <param name="compressionOption"></param>
        /// <param name="contentType"></param>
        internal ZipPackagePart(ZipPackage container,
            ZipArchive zipArchive,
            ZipArchiveEntry zipArchiveEntry,
            PackUriHelper.ValidatedPartUri partUri,
            string contentType,
            CompressionOption compressionOption)
            : base(container, partUri, contentType, compressionOption)
        {
            _zipArchive = zipArchive;
            _zipArchiveEntry = zipArchiveEntry;
        }

        #endregion Internal Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

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

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Variables

        // Zip item info for an atomic part.
        private ZipArchiveEntry _zipArchiveEntry = null;

        private ZipArchive _zipArchive;

        #endregion Private Variables

        //------------------------------------------------------
    }
}
