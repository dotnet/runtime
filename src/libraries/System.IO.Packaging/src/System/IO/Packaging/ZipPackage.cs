// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//-----------------------------------------------------------------------------
//
// Description:
//  This is a sub class of the abstract class for Package. 
//  This implementation is specific to Zip file format.
//
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml;                   //Required for Content Type File manipulation
using System.Diagnostics;
using System.IO.Compression;

namespace System.IO.Packaging
{
    /// <summary>
    /// ZipPackage is a specific implementation for the abstract Package
    /// class, corresponding to the Zip file format. 
    /// This is a part of the Packaging Layer APIs. 
    /// </summary>
    public sealed class ZipPackage : Package
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

        #region PackagePart Methods

        /// <summary>
        /// This method is for custom implementation for the underlying file format
        /// Adds a new item to the zip archive corresponding to the PackagePart in the package.
        /// </summary>
        /// <param name="partUri">PartName</param>
        /// <param name="contentType">Content type of the part</param>
        /// <param name="compressionOption">Compression option for this part</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If partUri parameter is null</exception>
        /// <exception cref="ArgumentNullException">If contentType parameter is null</exception>
        /// <exception cref="ArgumentException">If partUri parameter does not conform to the valid partUri syntax</exception>
        /// <exception cref="ArgumentOutOfRangeException">If CompressionOption enumeration [compressionOption] does not have one of the valid values</exception>
        protected override PackagePart CreatePartCore(Uri partUri,
            string contentType,
            CompressionOption compressionOption)
        {
            //Validating the PartUri - this method will do the argument checking required for uri.
            partUri = PackUriHelper.ValidatePartUri(partUri);

            if (contentType == null)
                throw new ArgumentNullException("contentType");

            Package.ThrowIfCompressionOptionInvalid(compressionOption);

            // Convert Metro CompressionOption to Zip CompressionMethodEnum.
            CompressionLevel level;
            GetZipCompressionMethodFromOpcCompressionOption(compressionOption,
                out level);

            // If any entries are present in the ignoredItemList that might correspond to 
            // the same part name, we delete all those entries.
            _ignoredItemHelper.Delete((PackUriHelper.ValidatedPartUri)partUri);

            // Create new Zip item.
            // We need to remove the leading "/" character at the beginning of the part name.
            // The partUri object must be a ValidatedPartUri
            string zipItemName = ((PackUriHelper.ValidatedPartUri)partUri).PartUriString.Substring(1);

            ZipArchiveEntry zipArchiveEntry = _zipArchive.CreateEntry(zipItemName, level);

            //Store the content type of this part in the content types stream.
            _contentTypeHelper.AddContentType((PackUriHelper.ValidatedPartUri)partUri, new ContentType(contentType), level);

            return new ZipPackagePart(this, zipArchiveEntry.Archive, zipArchiveEntry, _zipStreamManager, (PackUriHelper.ValidatedPartUri)partUri, contentType, compressionOption);
        }

        /// <summary>
        /// This method is for custom implementation specific to the file format.
        /// Returns the part after reading the actual physical bits. The method 
        /// returns a null to indicate that the part corresponding to the specified 
        /// Uri was not found in the container.
        /// This method does not throw an exception if a part does not exist.
        /// </summary>
        /// <param name="partUri"></param>
        /// <returns></returns>
        protected override PackagePart GetPartCore(Uri partUri)
        {
            //Currently the design has two aspects which makes it possible to return
            //a null from this method -
            //  1. All the parts are loaded at Package.Open time and as such, this 
            //     method would not be invoked, unless the user is asking for -
            //     i. a part that does not exist - we can safely return null
            //     ii.a part(interleaved/non-interleaved) that was added to the 
            //        underlying package by some other means, and the user wants to 
            //        access the updated part. This is currently not possible as the
            //        underlying zip i/o layer does not allow for FileShare.ReadWrite. 
            //  2. Also, its not a straighforward task to determine if a new part was 
            //     added as we need to look for atomic as well as interleaved parts and 
            //     this has to be done in a case sensitive manner. So, effectively
            //     we will have to go through the entier list of zip items to determine
            //     if there are any updates. 
            //  If ever the design changes, then this method must be updated accordingly

            return null;
        }

        /// <summary>
        /// This method is for custom implementation specific to the file format.
        /// Deletes the part corresponding to the uri specified. Deleting a part that does not
        /// exists is not an error and so we do not throw an exception in that case.
        /// </summary>
        /// <param name="partUri"></param>
        /// <exception cref="ArgumentNullException">If partUri parameter is null</exception>
        /// <exception cref="ArgumentException">If partUri parameter does not conform to the valid partUri syntax</exception>
        protected override void DeletePartCore(Uri partUri)
        {

            //Validating the PartUri - this method will do the argument checking required for uri.
            partUri = PackUriHelper.ValidatePartUri(partUri);

            string partZipName = GetZipItemNameFromOpcName(PackUriHelper.GetStringForPartUri(partUri));
            ZipArchiveEntry zipArchiveEntry = _zipArchive.GetEntry(partZipName);
            if (zipArchiveEntry != null)
            {
                // Case of an atomic part.
                zipArchiveEntry.Delete();
            }

            //We are not absolutely required to clean up all the items in the ignoredItems list,
            //but it will help to clean up incomplete and leftover pieces that belonged to the same
            //part
            _ignoredItemHelper.Delete((PackUriHelper.ValidatedPartUri)partUri);

            //Delete the content type for this part if it was specified as an override
            _contentTypeHelper.DeleteContentType((PackUriHelper.ValidatedPartUri)partUri);

        }

        /// <summary>
        /// This method is for custom implementation specific to the file format.
        /// This is the method that knows how to get the actual parts from the underlying
        /// zip archive.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Some or all of the parts may be interleaved. The Part object for an interleaved part encapsulates
        /// the Uri of the proper part name and the ZipFileInfo of the initial piece.
        /// This function does not go through the extra work of checking piece naming validity
        /// throughout the package. 
        /// </para>
        /// <para>
        /// This means that interleaved parts without an initial piece will be silently ignored.
        /// Other naming anomalies get caught at the Stream level when an I/O operation involves
        /// an anomalous or missing piece.
        /// </para>
        /// <para>
        /// This function reads directly from the underlying IO layer and is supposed to be called
        /// just once in the lifetime of a package (at init time).
        /// </para>
        /// </remarks>
        /// <returns>An array of ZipPackagePart.</returns>
        protected override PackagePart[] GetPartsCore()
        {
            List<PackagePart> parts = new List<PackagePart>(_initialPartListSize);

            // The list of files has to be searched linearly (1) to identify the content type
            // stream, and (2) to identify parts.
            System.Collections.ObjectModel.ReadOnlyCollection<ZipArchiveEntry> zipArchiveEntries = _zipArchive.Entries;

            // We have already identified the [ContentTypes].xml pieces if any are present during
            // the initialization of ZipPackage object

            // Record parts and ignored items.
            foreach (ZipArchiveEntry zipArchiveEntry in zipArchiveEntries)
            {
                //If the info is for a folder or a volume then we simply ignore it. 
                if (IsValidFileItem(zipArchiveEntry))
                {
                    //Returns false if - 
                    // a. its a content type item
                    // b. items that have either a leading or trailing slash.
                    if (IsZipItemValidOpcPartOrPiece(zipArchiveEntry.Name))
                    {
                        Uri partUri = new Uri(GetOpcNameFromZipItemName(zipArchiveEntry.FullName), UriKind.Relative);
                        PackUriHelper.ValidatedPartUri validatedPartUri;
                        if (PackUriHelper.TryValidatePartUri(partUri, out validatedPartUri))
                        {
                            ContentType contentType = _contentTypeHelper.GetContentType(validatedPartUri);
                            if (contentType != null)
                            {
                                // In case there was some redundancy between pieces and/or the atomic
                                // part, it will be detected at this point because the part's Uri (which
                                // is independent of interleaving) will already be in the dictionary.
                                parts.Add(new ZipPackagePart(this, zipArchiveEntry.Archive, zipArchiveEntry,
                                    _zipStreamManager, validatedPartUri, contentType.ToString(), GetCompressionOptionFromZipFileInfo(zipArchiveEntry)));
                            }
                            else
                                //Since this part does not have a valid content type we add it to the ignored list,
                                //as later if a another part with similar extension gets added, this part might become
                                //valid next time we open the package. 
                                _ignoredItemHelper.AddItemForAtomicPart(validatedPartUri, zipArchiveEntry.Name);
                        }
                        //If not valid part uri we can completely ignore this zip file item. Even if later someone adds
                        //a new part, the corresponding zip item can never map to one of these items
                    }
                    // If IsZipItemValidOpcPartOrPiece returns false, it implies that either the zip file Item
                    // starts or ends with a "/" and as such we can completely ignore this zip file item. Even if later
                    // a new part gets added, its corresponding zip item cannot map to one of these items.
                }
                else
                {
                    //If the zip item name that is a volume or a folder, is a valid name for a part, then
                    //we add it to the ignored items.
                    Uri partUri = new Uri(GetOpcNameFromZipItemName(zipArchiveEntry.Name), UriKind.Relative);
                    PackUriHelper.ValidatedPartUri validatedPartUri;
                    if (PackUriHelper.TryValidatePartUri(partUri, out validatedPartUri))
                        _ignoredItemHelper.AddItemForAtomicPart(validatedPartUri, zipArchiveEntry.Name);
                }
            }

            return parts.ToArray();
        }

        #endregion PackagePart Methods 

        #region Other Methods

        /// <summary>
        /// This method is for custom implementation corresponding to the underlying zip file format.
        /// </summary>
        protected override void FlushCore()
        {
            //Save the content type file to the archive.
            _contentTypeHelper.SaveToFile();
        }

        /// <summary>
        /// Closes the underlying ZipArchive object for this container
        /// </summary>
        /// <param name="disposing">True if called during Dispose, false if called during Finalize</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (_contentTypeHelper != null)
                    {
                        _contentTypeHelper.SaveToFile();
                    }

                    if (_zipStreamManager != null)
                    {
                        _zipStreamManager.Dispose();
                    }

                    if (_zipArchive != null)
                    {
                        _zipArchive.Dispose();
                    }

                    // _containerStream may be opened given a file name, in which case it should be closed here.
                    // _containerStream may be passed into the constructor, in which case, it should not be closed here.
                    if (_shouldCloseContainerStream)
                    {
                        _containerStream.Dispose();
                    }
                    else
                    {
                    }
                    _containerStream = null;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        #endregion Other Methods

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
        /// Internal constructor that is called by the OpenOnFile static method.
        /// </summary>
        /// <param name="path">File path to the container.</param>
        /// <param name="packageFileMode">Container is opened in the specified mode if possible</param>
        /// <param name="packageFileAccess">Container is opened with the speficied access if possible</param>
        /// <param name="share">Container is opened with the specified share if possible</param>

        internal ZipPackage(string path, FileMode packageFileMode, FileAccess packageFileAccess, FileShare share)
            : base(packageFileAccess)
        {
            ZipArchive zipArchive = null;
            IgnoredItemHelper ignoredItemHelper = null;
            ContentTypeHelper contentTypeHelper = null;
            _packageFileMode = packageFileMode;
            _packageFileAccess = packageFileAccess;

            try
            {
                _containerStream = new FileStream(path, _packageFileMode, _packageFileAccess, share);
                _shouldCloseContainerStream = true;
                ZipArchiveMode zipArchiveMode = ZipArchiveMode.Update;
                if (packageFileAccess == FileAccess.Read)
                    zipArchiveMode = ZipArchiveMode.Read;
                else if (packageFileAccess == FileAccess.Write)
                    zipArchiveMode = ZipArchiveMode.Create;
                else if (packageFileAccess == FileAccess.ReadWrite)
                    zipArchiveMode = ZipArchiveMode.Update;

                zipArchive = new ZipArchive(_containerStream, zipArchiveMode, true, Text.Encoding.UTF8);
                _zipStreamManager = new ZipStreamManager(zipArchive, _packageFileMode, _packageFileAccess);
                ignoredItemHelper = new IgnoredItemHelper(zipArchive);
                contentTypeHelper = new ContentTypeHelper(zipArchive, ignoredItemHelper, _packageFileMode, _packageFileAccess, _zipStreamManager);
            }
            catch
            {
                if (zipArchive != null)
                {
                    zipArchive.Dispose();
                }

                throw;
            }

            _zipArchive = zipArchive;
            _ignoredItemHelper = ignoredItemHelper;
            _contentTypeHelper = contentTypeHelper;
        }

        /// <summary>
        /// Internal constructor that is called by the Open(Stream) static methods.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="packageFileMode"></param>
        /// <param name="packageFileAccess"></param>
        internal ZipPackage(Stream s, FileMode packageFileMode, FileAccess packageFileAccess)
            : base(packageFileAccess)
        {
            ZipArchive zipArchive = null;
            IgnoredItemHelper ignoredItemHelper = null;
            ContentTypeHelper contentTypeHelper = null;
            _packageFileMode = packageFileMode;
            _packageFileAccess = packageFileAccess;

            try
            {
                ZipArchiveMode zipArchiveMode = ZipArchiveMode.Update;
                if (packageFileAccess == FileAccess.Read)
                    zipArchiveMode = ZipArchiveMode.Read;
                else if (packageFileAccess == FileAccess.Write)
                    zipArchiveMode = ZipArchiveMode.Create;
                else if (packageFileAccess == FileAccess.ReadWrite)
                    zipArchiveMode = ZipArchiveMode.Update;

                zipArchive = new ZipArchive(s, zipArchiveMode, true, Text.Encoding.UTF8);
                
                _zipStreamManager = new ZipStreamManager(zipArchive, packageFileMode, packageFileAccess);
                ignoredItemHelper = new IgnoredItemHelper(zipArchive);
                contentTypeHelper = new ContentTypeHelper(zipArchive, ignoredItemHelper, packageFileMode, packageFileAccess, _zipStreamManager);
            }
            catch
            {
                if (zipArchive != null)
                {
                    zipArchive.Dispose();
                }

                throw;
            }

            _containerStream = s;
            _shouldCloseContainerStream = false;
            _zipArchive = zipArchive;
            _ignoredItemHelper = ignoredItemHelper;
            _contentTypeHelper = contentTypeHelper;
        }

        #endregion Internal Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // More generic function than GetZipItemNameFromPartName. In particular, it will handle piece names.
        internal static string GetZipItemNameFromOpcName(string opcName)
        {
            Debug.Assert(opcName != null && opcName.Length > 0);
            return opcName.Substring(1);
        }

        // More generic function than GetPartNameFromZipItemName. In particular, it will handle piece names.
        internal static string GetOpcNameFromZipItemName(string zipItemName)
        {
            return String.Concat(s_forwardSlash, zipItemName);
        }

        // Convert from Metro CompressionOption to ZipFileInfo compression properties.
        internal static void GetZipCompressionMethodFromOpcCompressionOption(
            CompressionOption compressionOption,
            out CompressionLevel compressionLevel)
        {
            switch (compressionOption)
            {
                case CompressionOption.NotCompressed:
                    {
                        compressionLevel = CompressionLevel.NoCompression;
                    } break;
                case CompressionOption.Normal:
                    {
                        compressionLevel = CompressionLevel.Optimal;
                    } break;
                case CompressionOption.Maximum:
                    {
                        compressionLevel = CompressionLevel.Optimal;
                    } break;
                case CompressionOption.Fast:
                    {
                        compressionLevel = CompressionLevel.Fastest;
                    } break;
                case CompressionOption.SuperFast:
                    {
                        compressionLevel = CompressionLevel.Fastest;
                    } break;

                // fall-through is not allowed
                default:
                    {
                        Debug.Assert(false, "Encountered an invalid CompressionOption enum value");
                        goto case CompressionOption.NotCompressed;
                    }
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        
        internal FileMode PackageFileMode
        {
            get
            {
                return _packageFileMode;
            }
        }

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

        #region Private Methods

        //this method returns a false if the info is for a folder entry or a volume entry
        //it returns a true if it is for a file entry.
        private static bool IsValidFileItem(ZipArchiveEntry zipArchiveEntry)
        {
            // return !info.FolderFlag && !info.VolumeLabelFlag;
            // todo ew have questions about this
            return true;
        }

        //returns a boolean indicating if the underlying zip item is a valid metro part or piece
        // This mainly excludes the content type item, as well as entries with leading or trailing
        // slashes.
        private bool IsZipItemValidOpcPartOrPiece(string zipItemName)
        {
            Debug.Assert(zipItemName != null, "The parameter zipItemName should not be null");

            //check if the zip item is the Content type item -case sensitive comparison
            // The following test will filter out an atomic content type file, with name
            // "[Content_Types].xml", as well as an interleaved one, with piece names such as
            // "[Content_Types].xml/[0].piece" or "[Content_Types].xml/[5].last.piece".
            if (zipItemName.StartsWith(ContentTypeHelper.ContentTypeFileName, StringComparison.OrdinalIgnoreCase))
                return false;
            else
            {
                //Could be an empty zip folder
                //We decided to ignore zip items that contain a "/" as this could be a folder in a zip archive
                //Some of the tools support this and some dont. There is no way ensure that the zip item never have 
                //a leading "/", although this is a requirement we impose on items created through our API
                //Therefore we ignore them at the packaging api level.
                if (zipItemName.StartsWith(s_forwardSlash, StringComparison.Ordinal))
                    return false;
                //This will ignore the folder entries found in the zip package created by some zip tool
                //PartNames ending with a "/" slash is also invalid so we are skipping these entries,
                //this will also prevent the PackUriHelper.CreatePartUri from throwing when it encounters a
                // partname ending with a "/"
                if (zipItemName.EndsWith(s_forwardSlash, StringComparison.Ordinal))
                    return false;
                else
                    return true;
            }
        }

        // convert from Zip CompressionMethodEnum and DeflateOptionEnum to Metro CompressionOption 
        static private CompressionOption GetCompressionOptionFromZipFileInfo(ZipArchiveEntry zipFileInfo)
        {
            CompressionOption result = CompressionOption.Normal;

            // todo can't determine compression method / level from the ZipArchiveEntry.
#if false
            //Currently we do not Evaluate - CompressionMethodEnum.Deflated64
            //If CompressionMethodEnum.Stored is the value, CompressionOption will be NotCompressed
            //The following switch statement takes care of CompressionMethodEnum.Deflated
            if (zipFileInfo.CompressionMethod == CompressionMethodEnum.Deflated)
                switch (zipFileInfo.DeflateOption)
                {
                    case DeflateOptionEnum.Normal:
                        result = CompressionOption.Normal;
                        break;
                    case DeflateOptionEnum.Fast:
                        result = CompressionOption.Fast;
                        break;
                    case DeflateOptionEnum.Maximum:
                        result = CompressionOption.Maximum;
                        break;
                    case DeflateOptionEnum.SuperFast:
                        result = CompressionOption.SuperFast;
                        break;
                    case DeflateOptionEnum.None:
                        break;
                    default:
                        Debug.Assert(false, "Encountered and invalid value for DeflateOptionEnum");
                        break;
                }
#endif

            return result;
        }

#if false
        // ew todo don't need this.

        private static void DeleteInterleavedPartOrStream(List<PieceInfo> sortedPieceInfoList)
        {
            Debug.Assert(sortedPieceInfoList != null);
            if (sortedPieceInfoList.Count > 0)
            {
                ZipArchive zipArchive = sortedPieceInfoList[0].ZipFileInfo.ZipArchive;
                foreach (PieceInfo pieceInfo in sortedPieceInfoList)
                {
                    zipArchive.DeleteFile(pieceInfo.ZipFileInfo.Name);
                }
            }
            //Its okay for us to not clean up the sortedPieceInfoList datastructure, as the
            //owning part is about to be deleted.
        }

        /// <summary>
        /// An auxiliary function of GetPartsCore, this function sorts out the piece name
        /// descriptors accumulated in pieceNumber into valid piece sequences and garbage
        /// (i.e. ignorable Zip items).
        /// </summary>
        /// <remarks>
        /// <para>
        /// The procedure used relies on 'pieces' members to be sorted lexicographically
        /// on &lt;name, number, isLast> triples, with name comparisons being case insensitive.
        /// This is enforced by PieceInfo's IComparable implementation.
        /// </para>
        /// </remarks>
        private void ProcessPieces(SortedDictionary<PieceInfo, Object> pieceDictionary, List<PackagePart> parts)
        {
            // The zip items related to the ContentTypes.xml should have been already processed.
            // Only those zip items that follow the valid piece naming syntax and have a valid
            // part name should show up in this list. 
            // piece.PartUri should be non-null

            // Exit if nothing to do.
            if (pieceDictionary.Count == 0)
                return;

            string normalizedPrefixNameForCurrentSequence = null;
            int startIndexOfCurrentSequence = 0; // Value is ignored as long as
                                                 // prefixNameForCurrentSequence is null.

            List<PieceInfo> pieces = new List<PieceInfo>(pieceDictionary.Keys);

            for (int i = 0; i < pieces.Count; ++i)
            {
                // Looking for the start of a sequence.
                if (normalizedPrefixNameForCurrentSequence == null)
                {
                    if (pieces[i].PieceNumber != 0)
                    {
                        // Whether or not this piece bears the same unsuffixed name as a complete
                        // sequence just processed, it has to be ignored without reporting an error.
                        _ignoredItemHelper.AddItemForStrayPiece(pieces[i]);
                        continue;
                    }
                    else
                    {
                        // Found the start of a sequence. 
                        startIndexOfCurrentSequence = i;
                        normalizedPrefixNameForCurrentSequence = pieces[i].NormalizedPrefixName;
                    }
                }

                // Not a start piece. Carry out validity checks.
                else
                {
                    //Check for incomplete sequence.
                    if (String.CompareOrdinal(pieces[i].NormalizedPrefixName, normalizedPrefixNameForCurrentSequence) != 0)
                    {
                        // Check if the piece we have found is another first piece.
                        if (pieces[i].PieceNumber == 0)
                        {
                            //This can happen when we have an incomplete sequence and we encounter the first piece of the
                            //next sequence
                            _ignoredItemHelper.AddItemsForInvalidSequence(normalizedPrefixNameForCurrentSequence, pieces, startIndexOfCurrentSequence, checked(i - startIndexOfCurrentSequence));

                            //Reset these values as we found another first piece
                            startIndexOfCurrentSequence = i;
                            normalizedPrefixNameForCurrentSequence = pieces[i].NormalizedPrefixName;
                        }
                        else
                        {
                            //This can happen when we have an incomplete sequence and the next piece is also
                            //a stray piece. So we can safely ignore all the pieces till this point
                            _ignoredItemHelper.AddItemsForInvalidSequence(normalizedPrefixNameForCurrentSequence, pieces, startIndexOfCurrentSequence, checked(i - startIndexOfCurrentSequence + 1));
                            normalizedPrefixNameForCurrentSequence = null;
                            continue;
                        }
                    }
                    else
                    {
                        //if the names are the same we check if the numbers are increasing
                        if (pieces[i].PieceNumber != i - startIndexOfCurrentSequence)
                        {
                            _ignoredItemHelper.AddItemsForInvalidSequence(normalizedPrefixNameForCurrentSequence, pieces, startIndexOfCurrentSequence, checked(i - startIndexOfCurrentSequence + 1));
                            normalizedPrefixNameForCurrentSequence = null;
                            continue;
                        }
                    }
                }

                // Looking for the end of a sequence (i.e. a .last suffix).
                if (pieces[i].IsLastPiece)
                {
                    // Record sequence just seen.
                    RecordValidSequence(
                        normalizedPrefixNameForCurrentSequence,
                        pieces,
                        startIndexOfCurrentSequence,
                        i - startIndexOfCurrentSequence + 1,
                        parts);

                    // Resume searching for a new sequence.
                    normalizedPrefixNameForCurrentSequence = null;
                }
            }

            // clean up any pieces that might be at the end that do not make a complete sequence
            // This can happen when we find a valid piece zero and/or a few other pieces but not
            // the complete sequence, right at the end of the pieces list and we will finish the 
            // for loop
            if (normalizedPrefixNameForCurrentSequence != null)
            {
                _ignoredItemHelper.AddItemsForInvalidSequence(normalizedPrefixNameForCurrentSequence, pieces, startIndexOfCurrentSequence, checked(pieces.Count - startIndexOfCurrentSequence));
            }
        }

        /// <summary>
        /// The sequence of numItems starting at startIndex can be assumed valid
        /// from the point of view of piece-naming suffixes.
        /// This method makes sure a valid Uri and content type can be inferred
        /// from the name of the first piece. If so, a ZipPackagePart is created
        /// and added to the list 'parts'. If not, the piece names are recorded
        /// as ignorable items.
        /// </summary>
        /// <remarks>
        /// When the sequence and Uri are valid but there is no content type, the
        /// part name is recorded in a specific list of null-content type parts.
        /// </remarks>
        private void RecordValidSequence(
            string normalizedPrefixNameForCurrentSequence,
            List<PieceInfo> pieces,
            int startIndex,
            int numItems,
            List<PackagePart> parts)
        {
            // The Uri and content type are inferred from the unsuffixed name of the
            // first piece.
            PackUriHelper.ValidatedPartUri partUri = pieces[startIndex].PartUri;
            ContentType contentType = _contentTypeHelper.GetContentType(partUri);
            if (contentType == null)
            {
                _ignoredItemHelper.AddItemsForInvalidSequence(normalizedPrefixNameForCurrentSequence, pieces, startIndex, numItems);
                return;
            }

            // Add a new part, initializing with an array of PieceInfo.
            parts.Add(new ZipPackagePart(this, _zipArchive, pieces.GetRange(startIndex, numItems), partUri, contentType.ToString(),
                GetCompressionOptionFromZipFileInfo(pieces[startIndex].ZipFileInfo)));
        }
#endif

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Members

        private const int _initialPartListSize = 50;
        private const int _initialPieceNameListSize = 50;

        private ZipArchive _zipArchive;
        private Stream _containerStream;      // stream we are opened in if Open(Stream) was called
        private bool _shouldCloseContainerStream;
        private ContentTypeHelper _contentTypeHelper;    // manages the content types for all the parts in the container
        private IgnoredItemHelper _ignoredItemHelper;    // manages the ignored items in a zip package
        private ZipStreamManager _zipStreamManager;      // manages streams for all parts, avoiding opening streams multiple times
        private FileAccess _packageFileAccess;
        private FileMode _packageFileMode;

        private static readonly string s_forwardSlash = "/"; //Required for creating a part name from a zip item name

        //IEqualityComparer for extensions
        private static readonly ExtensionEqualityComparer s_extensionEqualityComparer = new ExtensionEqualityComparer();

        #endregion Private Members

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Class

        /// <summary>
        /// ExtensionComparer
        /// The Extensions are stored in the Default Dicitonary in their original form, 
        /// however they are compared in a normalized manner.
        /// Equivalence for extensions in the content type stream, should follow
        /// the same rules as extensions of partnames. Also, by the time this code is invoked, 
        /// we have already validated, that the extension is in the correct format as per the
        /// part name rules.So we are simplifying the logic here to just convert the extensions
        /// to Upper invariant form and then compare them.
        /// </summary>
        private sealed class ExtensionEqualityComparer : IEqualityComparer<string>
        {
            bool IEqualityComparer<string>.Equals(string extensionA, string extensionB)
            {
                Invariant.Assert(extensionA != null, "extenstion should not be null");
                Invariant.Assert(extensionB != null, "extenstion should not be null");

                //Important Note: any change to this should be made in accordance 
                //with the rules for comparing/normalizing partnames. 
                //Refer to PackUriHelper.ValidatedPartUri.GetNormalizedPartUri method.
                //Currently normalization just involves upper-casing ASCII and hence the simplification.
                return (String.CompareOrdinal(extensionA.ToUpperInvariant(), extensionB.ToUpperInvariant()) == 0);
            }

            int IEqualityComparer<string>.GetHashCode(string extension)
            {
                Invariant.Assert(extension != null, "extenstion should not be null");

                //Important Note: any change to this should be made in accordance 
                //with the rules for comparing/normalizing partnames.
                //Refer to PackUriHelper.ValidatedPartUri.GetNormalizedPartUri method.
                //Currently normalization just involves upper-casing ASCII and hence the simplification.
                return extension.ToUpperInvariant().GetHashCode();
            }
        }

        #region ContentTypeHelper Class

        /// <summary>
        /// This is a helper class that maintains the Content Types File related to 
        /// this ZipPackage.        
        /// </summary>
        private class ContentTypeHelper
        {
            #region Constructor

            /// <summary>
            /// Initialize the object without uploading any information from the package.
            /// Complete initialization in read mode also involves calling ParseContentTypesFile
            /// to deserialize content type information.
            /// </summary>
            internal ContentTypeHelper(ZipArchive zipArchive, IgnoredItemHelper ignoredItemHelper, FileMode packageFileMode, FileAccess packageFileAccess, ZipStreamManager zipStreamManager)
            {
                _zipArchive = zipArchive;               //initialized in the ZipPackage constructor
                _packageFileMode = packageFileMode;
                _packageFileAccess = packageFileAccess;
                _zipStreamManager = zipStreamManager;   //initialized in the ZipPackage constructor
                // The extensions are stored in the default Dictionary in their original form , but they are compared
                // in a normalized manner using the ExtensionComparer.
                _defaultDictionary = new Dictionary<string, ContentType>(s_defaultDictionaryInitialSize, s_extensionEqualityComparer);

                //IgnoredItemHelper
                _ignoredItemHelper = ignoredItemHelper; //initialized in the ZipPackage constructor

                // Identify the content type file or files before identifying parts and piece sequences.
                // This is necessary because the name of the content type stream is not a part name and
                // the information it contains is needed to recognize valid parts.
                if (_zipArchive.Mode == ZipArchiveMode.Read || _zipArchive.Mode == ZipArchiveMode.Update)
                    ParseContentTypesFile(_zipArchive.Entries);

                //No contents to persist to the disk - 
                _dirty = false; //by default 

                //Lazy initialize these members as required                
                //_overrideDictionary      - Overrides should be rare
                //_contentTypeFileInfo     - We will either find an atomin part, or
                //_contentTypeStreamPieces - an interleaved part
                //_contentTypeStreamExists - defaults to false - not yet found
            }

            #endregion Constructor

            #region Internal Properties

            internal static string ContentTypeFileName
            {
                get
                {
                    return s_contentTypesFile;
                }
            }

            #endregion Internal Properties

            #region Internal Methods

            //Adds the Default entry if it is the first time we come across
            //the extension for the partUri, does nothing if the content type
            //corresponding to the default entry for the extension matches or
            //adds a override corresponding to this part and content type.
            //This call is made when a new part is being added to the package.

            // This method assumes the partUri is valid.
            internal void AddContentType(PackUriHelper.ValidatedPartUri partUri, ContentType contentType,
                CompressionLevel compressionLevel)
            {
                //save the compressionOption and deflateOption that should be used
                //to create the content type item later
                if (!_contentTypeStreamExists)
                {
                    _cachedCompressionLevel = compressionLevel;
                }

                // Figure out whether the mapping matches a default entry, can be made into a new
                // default entry, or has to be entered as an override entry.
                bool foundMatchingDefault = false;
                string extension = partUri.PartUriExtension;

                // Need to create an override entry?
                if (extension.Length == 0
                    || (_defaultDictionary.ContainsKey(extension)
                        && !(foundMatchingDefault =
                               _defaultDictionary[extension].AreTypeAndSubTypeEqual(contentType))))
                {
                    AddOverrideElement(partUri, contentType);
                }

                // Else, either there is already a mapping from extension to contentType,
                // or one needs to be created.
                else if (!foundMatchingDefault)
                {
                    AddDefaultElement(extension, contentType);
                    //Delete all items that might map to the same extension as these currently ignored
                    //items might show up as valid parts later.
                    _ignoredItemHelper.DeleteItemsWithSimilarExtension(extension);
                }
            }


            //Returns the content type for the part, if present, else returns null.
            internal ContentType GetContentType(PackUriHelper.ValidatedPartUri partUri)
            {
                //Step 1: Check if there is an override entry present corresponding to the 
                //partUri provided. Override takes precedence over the default entries
                if (_overrideDictionary != null)
                {
                    if (_overrideDictionary.ContainsKey(partUri))
                        return _overrideDictionary[partUri];
                }

                //Step 2: Check if there is a default entry corresponding to the 
                //extension of the partUri provided.
                string extension = partUri.PartUriExtension;

                if (_defaultDictionary.ContainsKey(extension))
                    return _defaultDictionary[extension];

                //Step 3: If we did not find an entry in the override and the default
                //dictionaries, this is an error condition
                return null;
            }

            //Deletes the override entry corresponding to the partUri, if it exists
            internal void DeleteContentType(PackUriHelper.ValidatedPartUri partUri)
            {
                if (_overrideDictionary != null)
                {
                    if (_overrideDictionary.Remove(partUri))
                        _dirty = true;
                }
            }

            internal void SaveToFile()
            {
                if (_dirty)
                {
                    //Lazy init: Initialize when the first part is added.
                    if (!_contentTypeStreamExists)
                    {
                        _contentTypeZipArchiveEntry = _zipArchive.CreateEntry(s_contentTypesFile, _cachedCompressionLevel);
                        _contentTypeStreamExists = true;
                    }

                    // delete and re-create entry for content part.  When writing this, the stream will not truncate the content
                    // if the XML is shorter than the existing content part.
                    var contentTypefullName = _contentTypeZipArchiveEntry.FullName;
                    var thisArchive = _contentTypeZipArchiveEntry.Archive;
                    _zipStreamManager.Close(_contentTypeZipArchiveEntry);
                    _contentTypeZipArchiveEntry.Delete();
                    _contentTypeZipArchiveEntry = thisArchive.CreateEntry(contentTypefullName);


                    using (Stream s = _zipStreamManager.Open(_contentTypeZipArchiveEntry, _packageFileMode, FileAccess.ReadWrite))
                    {
                        // use UTF-8 encoding by default
                        using (XmlWriter writer = XmlWriter.Create(s, new XmlWriterSettings { Encoding = System.Text.Encoding.UTF8 }))
                        {
                            writer.WriteStartDocument();

                            // write root element tag - Types
                            writer.WriteStartElement(s_typesTagName, s_typesNamespaceUri);

                            // for each default entry
                            foreach (string key in _defaultDictionary.Keys)
                            {
                                WriteDefaultElement(writer, key, _defaultDictionary[key]);
                            }

                            if (_overrideDictionary != null)
                            {
                                // for each override entry
                                foreach (PackUriHelper.ValidatedPartUri key in _overrideDictionary.Keys)
                                {
                                    WriteOverrideElement(writer, key, _overrideDictionary[key]);
                                }
                            }

                            // end of Types tag
                            writer.WriteEndElement();

                            // close the document
                            writer.WriteEndDocument();

                            _dirty = false;
                        }
                    }
                }
            }

            #endregion Internal Methods

            #region Private Methods

            private void EnsureOverrideDictionary()
            {
                // The part Uris are stored in the Override Dictionary in their original form , but they are compared
                // in a normalized manner using the PartUriComparer
                if (_overrideDictionary == null)
                    _overrideDictionary = new Dictionary<PackUriHelper.ValidatedPartUri, ContentType>(s_overrideDictionaryInitialSize);
            }

            private void ParseContentTypesFile(System.Collections.ObjectModel.ReadOnlyCollection<ZipArchiveEntry> zipFiles)
            {
                // Find the content type stream, allowing for interleaving. Naming collisions
                // (as between an atomic and an interleaved part) will result in an exception being thrown.
                Stream s = OpenContentTypeStream(zipFiles);

                // Allow non-existent content type stream.
                if (s == null)
                    return;

                XmlReaderSettings xrs = new XmlReaderSettings();
                xrs.IgnoreWhitespace = true;

                using (s)
                using (XmlReader reader = XmlReader.Create(s, xrs))
                {
                    //Prohibit DTD from the markup as per the OPC spec
#pragma warning disable 618
                    // reader.ProhibitDtd = true; todo ew
#pragma warning restore 618

                    //This method expects the reader to be in ReadState.Initial.
                    //It will make the first read call.
                    PackagingUtilities.PerformInitailReadAndVerifyEncoding(reader);

                    //Note: After the previous method call the reader should be at the first tag in the markup.
                    //MoveToContent - Skips over the following - ProcessingInstruction, DocumentType, Comment, Whitespace, or SignificantWhitespace
                    //If the reader is currently at a content node then this function call is a no-op
                    reader.MoveToContent();

                    // look for our root tag and namespace pair - ignore others in case of version changes
                    // Make sure that the current node read is an Element
                    if ((reader.NodeType == XmlNodeType.Element)
                        && (reader.Depth == 0)
                        && (String.CompareOrdinal(reader.NamespaceURI, s_typesNamespaceUri) == 0)
                        && (String.CompareOrdinal(reader.Name, s_typesTagName) == 0))
                    {
                        //There should be a namespace Attribute present at this level.
                        //Also any other attribute on the <Types> tag is an error including xml: and xsi: attributes
                        if (PackagingUtilities.GetNonXmlnsAttributeCount(reader) > 0)
                        {
                            throw new XmlException(SR.Get(SRID.TypesTagHasExtraAttributes), null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
                        }

                        // start tag encountered
                        // now parse individual Default and Override tags
                        while (reader.Read())
                        {
                            //Skips over the following - ProcessingInstruction, DocumentType, Comment, Whitespace, or SignificantWhitespace
                            //If the reader is currently at a content node then this function call is a no-op
                            reader.MoveToContent();

                            //If MoveToContent() takes us to the end of the content
                            if (reader.NodeType == XmlNodeType.None)
                                continue;

                            // Make sure that the current node read is an element 
                            // Currently we expect the Default and Override Tag at Depth 1
                            if (reader.NodeType == XmlNodeType.Element
                                && reader.Depth == 1
                                && (String.CompareOrdinal(reader.NamespaceURI, s_typesNamespaceUri) == 0)
                                && (String.CompareOrdinal(reader.Name, s_defaultTagName) == 0))
                            {
                                ProcessDefaultTagAttributes(reader);
                            }
                            else
                                if (reader.NodeType == XmlNodeType.Element
                                    && reader.Depth == 1
                                    && (String.CompareOrdinal(reader.NamespaceURI, s_typesNamespaceUri) == 0)
                                    && (String.CompareOrdinal(reader.Name, s_overrideTagName) == 0))
                                {
                                    ProcessOverrideTagAttributes(reader);
                                }
                                else
                                    if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == 0 && String.CompareOrdinal(reader.Name, s_typesTagName) == 0)
                                        continue;
                                    else
                                    {
                                        throw new XmlException(SR.Get(SRID.TypesXmlDoesNotMatchSchema), null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
                                    }
                        }
                    }
                    else
                    {
                        throw new XmlException(SR.Get(SRID.TypesElementExpected), null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
                    }
                }
            }

            /// <summary>
            /// Find the content type stream, allowing for interleaving. Naming collisions
            /// (as between an atomic and an interleaved part) will result in an exception being thrown.
            /// Return null if no content type stream has been found.
            /// </summary>
            /// <remarks>
            /// The input array is lexicographically sorted
            /// </remarks>
            private Stream OpenContentTypeStream(System.Collections.ObjectModel.ReadOnlyCollection<ZipArchiveEntry> zipFiles)
            {
#if false
                // ew todo don't need this
                // Collect all pieces found prior to sorting and validating the sequence.
                SortedDictionary<PieceInfo, ZipFileInfo> contentTypeStreamPieces = null;
#endif

                foreach (ZipArchiveEntry zipFileInfo in zipFiles) // todo
                {
                    if (zipFileInfo.Name.ToUpperInvariant().StartsWith(s_contentTypesFileUpperInvariant, StringComparison.Ordinal))
                    {
                        // Atomic name.
                        if (zipFileInfo.Name.Length == ContentTypeFileName.Length)
                        {
                            // Record the file info.
                            _contentTypeZipArchiveEntry = zipFileInfo;
                        }
#if false
                        // ew todo don't need this
                        // Piece name.
                        else if (PieceNameHelper.TryCreatePieceInfo(zipFileInfo, out pieceInfo))
                        {
                            // Lazy init.
                            if (contentTypeStreamPieces == null)
                                contentTypeStreamPieces = new SortedDictionary<PieceInfo, ZipFileInfo>(PieceNameHelper.PieceNameComparer);

                            // Record the piece info.
                            contentTypeStreamPieces.Add(pieceInfo, zipFileInfo);
                        }
#endif
                    }
                }

#if false
                // ew todo don't need
                List<PieceInfo> pieces = null;

                // If pieces were found. Find out if there is a piece 0, in which case
                // sequence validity will be required.
                // Since the general case requires a sorted array, use a sorted array for this.
                if (contentTypeStreamPieces != null)
                {
                    pieces = new List<PieceInfo>(contentTypeStreamPieces.Keys);

                    // The piece name helper does not recognize negative piece numbers.
                    // So if piece 0 occurs at all, it occurs first.
                    if (pieces[0].PieceNumber != 0)
                    {
                        // The pieces we have found form an incomplete sequence as the first 
                        // piece is missing. So we add these to the list of ignored items
                        _ignoredItemHelper.AddItemsForInvalidSequence(s_contentTypesFileUpperInvariant, pieces, 0, pieces.Count);

                        contentTypeStreamPieces = null;
                        pieces = null;
                    }
                    else
                    {
                        // Check piece numbers and end indicator.
                        int lastPieceNumber = -1;
                        for (int pieceNumber = 0; pieceNumber < pieces.Count; ++pieceNumber)
                        {
                            if (pieces[pieceNumber].PieceNumber != pieceNumber)
                            {
                                _ignoredItemHelper.AddItemsForInvalidSequence(s_contentTypesFileUpperInvariant, pieces, 0, pieces.Count);
                                contentTypeStreamPieces = null;
                                pieces = null;
                                break;
                            }
                            if (pieces[pieceNumber].IsLastPiece)
                            {
                                lastPieceNumber = pieceNumber;
                                break;
                            }
                        }

                        if (pieces != null)
                        {
                            // Last piece not found.
                            if (lastPieceNumber == -1)
                            {
                                _ignoredItemHelper.AddItemsForInvalidSequence(s_contentTypesFileUpperInvariant, pieces, 0, pieces.Count);
                                contentTypeStreamPieces = null;
                                pieces = null;
                            }
                            else
                            {
                                // Add any extra items after the last piece to the 
                                // the ignored items list.
                                if (lastPieceNumber < pieces.Count - 1)
                                {
                                    // The pieces we have found are extra pieces after a last piece has been found.
                                    // So we  add all the extra pieces to the ignored item list.
                                    _ignoredItemHelper.AddItemsForInvalidSequence(s_contentTypesFileUpperInvariant, pieces, lastPieceNumber + 1, pieces.Count - lastPieceNumber - 1);
                                    pieces.RemoveRange(lastPieceNumber + 1, pieces.Count - lastPieceNumber - 1);
                                }
                            }
                        }
                    }
                }
                // Detect conflict with piece name(s).
                if (_contentTypeFileInfo != null && pieces != null)
                {
                    throw new FormatException(SR.Get(SRID.BadPackageFormat));
                }
#endif

                // If an atomic file was found, open a stream on it.
                if (_contentTypeZipArchiveEntry != null)
                {
                    _contentTypeStreamExists = true;
                    return _zipStreamManager.Open(_contentTypeZipArchiveEntry, _packageFileMode, FileAccess.ReadWrite);
                }

#if false
                // ew todo don't need this
                // If the content type stream is interleaved, validate the piece numbering.
                if (pieces != null)
                {
                    _contentTypeStreamExists = true;
                    _contentTypeStreamPieces = pieces;

                    // Create an interleaved stream.
                    return new InterleavedZipPartStream(
                        pieces[0].PrefixName,
                        pieces, FileMode.Open, FileAccess.Read);
                }
#endif

                // No content type stream was found.
                return null;
            }

            // Process the attributes for the Default tag
            private void ProcessDefaultTagAttributes(XmlReader reader)
            {
                #region Default Tag

                //There could be a namespace Attribute present at this level. 
                //Also any other attribute on the <Default> tag is an error including xml: and xsi: attributes
                if (PackagingUtilities.GetNonXmlnsAttributeCount(reader) != 2)
                    throw new XmlException(SR.Get(SRID.DefaultTagDoesNotMatchSchema), null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);

                // get the required Extension and ContentType attributes

                string extensionAttributeValue = reader.GetAttribute(s_extensionAttributeName);
                ValidateXmlAttribute(s_extensionAttributeName, extensionAttributeValue, s_defaultTagName, reader);

                string contentTypeAttributeValue = reader.GetAttribute(s_contentTypeAttributeName);
                ThrowIfXmlAttributeMissing(s_contentTypeAttributeName, contentTypeAttributeValue, s_defaultTagName, reader);

                // The extensions are stored in the Default Dictionary in their original form , but they are compared
                // in a normalized manner using the ExtensionComparer.
                PackUriHelper.ValidatedPartUri temporaryUri = PackUriHelper.ValidatePartUri(
                    new Uri(s_temporaryPartNameWithoutExtension + extensionAttributeValue, UriKind.Relative));
                _defaultDictionary.Add(temporaryUri.PartUriExtension, new ContentType(contentTypeAttributeValue));

                //Skip the EndElement for Default Tag
                if (!reader.IsEmptyElement)
                    ProcessEndElement(reader, s_defaultTagName);

                #endregion Default Tag
            }

            // Process the attributes for the Default tag
            private void ProcessOverrideTagAttributes(XmlReader reader)
            {
                #region Override Tag

                //There could be a namespace Attribute present at this level. 
                //Also any other attribute on the <Override> tag is an error including xml: and xsi: attributes
                if (PackagingUtilities.GetNonXmlnsAttributeCount(reader) != 2)
                    throw new XmlException(SR.Get(SRID.OverrideTagDoesNotMatchSchema), null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);

                // get the required Extension and ContentType attributes

                string partNameAttributeValue = reader.GetAttribute(s_partNameAttributeName);
                ValidateXmlAttribute(s_partNameAttributeName, partNameAttributeValue, s_overrideTagName, reader);

                string contentTypeAttributeValue = reader.GetAttribute(s_contentTypeAttributeName);
                ThrowIfXmlAttributeMissing(s_contentTypeAttributeName, contentTypeAttributeValue, s_overrideTagName, reader);

                PackUriHelper.ValidatedPartUri partUri = PackUriHelper.ValidatePartUri(new Uri(partNameAttributeValue, UriKind.Relative));

                //Lazy initializing - ensure that the override dictionary has been initialized
                EnsureOverrideDictionary();

                // The part Uris are stored in the Override Dictionary in their original form , but they are compared
                // in a normalized manner using PartUriComparer.
                _overrideDictionary.Add(partUri, new ContentType(contentTypeAttributeValue));

                //Skip the EndElement for Override Tag
                if (!reader.IsEmptyElement)
                    ProcessEndElement(reader, s_overrideTagName);

                #endregion Override Tag
            }

            //If End element is present for Relationship then we process it
            private void ProcessEndElement(XmlReader reader, string elementName)
            {
                Debug.Assert(!reader.IsEmptyElement, "This method should only be called it the Relationship Element is not empty");

                reader.Read();

                //Skips over the following - ProcessingInstruction, DocumentType, Comment, Whitespace, or SignificantWhitespace
                reader.MoveToContent();

                if (reader.NodeType == XmlNodeType.EndElement && String.CompareOrdinal(elementName, reader.LocalName) == 0)
                    return;
                else
                    throw new XmlException(SR.Get(SRID.ElementIsNotEmptyElement, elementName), null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
            }

            private void AddOverrideElement(PackUriHelper.ValidatedPartUri partUri, ContentType contentType)
            {
                //Delete any entry corresponding in the Override dictionary 
                //corresponding to the PartUri for which the contentType is being added.
                //This is to compensate for dead override entries in the content types file.                
                DeleteContentType(partUri);

                //Lazy initializing - ensure that the override dictionary has been initialized
                EnsureOverrideDictionary();

                // The part Uris are stored in the Override Dictionary in their original form , but they are compared
                // in a normalized manner using PartUriComparer.
                _overrideDictionary.Add(partUri, contentType);
                _dirty = true;
            }

            private void AddDefaultElement(string extension, ContentType contentType)
            {
                // The extensions are stored in the Default Dictionary in their original form , but they are compared
                // in a normalized manner using the ExtensionComparer.
                _defaultDictionary.Add(extension, contentType);

                _dirty = true;
            }

            private void WriteOverrideElement(XmlWriter xmlWriter, PackUriHelper.ValidatedPartUri partUri, ContentType contentType)
            {
                xmlWriter.WriteStartElement(s_overrideTagName);
                xmlWriter.WriteAttributeString(s_partNameAttributeName,
                    partUri.PartUriString);
                xmlWriter.WriteAttributeString(s_contentTypeAttributeName, contentType.ToString());
                xmlWriter.WriteEndElement();
            }

            private void WriteDefaultElement(XmlWriter xmlWriter, string extension, ContentType contentType)
            {
                xmlWriter.WriteStartElement(s_defaultTagName);
                xmlWriter.WriteAttributeString(s_extensionAttributeName, extension);
                xmlWriter.WriteAttributeString(s_contentTypeAttributeName, contentType.ToString());
                xmlWriter.WriteEndElement();
            }

            //Validate if the required XML attribute is present and not an empty string
            private void ValidateXmlAttribute(string attributeName, string attributeValue, string tagName, XmlReader reader)
            {
                ThrowIfXmlAttributeMissing(attributeName, attributeValue, tagName, reader);

                //Checking for empty attribute
                if (attributeValue == String.Empty)
                    throw new XmlException(SR.Get(SRID.RequiredAttributeEmpty, tagName, attributeName), null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
            }


            //Validate if the required Content type XML attribute is present
            //Content type of a part can be empty
            private void ThrowIfXmlAttributeMissing(string attributeName, string attributeValue, string tagName, XmlReader reader)
            {
                if (attributeValue == null)
                    throw new XmlException(SR.Get(SRID.RequiredAttributeMissing, tagName, attributeName), null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
            }

            #endregion Private Methods

            #region Member Variables

            private Dictionary<PackUriHelper.ValidatedPartUri, ContentType> _overrideDictionary;
            private Dictionary<string, ContentType> _defaultDictionary;
            private ZipArchive _zipArchive;
            private FileMode _packageFileMode;
            private FileAccess _packageFileAccess;
            private ZipStreamManager _zipStreamManager;
            private IgnoredItemHelper _ignoredItemHelper;
            private ZipArchiveEntry _contentTypeZipArchiveEntry;
            private bool _contentTypeStreamExists;
            private bool _dirty;
            private CompressionLevel _cachedCompressionLevel;
            private static readonly string s_contentTypesFile = "[Content_Types].xml";
            private static readonly string s_contentTypesFileUpperInvariant = "[CONTENT_TYPES].XML";
            private static readonly int s_defaultDictionaryInitialSize = 16;
            private static readonly int s_overrideDictionaryInitialSize = 8;


            //Xml tag specific strings for the Content Type file
            private static readonly string s_typesNamespaceUri = "http://schemas.openxmlformats.org/package/2006/content-types";
            private static readonly string s_typesTagName = "Types";
            private static readonly string s_defaultTagName = "Default";
            private static readonly string s_extensionAttributeName = "Extension";
            private static readonly string s_contentTypeAttributeName = "ContentType";
            private static readonly string s_overrideTagName = "Override";
            private static readonly string s_partNameAttributeName = "PartName";
            private static readonly string s_temporaryPartNameWithoutExtension = "/tempfiles/sample.";

            #endregion Member Variables 
        }

        #endregion ContentTypeHelper Class

        #region IgnoredItemHelper Class

        /// <summary>
        /// This class is used to maintain a list of the zip items that currently do not 
        /// map to a part name or [ContentTypes].xml. These items may get added to the ignored 
        /// items list for one of the reasons -
        /// a. If the item encountered is a volume lable or folder in the zip archive and has a 
        ///    valid part name
        /// b. If the interleaved sequence encountered is incomplete
        /// c. If the atomic piece or complete interleaved sequence encountered
        ///    does not have a corresponding content type.        
        /// d. If the are extra pieces that are found after encountering the last piece for a 
        ///    sequence.
        /// 
        /// These items are subject to deletion if -
        /// i.   A part with a similar prefix name gets added to the package and as such we 
        ///      need to delete the existing items so that there will be no naming conflict and
        ///      we can safely at the new part.
        /// ii.  A part with an extension that matches to some of the items in the ingnored list.
        ///      We need to delete these items so that they do not show up as actual parts next
        ///      time the package is opened. 
        /// iii. A part that is getting deleted, we clean up the leftover sequences that might be
        ///      present as well
        /// 
        /// The same helper class object is used to maintain the ignored pieces corresponding to 
        /// valid part name prefixes and the [ContentTypes].xml prefix
        /// </summary>
        private class IgnoredItemHelper
        {
            #region Constructor

            /// <summary>
            /// IgnoredItemHelper - private class to keep track of all the items in the 
            /// zipArchive that can be ignored and might need to be deleted later.
            /// </summary>
            /// <param name="zipArchive"></param>
            internal IgnoredItemHelper(ZipArchive zipArchive)
            {
                _extensionDictionary = new Dictionary<string, List<string>>(_dictionaryInitialSize, s_extensionEqualityComparer);
                _ignoredItemDictionary = new Dictionary<string, List<string>>(_dictionaryInitialSize, StringComparer.Ordinal);
                _zipArchive = zipArchive;
            }

            #endregion Constructor

            #region Internal Methods

            /// <summary>
            /// Adds a partUri and zipFilename pair that corresponds to one of the following -
            /// 1. A zipFile item that has a valid part name, but does no have a content type
            /// 2. A zipFile item that may be a volume or a folder entry, that has a valid part name
            /// </summary>
            /// <param name="partUri">partUri of the item</param>
            /// <param name="zipFileName">actual zipFileName</param>
            internal void AddItemForAtomicPart(PackUriHelper.ValidatedPartUri partUri, string zipFileName)
            {
                AddItem(partUri, partUri.NormalizedPartUriString, zipFileName);
            }

#if false
            // ew todo dont need
            /// <summary>
            /// Adds an entry corresponding to the pieceInfo to the ignoredItems list if -
            /// 1. We encounter random piece items that are not a part of a complete sequence
            /// </summary>
            /// <param name="pieceInfo">pieceInfo of the item to be ignored</param>
            internal void AddItemForStrayPiece(PieceInfo pieceInfo)
            {
                AddItem(pieceInfo.PartUri, pieceInfo.NormalizedPrefixName, pieceInfo.ZipFileInfo.Name);
            }

            /// <summary>
            /// Adds an entry corresponding to the prefix name when - 
            /// 1. An invalid sequence is encountered, we record the entire sequence to be ignored.
            /// 2. If there is no content type for a valid sequence
            /// </summary>
            /// <param name="normalizedPrefixNameForThisSequence"></param>
            /// <param name="pieces"></param>
            /// <param name="startIndex"></param>
            /// <param name="count"></param>
            internal void AddItemsForInvalidSequence(string normalizedPrefixNameForThisSequence, List<PieceInfo> pieces, int startIndex, int count)
            {
                List<string> zipFileInfoNameList;

                if (_ignoredItemDictionary.ContainsKey(normalizedPrefixNameForThisSequence))
                    zipFileInfoNameList = _ignoredItemDictionary[normalizedPrefixNameForThisSequence];
                else
                {
                    zipFileInfoNameList = new List<string>(count);
                    _ignoredItemDictionary.Add(normalizedPrefixNameForThisSequence, zipFileInfoNameList);
                }

                //there is no suitable List<>.AddRange method that we can use, so have to add
                //using a "for" loop
                for (int i = startIndex; i < startIndex + count; ++i)
                {
                    zipFileInfoNameList.Add(pieces[i].ZipFileInfo.Name);
                }

                //If we are adding ignored items where the prefix name maps to the valid part name
                //the we update the extension dictionary as well
                if (pieces[startIndex].PartUri != null)
                    UpdateExtensionDictionary(pieces[startIndex].PartUri, pieces[startIndex].NormalizedPrefixName);
            }
#endif

            /// <summary>
            /// Delete all the items in the underlying archive that might have the same
            /// normalized name as that of the part being added.
            /// </summary>
            /// <param name="partUri"></param>
            internal void Delete(PackUriHelper.ValidatedPartUri partUri)
            {
                string normalizedPartName = partUri.NormalizedPartUriString;
                if (_ignoredItemDictionary.ContainsKey(normalizedPartName))
                {
                    foreach (string zipFileInfoName in _ignoredItemDictionary[normalizedPartName])
                    {
                        ZipArchiveEntry _zipArchiveEntry = this._zipArchive.GetEntry(zipFileInfoName);
                        if (_zipArchiveEntry != null)
                            _zipArchiveEntry.Delete();
                    }
                    _ignoredItemDictionary.Remove(normalizedPartName);
                }
            }

            /// <summary>
            /// If we are adding a new content type then we should delete all the items
            /// in the ignored items list that might have the similar content
            /// </summary>
            /// <param name="extension"></param>
            internal void DeleteItemsWithSimilarExtension(string extension)
            {
                if (_extensionDictionary.ContainsKey(extension))
                {
                    foreach (string normalizedPartName in _extensionDictionary[extension])
                    {
                        if (_ignoredItemDictionary.ContainsKey(normalizedPartName))
                        {
                            foreach (string zipFileInfoName in _ignoredItemDictionary[normalizedPartName])
                            {
                                ZipArchiveEntry _zipArchiveEntry = this._zipArchive.GetEntry(zipFileInfoName);
                                if (_zipArchiveEntry != null)
                                    _zipArchiveEntry.Delete();
                            }
                            _ignoredItemDictionary.Remove(normalizedPartName);
                        }
                    }
                    _extensionDictionary.Remove(extension);
                }
            }

            #endregion Internal Methods

            #region Private Methods

            private void AddItem(PackUriHelper.ValidatedPartUri partUri, string normalizedPrefixName, string zipFileName)
            {
                if (!_ignoredItemDictionary.ContainsKey(normalizedPrefixName))
                    _ignoredItemDictionary.Add(normalizedPrefixName, new List<string>(_listInitialSize));

                _ignoredItemDictionary[normalizedPrefixName].Add(zipFileName);

                //If we are adding ignored items where the prefix name maps to the valid part name
                //the we update the extension dictionary as well
                if (partUri != null)
                    UpdateExtensionDictionary(partUri, normalizedPrefixName);
            }

            private void UpdateExtensionDictionary(PackUriHelper.ValidatedPartUri partUri, string normalizedPrefixName)
            {
                string extension = partUri.PartUriExtension;

                if (!_extensionDictionary.ContainsKey(extension))
                    _extensionDictionary.Add(extension, new List<string>(_listInitialSize));

                _extensionDictionary[extension].Add(normalizedPrefixName);
            }

            #endregion Private Methods

            #region Private Member Variables

            private const int _dictionaryInitialSize = 8;
            private const int _listInitialSize = 1;

            //dictionary mapping a normalized prefix name to different items
            //with the same prefix name.
            private Dictionary<string, List<string>> _ignoredItemDictionary;

            //using an additional extension dictionary to map an extenstion to 
            //different prefix names with the same extension, in order to 
            //reduce the string parsing
            private Dictionary<string, List<string>> _extensionDictionary;


            private ZipArchive _zipArchive;

            #endregion Private Member Variables
        }

        #endregion IgnoredItemHelper Class

        #endregion Private Class
    }
}
