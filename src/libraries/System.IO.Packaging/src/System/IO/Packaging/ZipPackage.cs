// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Xml;                   //Required for Content Type File manipulation

namespace System.IO.Packaging
{
    /// <summary>
    /// ZipPackage is a specific implementation for the abstract Package
    /// class, corresponding to the Zip file format.
    /// This is a part of the Packaging Layer APIs.
    /// </summary>
    public sealed class ZipPackage : Package
    {
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
                throw new ArgumentNullException(nameof(contentType));

            Package.ThrowIfCompressionOptionInvalid(compressionOption);

            // Convert XPS CompressionOption to Zip CompressionMethodEnum.
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
        protected override PackagePart? GetPartCore(Uri partUri)
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
            //  2. Also, its not a straightforward task to determine if a new part was
            //     added as we need to look for atomic as well as interleaved parts and
            //     this has to be done in a case sensitive manner. So, effectively
            //     we will have to go through the entire list of zip items to determine
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
            PackUriHelper.ValidatedPartUri validatedUri = PackUriHelper.ValidatePartUri(partUri);

            string partZipName = GetZipItemNameFromOpcName(PackUriHelper.GetStringForPartUri(validatedUri));
            ZipArchiveEntry? zipArchiveEntry = _zipArchive.GetEntry(partZipName);
            if (zipArchiveEntry != null)
            {
                // Case of an atomic part.
                zipArchiveEntry?.Delete();
            }
            else
            {
                // This can happen if the part is interleaved.
                // Important Note: This method relies on the fact that the base class does not
                // clean up all the information about the part to be deleted before this method
                // is called. If ever that behaviour in Package.Delete() changes, this method
                // should be changed.
                // Ideally we would have liked to avoid this kind of a restriction but due to the
                // current class interfaces and data structure ownerships between these objects,
                // it's tough to re-design at this point.
                if (PartExists(validatedUri))
                {
                    ZipPackagePart partToDelete = (ZipPackagePart)GetPart(validatedUri);
                    // If the part has a non-null PieceDescriptors property, it is interleaved.
                    List<ZipPackagePartPiece>? pieceDescriptors = partToDelete.PieceDescriptors;

                    if (pieceDescriptors != null && pieceDescriptors.Count > 0)
                    {
                        DeleteInterleavedPartOrStream(pieceDescriptors);
                    }
                }
            }

            // We are not absolutely required to clean up all the items in the ignoredItems list,
            // but it will help to clean up incomplete and leftover pieces that belonged to the same
            // part.
            _ignoredItemHelper.Delete(validatedUri);

            //Delete the content type for this part if it was specified as an override
            _contentTypeHelper.DeleteContentType(validatedUri);
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
            List<PackagePart> parts = new List<PackagePart>(InitialPartListSize);
            SortedSet<ZipPackagePartPiece> pieceSet = new SortedSet<ZipPackagePartPiece>();

            // The list of files has to be searched linearly (1) to identify the content type
            // stream, and (2) to identify parts.
            System.Collections.ObjectModel.ReadOnlyCollection<ZipArchiveEntry> zipArchiveEntries = _zipArchive.Entries;

            // We have already identified the [ContentTypes].xml pieces if any are present during
            // the initialization of ZipPackage object

            // Record parts and ignored items.
            foreach (ZipArchiveEntry zipArchiveEntry in zipArchiveEntries)
            {
                //Returns false if -
                // a. its a content type item
                // b. items that have either a leading or trailing slash.
                if (IsZipItemValidOpcPartOrPiece(zipArchiveEntry.FullName))
                {
                    // In the case of a piece name, postpone processing until all piece
                    // candidates have been collected.
                    if (ZipPackagePartPiece.TryParse(zipArchiveEntry, out ZipPackagePartPiece? partPiece))
                    {
                        if (pieceSet.Contains(partPiece))
                        {
                            throw new FormatException(SR.DuplicatePiecesFound);
                        }

                        if (partPiece.PartUri != null)
                        {
                            // If a part does not have a valid URI, then we should just ignore it.
                            // It is not meaningful to even add it to the ignored items list as we will
                            // never generate a name that corresponds to this ZIP item and as such will
                            // never have to delete it.
                            pieceSet.Add(partPiece);
                        }
                        continue;
                    }

                    Uri partUri = new Uri(GetOpcNameFromZipItemName(zipArchiveEntry.FullName), UriKind.Relative);
                    if (PackUriHelper.TryValidatePartUri(partUri, out PackUriHelper.ValidatedPartUri? validatedPartUri))
                    {
                        ContentType? contentType = _contentTypeHelper.GetContentType(validatedPartUri);
                        if (contentType != null)
                        {
                            // In case there was some redundancy between pieces and/or the atomic
                            // part, it will be detected at this point because the part's Uri (which
                            // is independent of interleaving) will already be in the dictionary.
                            parts.Add(new ZipPackagePart(this, zipArchiveEntry.Archive, zipArchiveEntry,
                                _zipStreamManager, validatedPartUri, contentType.ToString(), GetCompressionOptionFromZipFileInfo()));
                        }
                        else
                        {
                            // Since this part does not have a valid content type we add it to the ignored list,
                            // as later if another part with a similar extension gets added, this part might become
                            // valid the next time we open the package.
                            _ignoredItemHelper.AddItemForAtomicPart(validatedPartUri, zipArchiveEntry.FullName);
                        }
                    }
                    //If not valid part uri we can completely ignore this zip file item. Even if later someone adds
                    //a new part, the corresponding zip item can never map to one of these items
                }
                // If IsZipItemValidOpcPartOrPiece returns false, it implies that either the zip file Item
                // starts or ends with a "/" and as such we can completely ignore this zip file item. Even if later
                // a new part gets added, its corresponding zip item cannot map to one of these items.
            }

            // TODO: The original would add ZipItemNames to the ignored item helper if they had folder or volume entries
            // in the ZIP file and were a valid name for a part. We don't do that. I don't think this is a problem.

            // Well-formed piece sequences get recorded in parts.
            // Debris from invalid sequences get swept into _ignoredItems.
            ProcessPieces(pieceSet, parts);

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
                    _contentTypeHelper?.SaveToFile();
                    _zipArchive?.Dispose();

                    // _containerStream may be opened given a file name, in which case it should be closed here.
                    // _containerStream may be passed into the constructor, in which case, it should not be closed here.
                    if (_shouldCloseContainerStream)
                    {
                        _containerStream.Dispose();
                    }
                    _containerStream = null!;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        #endregion Other Methods

        #endregion Public Methods

        #region Internal Constructors

        /// <summary>
        /// Internal constructor that is called by the OpenOnFile static method.
        /// </summary>
        /// <param name="path">File path to the container.</param>
        /// <param name="packageFileMode">Container is opened in the specified mode if possible</param>
        /// <param name="packageFileAccess">Container is opened with the specified access if possible</param>
        /// <param name="share">Container is opened with the specified share if possible</param>

        internal ZipPackage(string path, FileMode packageFileMode, FileAccess packageFileAccess, FileShare share)
            : base(packageFileAccess)
        {
            ZipArchive? zipArchive = null;
            IgnoredItemHelper? ignoredItemHelper;
            ContentTypeHelper? contentTypeHelper;
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

                zipArchive = new ZipArchive(_containerStream, zipArchiveMode, true);
                _zipStreamManager = new ZipStreamManager(zipArchive, _packageFileMode, _packageFileAccess);
                ignoredItemHelper = new IgnoredItemHelper(zipArchive);
                contentTypeHelper = new ContentTypeHelper(zipArchive, _packageFileMode, _packageFileAccess, _zipStreamManager, ignoredItemHelper);
            }
            catch
            {
                zipArchive?.Dispose();
                _containerStream?.Dispose();

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
            ZipArchive? zipArchive = null;
            IgnoredItemHelper? ignoredItemHelper;
            ContentTypeHelper? contentTypeHelper;
            _packageFileMode = packageFileMode;
            _packageFileAccess = packageFileAccess;

            try
            {
                if (s.CanSeek)
                {
                    switch (packageFileMode)
                    {
                        case FileMode.Open:
                            if (s.Length == 0)
                            {
                                throw new FileFormatException(SR.ZipZeroSizeFileIsNotValidArchive);
                            }
                            break;

                        case FileMode.CreateNew:
                            if (s.Length != 0)
                            {
                                throw new IOException(SR.CreateNewOnNonEmptyStream);
                            }
                            break;

                        case FileMode.Create:
                            if (s.Length != 0)
                            {
                                s.SetLength(0); // Discard existing data
                            }
                            break;
                    }
                }

                ZipArchiveMode zipArchiveMode = ZipArchiveMode.Update;
                if (packageFileAccess == FileAccess.Read)
                    zipArchiveMode = ZipArchiveMode.Read;
                else if (packageFileAccess == FileAccess.Write)
                    zipArchiveMode = ZipArchiveMode.Create;
                else if (packageFileAccess == FileAccess.ReadWrite)
                    zipArchiveMode = ZipArchiveMode.Update;

                zipArchive = new ZipArchive(s, zipArchiveMode, true);

                _zipStreamManager = new ZipStreamManager(zipArchive, packageFileMode, packageFileAccess);
                ignoredItemHelper = new IgnoredItemHelper(zipArchive);
                contentTypeHelper = new ContentTypeHelper(zipArchive, packageFileMode, packageFileAccess, _zipStreamManager, ignoredItemHelper);
            }
            catch (InvalidDataException)
            {
                throw new FileFormatException(SR.FileContainsCorruptedData);
            }
            catch
            {
                zipArchive?.Dispose();

                throw;
            }

            _containerStream = s;
            _shouldCloseContainerStream = false;
            _zipArchive = zipArchive;
            _ignoredItemHelper = ignoredItemHelper;
            _contentTypeHelper = contentTypeHelper;
        }

        #endregion Internal Constructors

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
            return string.Concat(ForwardSlashString, zipItemName);
        }

        // Convert from XPS CompressionOption to ZipFileInfo compression properties.
        internal static void GetZipCompressionMethodFromOpcCompressionOption(
            CompressionOption compressionOption,
            out CompressionLevel compressionLevel)
        {
            switch (compressionOption)
            {
                case CompressionOption.NotCompressed:
                    {
                        compressionLevel = CompressionLevel.NoCompression;
                    }
                    break;
                case CompressionOption.Normal:
                    {
                        compressionLevel = CompressionLevel.Optimal;
                    }
                    break;
                case CompressionOption.Maximum:
                    {
                        compressionLevel = CompressionLevel.Optimal;
                    }
                    break;
                case CompressionOption.Fast:
                    {
                        compressionLevel = CompressionLevel.Fastest;
                    }
                    break;
                case CompressionOption.SuperFast:
                    {
                        compressionLevel = CompressionLevel.Fastest;
                    }
                    break;

                // fall-through is not allowed
                default:
                    {
                        Debug.Fail("Encountered an invalid CompressionOption enum value");
                        goto case CompressionOption.NotCompressed;
                    }
            }
        }

        #endregion Internal Methods

        internal FileMode PackageFileMode
        {
            get
            {
                return _packageFileMode;
            }
        }

        #region Private Methods

        //returns a boolean indicating if the underlying zip item is a valid metro part or piece
        // This mainly excludes the content type item, as well as entries with leading or trailing
        // slashes.
        private static bool IsZipItemValidOpcPartOrPiece(string zipItemName)
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
                //Some of the tools support this and some don't. There is no way ensure that the zip item never have
                //a leading "/", although this is a requirement we impose on items created through our API
                //Therefore we ignore them at the packaging api level.
                if (zipItemName.StartsWith(ForwardSlashString, StringComparison.Ordinal))
                    return false;
                //This will ignore the folder entries found in the zip package created by some zip tool
                //PartNames ending with a "/" slash is also invalid so we are skipping these entries,
                //this will also prevent the PackUriHelper.CreatePartUri from throwing when it encounters a
                // partname ending with a "/"
                if (zipItemName.EndsWith(ForwardSlashString, StringComparison.Ordinal))
                    return false;
                else
                    return true;
            }
        }

        // convert from Zip CompressionMethodEnum and DeflateOptionEnum to XPS CompressionOption
        private static CompressionOption GetCompressionOptionFromZipFileInfo()
        {
            // Note: we can't determine compression method / level from the ZipArchiveEntry.
            CompressionOption result = CompressionOption.Normal;
            return result;
        }

        private static void DeleteInterleavedPartOrStream(List<ZipPackagePartPiece> sortedPieceInfoList)
        {
            Debug.Assert(sortedPieceInfoList != null);
            if (sortedPieceInfoList.Count > 0)
            {
                foreach (ZipPackagePartPiece pieceInfo in sortedPieceInfoList)
                {
                    pieceInfo.ZipArchiveEntry.Delete();
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
        private void ProcessPieces(SortedSet<ZipPackagePartPiece> pieceSet, List<PackagePart> parts)
        {
            // The zip items related to the ContentTypes.xml should have been already processed.
            // Only those zip items that follow the valid piece naming syntax and have a valid
            // part name should show up in this list.
            // piece.PartUri should be non-null

            // Exit if nothing to do.
            if (pieceSet.Count == 0)
                return;

            string? normalizedPrefixNameForCurrentSequence = null;
            // Value is ignored as long as prefixNameForCurrentSequence is null.
            int startIndexOfCurrentSequence = 0;
            List<ZipPackagePartPiece> pieces = new List<ZipPackagePartPiece>(pieceSet);

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
                    if (pieces[i].NormalizedPrefixName != normalizedPrefixNameForCurrentSequence)
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
            List<ZipPackagePartPiece> pieces,
            int startIndex,
            int numItems,
            List<PackagePart> parts)
        {
            // The Uri and content type are inferred from the unsuffixed name of the
            // first piece.
            PackUriHelper.ValidatedPartUri partUri = pieces[startIndex].PartUri!;
            ContentType? contentType = _contentTypeHelper.GetContentType(partUri);
            if (contentType == null)
            {
                _ignoredItemHelper.AddItemsForInvalidSequence(normalizedPrefixNameForCurrentSequence, pieces, startIndex, numItems);
                return;
            }

            // Add a new part, initializing with an array of PieceInfo.
            parts.Add(new ZipPackagePart(this, _zipArchive, _zipStreamManager, pieces.GetRange(startIndex, numItems), partUri, contentType.ToString(),
                GetCompressionOptionFromZipFileInfo()));
        }

        #endregion Private Methods

        #region Private Members

        private const int InitialPartListSize = 50;

        private readonly ZipArchive _zipArchive;
        private Stream _containerStream;      // stream we are opened in if Open(Stream) was called
        private readonly bool _shouldCloseContainerStream;
        private readonly ContentTypeHelper _contentTypeHelper;    // manages the content types for all the parts in the container
        private readonly IgnoredItemHelper _ignoredItemHelper;    // manages the ignored items in a ZIP package
        private readonly ZipStreamManager _zipStreamManager;      // manages streams for all parts, avoiding opening streams multiple times
        private readonly FileAccess _packageFileAccess;
        private readonly FileMode _packageFileMode;

        private const string ForwardSlashString = "/"; //Required for creating a part name from a zip item name

        //IEqualityComparer for extensions
        private static readonly ExtensionEqualityComparer s_extensionEqualityComparer = new ExtensionEqualityComparer();

        #endregion Private Members

        /// <summary>
        /// ExtensionComparer
        /// The Extensions are stored in the Default Dictionary in their original form,
        /// however they are compared in a normalized manner.
        /// Equivalence for extensions in the content type stream, should follow
        /// the same rules as extensions of partnames. Also, by the time this code is invoked,
        /// we have already validated, that the extension is in the correct format as per the
        /// part name rules.So we are simplifying the logic here to just convert the extensions
        /// to Upper invariant form and then compare them.
        /// </summary>
        private sealed class ExtensionEqualityComparer : IEqualityComparer<string>
        {
            bool IEqualityComparer<string>.Equals(string? extensionA, string? extensionB)
            {
                Debug.Assert(extensionA != null, "extension should not be null");
                Debug.Assert(extensionB != null, "extension should not be null");

                //Important Note: any change to this should be made in accordance
                //with the rules for comparing/normalizing partnames.
                //Refer to PackUriHelper.ValidatedPartUri.GetNormalizedPartUri method.
                //Currently normalization just involves upper-casing ASCII and hence the simplification.
                return extensionA.Equals(extensionB, StringComparison.InvariantCultureIgnoreCase);
            }

            int IEqualityComparer<string>.GetHashCode(string extension)
            {
                Debug.Assert(extension != null, "extension should not be null");

                //Important Note: any change to this should be made in accordance
                //with the rules for comparing/normalizing partnames.
                //Refer to PackUriHelper.ValidatedPartUri.GetNormalizedPartUri method.
                //Currently normalization just involves upper-casing ASCII and hence the simplification.
                return extension.ToUpperInvariant().GetHashCode();
            }
        }

        /// <summary>
        /// This is a helper class that maintains the Content Types File related to
        /// this ZipPackage.
        /// </summary>
        private sealed class ContentTypeHelper
        {
            /// <summary>
            /// Initialize the object without uploading any information from the package.
            /// Complete initialization in read mode also involves calling ParseContentTypesFile
            /// to deserialize content type information.
            /// </summary>
            internal ContentTypeHelper(ZipArchive zipArchive, FileMode packageFileMode, FileAccess packageFileAccess, ZipStreamManager zipStreamManager, IgnoredItemHelper ignoredItemHelper)
            {
                _zipArchive = zipArchive;               //initialized in the ZipPackage constructor
                _packageFileMode = packageFileMode;
                _packageFileAccess = packageFileAccess;
                _zipStreamManager = zipStreamManager;   //initialized in the ZipPackage constructor
                // The extensions are stored in the default Dictionary in their original form , but they are compared
                // in a normalized manner using the ExtensionComparer.
                _defaultDictionary = new Dictionary<string, ContentType>(DefaultDictionaryInitialSize, s_extensionEqualityComparer);

                _ignoredItemHelper = ignoredItemHelper;

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

            internal static string ContentTypeFileName
            {
                get
                {
                    return ContentTypesFile;
                }
            }

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
                    || (_defaultDictionary.TryGetValue(extension, out ContentType? value)
                        && !(foundMatchingDefault = value.AreTypeAndSubTypeEqual(contentType))))
                {
                    AddOverrideElement(partUri, contentType);
                }

                // Else, either there is already a mapping from extension to contentType,
                // or one needs to be created.
                else if (!foundMatchingDefault)
                {
                    AddDefaultElement(extension, contentType);

                    // Delete all items that might map to the same extension, as these currently
                    // ignored items might show up as valid parts later.
                    _ignoredItemHelper.DeleteItemsWithSimilarExtension(extension);
                }
            }


            //Returns the content type for the part, if present, else returns null.
            internal ContentType? GetContentType(PackUriHelper.ValidatedPartUri partUri)
            {
                //Step 1: Check if there is an override entry present corresponding to the
                //partUri provided. Override takes precedence over the default entries
                if (_overrideDictionary != null)
                {
                    if (_overrideDictionary.TryGetValue(partUri, out ContentType? val))
                        return val;
                }

                //Step 2: Check if there is a default entry corresponding to the
                //extension of the partUri provided.
                string extension = partUri.PartUriExtension;

                if (_defaultDictionary.TryGetValue(extension, out ContentType? value))
                    return value;

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
                        _contentTypeZipArchiveEntry = _zipArchive.CreateEntry(ContentTypesFile, _cachedCompressionLevel);
                        _contentTypeStreamExists = true;
                    }
                    else
                    {
                        // delete and re-create entry for content part.  When writing this, the stream will not truncate the content
                        // if the XML is shorter than the existing content part.

                        if (_contentTypeStreamPieces != null)
                        {
                            // Delete all part pieces for content part.
                            DeleteInterleavedPartOrStream(_contentTypeStreamPieces);
                        }
                        else
                        {
                            // Atomic name
                            _contentTypeZipArchiveEntry!.Delete();
                        }
                        _contentTypeZipArchiveEntry = _zipArchive.CreateEntry(ContentTypesFile);
                    }

                    using (Stream s = _zipStreamManager.Open(_contentTypeZipArchiveEntry, FileAccess.ReadWrite))
                    {
                        // use UTF-8 encoding by default
                        using (XmlWriter writer = XmlWriter.Create(s, new XmlWriterSettings { Encoding = System.Text.Encoding.UTF8 }))
                        {
                            writer.WriteStartDocument();

                            // write root element tag - Types
                            writer.WriteStartElement(TypesTagName, TypesNamespaceUri);

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

            [MemberNotNull(nameof(_overrideDictionary))]
            private void EnsureOverrideDictionary()
            {
                // The part Uris are stored in the Override Dictionary in their original form , but they are compared
                // in a normalized manner using the PartUriComparer
                _overrideDictionary ??= new Dictionary<PackUriHelper.ValidatedPartUri, ContentType>(OverrideDictionaryInitialSize);
            }

            private void ParseContentTypesFile(System.Collections.ObjectModel.ReadOnlyCollection<ZipArchiveEntry> zipFiles)
            {
                // Find the content type stream, allowing for interleaving. Naming collisions
                // (as between an atomic and an interleaved part) will result in an exception being thrown.
                Stream? s = OpenContentTypeStream(zipFiles);

                // Allow non-existent content type stream.
                if (s == null)
                    return;

                XmlReaderSettings xrs = new XmlReaderSettings();
                xrs.IgnoreWhitespace = true;

                using (s)
                using (XmlReader reader = XmlReader.Create(s, xrs))
                {
                    //This method expects the reader to be in ReadState.Initial.
                    //It will make the first read call.
                    PackagingUtilities.PerformInitialReadAndVerifyEncoding(reader);

                    //Note: After the previous method call the reader should be at the first tag in the markup.
                    //MoveToContent - Skips over the following - ProcessingInstruction, DocumentType, Comment, Whitespace, or SignificantWhitespace
                    //If the reader is currently at a content node then this function call is a no-op
                    reader.MoveToContent();

                    // look for our root tag and namespace pair - ignore others in case of version changes
                    // Make sure that the current node read is an Element
                    if ((reader.NodeType == XmlNodeType.Element)
                        && (reader.Depth == 0)
                        && (reader.NamespaceURI == TypesNamespaceUri)
                        && (reader.Name == TypesTagName))
                    {
                        //There should be a namespace Attribute present at this level.
                        //Also any other attribute on the <Types> tag is an error including xml: and xsi: attributes
                        if (PackagingUtilities.GetNonXmlnsAttributeCount(reader) > 0)
                        {
                            throw new XmlException(SR.TypesTagHasExtraAttributes, null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
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
                                && (reader.NamespaceURI == TypesNamespaceUri)
                                && (reader.Name == DefaultTagName))
                            {
                                ProcessDefaultTagAttributes(reader);
                            }
                            else if (reader.NodeType == XmlNodeType.Element
                                     && reader.Depth == 1
                                     && (reader.NamespaceURI == TypesNamespaceUri)
                                     && (reader.Name == OverrideTagName))
                            {
                                ProcessOverrideTagAttributes(reader);
                            }
                            else if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == 0 && reader.Name == TypesTagName)
                            {
                                continue;
                            }
                            else
                            {
                                throw new XmlException(SR.TypesXmlDoesNotMatchSchema, null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
                            }
                        }
                    }
                    else
                    {
                        throw new XmlException(SR.TypesElementExpected, null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
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
            private Stream? OpenContentTypeStream(System.Collections.ObjectModel.ReadOnlyCollection<ZipArchiveEntry> zipFiles)
            {
                // Collect all pieces found prior to sorting and validating the sequence.
                SortedDictionary<ZipPackagePartPiece, ZipArchiveEntry>? contentTypeStreamPieces = null;

                foreach (ZipArchiveEntry zipFileInfo in zipFiles)
                {
                    if (zipFileInfo.FullName.StartsWith(ContentTypesFileUpperInvariant, StringComparison.OrdinalIgnoreCase))
                    {
                        // Atomic name.
                        if (zipFileInfo.FullName.Length == ContentTypeFileName.Length)
                        {
                            // Record the file info.
                            _contentTypeZipArchiveEntry = zipFileInfo;
                        }
                        else if (ZipPackagePartPiece.TryParse(zipFileInfo, out ZipPackagePartPiece? pieceInfo))
                        {
                            // Lazy init.
                            contentTypeStreamPieces ??= new SortedDictionary<ZipPackagePartPiece, ZipArchiveEntry>();

                            // Record the piece info.
                            contentTypeStreamPieces.Add(pieceInfo, zipFileInfo);
                        }
                    }
                }

                List<ZipPackagePartPiece>? partPieces = null;

                // If pieces were found, find out if there is a piece 0, in which case
                // sequence validity will be required.
                // Since the general case requires a sorted array, use a sorted array for this.
                if (contentTypeStreamPieces != null)
                {
                    partPieces = new List<ZipPackagePartPiece>(contentTypeStreamPieces.Keys);

                    // Negative piece numbers are invalid, so if piece 0 occurs at all, it occurs first.
                    if (partPieces[0].PieceNumber != 0)
                    {
                        // The pieces we have found form an incomplete sequence as the first
                        // piece is missing. So owe add these to the list of ignored items.
                        _ignoredItemHelper.AddItemsForInvalidSequence(ContentTypesFileUpperInvariant, partPieces, 0, partPieces.Count);

                        partPieces = null;
                    }
                    else
                    {
                        // Check piece numbers and end indicator.
                        int lastPieceNumber = -1;

                        for (int pieceNumber = 0; pieceNumber < partPieces.Count; ++pieceNumber)
                        {
                            if (partPieces[pieceNumber].PieceNumber != pieceNumber)
                            {
                                _ignoredItemHelper.AddItemsForInvalidSequence(ContentTypesFileUpperInvariant, partPieces, 0, partPieces.Count);

                                partPieces = null;
                                break;
                            }

                            if (partPieces[pieceNumber].IsLastPiece)
                            {
                                lastPieceNumber = pieceNumber;
                                break;
                            }
                        }

                        if (partPieces != null)
                        {
                            // Last piece not found
                            if (lastPieceNumber == -1)
                            {
                                _ignoredItemHelper.AddItemsForInvalidSequence(ContentTypesFileUpperInvariant, partPieces, 0, partPieces.Count);

                                partPieces = null;
                            }
                            else
                            {
                                // Add any extra items after the last piece to the ignored items list.
                                if (lastPieceNumber < partPieces.Count - 1)
                                {
                                    // The pieces we have found are extra pieces after a last piece has been found.
                                    // So we add all the extra pieces to the ignored item list.
                                    _ignoredItemHelper.AddItemsForInvalidSequence(ContentTypesFileUpperInvariant, partPieces, lastPieceNumber + 1, partPieces.Count - lastPieceNumber - 1);
                                    partPieces.RemoveRange(lastPieceNumber + 1, partPieces.Count - lastPieceNumber - 1);
                                }
                            }
                        }
                    }
                }

                // If an atomic file was found, open a stream on it.
                if (_contentTypeZipArchiveEntry != null)
                {
                    // Detect conflict with piece name(s).
                    if (contentTypeStreamPieces != null)
                    {
                        throw new FormatException(SR.BadPackageFormat);
                    }

                    _contentTypeStreamExists = true;
                    return _zipStreamManager.Open(_contentTypeZipArchiveEntry, FileAccess.ReadWrite);
                }
                // If the content type stream is interleaved, validate the piece numbering.
                else if (partPieces != null)
                {
                    _contentTypeStreamExists = true;
                    _contentTypeStreamPieces = partPieces;

                    return new InterleavedZipPackagePartStream(_zipStreamManager, _contentTypeStreamPieces, FileAccess.Read);
                }

                // No content type stream was found.
                return null;
            }

            // Process the attributes for the Default tag
            private void ProcessDefaultTagAttributes(XmlReader reader)
            {
                //There could be a namespace Attribute present at this level.
                //Also any other attribute on the <Default> tag is an error including xml: and xsi: attributes
                if (PackagingUtilities.GetNonXmlnsAttributeCount(reader) != 2)
                    throw new XmlException(SR.DefaultTagDoesNotMatchSchema, null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);

                // get the required Extension and ContentType attributes

                string? extensionAttributeValue = reader.GetAttribute(ExtensionAttributeName);
                ValidateXmlAttribute(ExtensionAttributeName, extensionAttributeValue, DefaultTagName, reader);

                string? contentTypeAttributeValue = reader.GetAttribute(ContentTypeAttributeName);
                ThrowIfXmlAttributeMissing(ContentTypeAttributeName, contentTypeAttributeValue, DefaultTagName, reader);

                // The extensions are stored in the Default Dictionary in their original form , but they are compared
                // in a normalized manner using the ExtensionComparer.
                PackUriHelper.ValidatedPartUri temporaryUri = PackUriHelper.ValidatePartUri(
                    new Uri(TemporaryPartNameWithoutExtension + extensionAttributeValue, UriKind.Relative));
                _defaultDictionary.Add(temporaryUri.PartUriExtension, new ContentType(contentTypeAttributeValue!));

                //Skip the EndElement for Default Tag
                if (!reader.IsEmptyElement)
                    ProcessEndElement(reader, DefaultTagName);
            }

            // Process the attributes for the Default tag
            private void ProcessOverrideTagAttributes(XmlReader reader)
            {
                //There could be a namespace Attribute present at this level.
                //Also any other attribute on the <Override> tag is an error including xml: and xsi: attributes
                if (PackagingUtilities.GetNonXmlnsAttributeCount(reader) != 2)
                    throw new XmlException(SR.OverrideTagDoesNotMatchSchema, null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);

                // get the required Extension and ContentType attributes
                string? partNameAttributeValue = reader.GetAttribute(PartNameAttributeName);
                ValidateXmlAttribute(PartNameAttributeName, partNameAttributeValue, OverrideTagName, reader);

                string? contentTypeAttributeValue = reader.GetAttribute(ContentTypeAttributeName);
                ThrowIfXmlAttributeMissing(ContentTypeAttributeName, contentTypeAttributeValue, OverrideTagName, reader);

                PackUriHelper.ValidatedPartUri partUri = PackUriHelper.ValidatePartUri(new Uri(partNameAttributeValue!, UriKind.Relative));

                //Lazy initializing - ensure that the override dictionary has been initialized
                EnsureOverrideDictionary();

                // The part Uris are stored in the Override Dictionary in their original form , but they are compared
                // in a normalized manner using PartUriComparer.
                _overrideDictionary.Add(partUri, new ContentType(contentTypeAttributeValue!));

                //Skip the EndElement for Override Tag
                if (!reader.IsEmptyElement)
                    ProcessEndElement(reader, OverrideTagName);
            }

            //If End element is present for Relationship then we process it
            private static void ProcessEndElement(XmlReader reader, string elementName)
            {
                Debug.Assert(!reader.IsEmptyElement, "This method should only be called it the Relationship Element is not empty");

                reader.Read();

                //Skips over the following - ProcessingInstruction, DocumentType, Comment, Whitespace, or SignificantWhitespace
                reader.MoveToContent();

                if (reader.NodeType == XmlNodeType.EndElement && elementName == reader.LocalName)
                    return;
                else
                    throw new XmlException(SR.Format(SR.ElementIsNotEmptyElement, elementName), null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
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

            private static void WriteOverrideElement(XmlWriter xmlWriter, PackUriHelper.ValidatedPartUri partUri, ContentType contentType)
            {
                xmlWriter.WriteStartElement(OverrideTagName);
                xmlWriter.WriteAttributeString(PartNameAttributeName,
                    partUri.PartUriString);
                xmlWriter.WriteAttributeString(ContentTypeAttributeName, contentType.ToString());
                xmlWriter.WriteEndElement();
            }

            private static void WriteDefaultElement(XmlWriter xmlWriter, string extension, ContentType contentType)
            {
                xmlWriter.WriteStartElement(DefaultTagName);
                xmlWriter.WriteAttributeString(ExtensionAttributeName, extension);
                xmlWriter.WriteAttributeString(ContentTypeAttributeName, contentType.ToString());
                xmlWriter.WriteEndElement();
            }

            //Validate if the required XML attribute is present and not an empty string
            private static void ValidateXmlAttribute(string attributeName, string? attributeValue, string tagName, XmlReader reader)
            {
                ThrowIfXmlAttributeMissing(attributeName, attributeValue, tagName, reader);

                //Checking for empty attribute
                if (attributeValue!.Length == 0)
                    throw new XmlException(SR.Format(SR.RequiredAttributeEmpty, tagName, attributeName), null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
            }


            //Validate if the required Content type XML attribute is present
            //Content type of a part can be empty
            private static void ThrowIfXmlAttributeMissing(string attributeName, string? attributeValue, string tagName, XmlReader reader)
            {
                if (attributeValue == null)
                    throw new XmlException(SR.Format(SR.RequiredAttributeMissing, tagName, attributeName), null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
            }

            private Dictionary<PackUriHelper.ValidatedPartUri, ContentType>? _overrideDictionary;
            private readonly Dictionary<string, ContentType> _defaultDictionary;
            private readonly ZipArchive _zipArchive;
            private readonly IgnoredItemHelper _ignoredItemHelper;
            private readonly FileMode _packageFileMode;
            private readonly FileAccess _packageFileAccess;
            private readonly ZipStreamManager _zipStreamManager;
            private ZipArchiveEntry? _contentTypeZipArchiveEntry;
            private List<ZipPackagePartPiece>? _contentTypeStreamPieces;
            private bool _contentTypeStreamExists;
            private bool _dirty;
            private CompressionLevel _cachedCompressionLevel;
            private const string ContentTypesFile = "[Content_Types].xml";
            private const string ContentTypesFileUpperInvariant = "[CONTENT_TYPES].XML";
            private const int DefaultDictionaryInitialSize = 16;
            private const int OverrideDictionaryInitialSize = 8;

            //Xml tag specific strings for the Content Type file
            private const string TypesNamespaceUri = "http://schemas.openxmlformats.org/package/2006/content-types";
            private const string TypesTagName = "Types";
            private const string DefaultTagName = "Default";
            private const string ExtensionAttributeName = "Extension";
            private const string ContentTypeAttributeName = "ContentType";
            private const string OverrideTagName = "Override";
            private const string PartNameAttributeName = "PartName";
            private const string TemporaryPartNameWithoutExtension = "/tempfiles/sample.";
        }

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
        private sealed class IgnoredItemHelper
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

            /// <summary>
            /// Adds an entry corresponding to the pieceInfo to the ignoredItems list if -
            /// 1. We encounter random piece items that are not a part of a complete sequence
            /// </summary>
            /// <param name="pieceInfo">pieceInfo of the item to be ignored</param>
            internal void AddItemForStrayPiece(ZipPackagePartPiece pieceInfo)
            {
                AddItem(pieceInfo.PartUri, pieceInfo.NormalizedPrefixName, pieceInfo.ZipArchiveEntry.FullName);
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
            internal void AddItemsForInvalidSequence(string normalizedPrefixNameForThisSequence, List<ZipPackagePartPiece> pieces, int startIndex, int count)
            {
                if (! _ignoredItemDictionary.TryGetValue(normalizedPrefixNameForThisSequence, out List<string>? zipFileInfoNameList))
                {
                    zipFileInfoNameList = new List<string>(count);
                    _ignoredItemDictionary.Add(normalizedPrefixNameForThisSequence, zipFileInfoNameList);
                }

                //there is no suitable List<>.AddRange method that we can use, so have to add
                //using a "for" loop
                for (int i = startIndex; i < startIndex + count; ++i)
                {
                    zipFileInfoNameList.Add(pieces[i].ZipArchiveEntry.FullName);
                }

                //If we are adding ignored items where the prefix name maps to the valid part name
                //the we update the extension dictionary as well
                if (pieces[startIndex].PartUri != null)
                {
                    UpdateExtensionDictionary(pieces[startIndex].PartUri!, pieces[startIndex].NormalizedPrefixName);
                }
            }

            /// <summary>
            /// Delete all the items in the underlying archive that might have the same
            /// normalized name as that of the part being added.
            /// </summary>
            /// <param name="partUri"></param>
            internal void Delete(PackUriHelper.ValidatedPartUri partUri)
                => DeleteCore(partUri.NormalizedPartUriString);

            private void DeleteCore(string normalizedPartName)
            {
                if (_ignoredItemDictionary.TryGetValue(normalizedPartName, out List<string>? zipFileInfoNames))
                {
                    foreach (string zipFileInfoName in zipFileInfoNames)
                    {
                        ZipArchiveEntry? entry = _zipArchive.GetEntry(zipFileInfoName);

                        entry?.Delete();
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
                if (_extensionDictionary.TryGetValue(extension, out List<string>? normalizedPartNames))
                {
                    foreach (string normalizedPartName in normalizedPartNames)
                    {
                        DeleteCore(normalizedPartName);
                    }
                    _extensionDictionary.Remove(extension);
                }
            }

            #endregion Internal Methods

            #region Private Methods

            private void AddItem(PackUriHelper.ValidatedPartUri? partUri, string normalizedPrefixName, string zipFileName)
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
            private readonly Dictionary<string, List<string>> _ignoredItemDictionary;

            //using an additional extension dictionary to map an extenstion to
            //different prefix names with the same extension, in order to
            //reduce the string parsing
            private readonly Dictionary<string, List<string>> _extensionDictionary;


            private readonly ZipArchive _zipArchive;

            #endregion Private Member Variables
        }
    }
}
