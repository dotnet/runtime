// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            // Convert Metro CompressionOption to Zip CompressionMethodEnum.
            CompressionLevel level;
            GetZipCompressionMethodFromOpcCompressionOption(compressionOption,
                out level);

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
            partUri = PackUriHelper.ValidatePartUri(partUri);

            string partZipName = GetZipItemNameFromOpcName(PackUriHelper.GetStringForPartUri(partUri));
            ZipArchiveEntry zipArchiveEntry = _zipArchive.GetEntry(partZipName);
            if (zipArchiveEntry != null)
            {
                // Case of an atomic part.
                zipArchiveEntry.Delete();
            }

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
            List<PackagePart> parts = new List<PackagePart>(InitialPartListSize);

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
                    }
                    //If not valid part uri we can completely ignore this zip file item. Even if later someone adds
                    //a new part, the corresponding zip item can never map to one of these items
                }
                // If IsZipItemValidOpcPartOrPiece returns false, it implies that either the zip file Item
                // starts or ends with a "/" and as such we can completely ignore this zip file item. Even if later
                // a new part gets added, its corresponding zip item cannot map to one of these items.
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
            ZipArchive zipArchive = null;
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
                contentTypeHelper = new ContentTypeHelper(zipArchive, _packageFileMode, _packageFileAccess, _zipStreamManager);
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
            ContentTypeHelper contentTypeHelper = null;
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

                zipArchive = new ZipArchive(s, zipArchiveMode, true, Text.Encoding.UTF8);
                
                _zipStreamManager = new ZipStreamManager(zipArchive, packageFileMode, packageFileAccess);
                contentTypeHelper = new ContentTypeHelper(zipArchive, packageFileMode, packageFileAccess, _zipStreamManager);
            }
            catch (InvalidDataException)
            {
                throw new FileFormatException("File contains corrupted data.");
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

        // convert from Zip CompressionMethodEnum and DeflateOptionEnum to Metro CompressionOption 
        private static CompressionOption GetCompressionOptionFromZipFileInfo(ZipArchiveEntry zipFileInfo)
        {
            // Note: we can't determine compression method / level from the ZipArchiveEntry.
            CompressionOption result = CompressionOption.Normal;
            return result;
        }

        #endregion Private Methods
        
        #region Private Members

        private const int InitialPartListSize = 50;
        private const int InitialPieceNameListSize = 50;

        private ZipArchive _zipArchive;
        private Stream _containerStream;      // stream we are opened in if Open(Stream) was called
        private bool _shouldCloseContainerStream;
        private ContentTypeHelper _contentTypeHelper;    // manages the content types for all the parts in the container
        private ZipStreamManager _zipStreamManager;      // manages streams for all parts, avoiding opening streams multiple times
        private FileAccess _packageFileAccess;
        private FileMode _packageFileMode;

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
            bool IEqualityComparer<string>.Equals(string extensionA, string extensionB)
            {
                Debug.Assert(extensionA != null, "extension should not be null");
                Debug.Assert(extensionB != null, "extension should not be null");

                //Important Note: any change to this should be made in accordance 
                //with the rules for comparing/normalizing partnames. 
                //Refer to PackUriHelper.ValidatedPartUri.GetNormalizedPartUri method.
                //Currently normalization just involves upper-casing ASCII and hence the simplification.
                return (string.CompareOrdinal(extensionA.ToUpperInvariant(), extensionB.ToUpperInvariant()) == 0);
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
        private class ContentTypeHelper
        {
            /// <summary>
            /// Initialize the object without uploading any information from the package.
            /// Complete initialization in read mode also involves calling ParseContentTypesFile
            /// to deserialize content type information.
            /// </summary>
            internal ContentTypeHelper(ZipArchive zipArchive, FileMode packageFileMode, FileAccess packageFileAccess, ZipStreamManager zipStreamManager)
            {
                _zipArchive = zipArchive;               //initialized in the ZipPackage constructor
                _packageFileMode = packageFileMode;
                _packageFileAccess = packageFileAccess;
                _zipStreamManager = zipStreamManager;   //initialized in the ZipPackage constructor
                // The extensions are stored in the default Dictionary in their original form , but they are compared
                // in a normalized manner using the ExtensionComparer.
                _defaultDictionary = new Dictionary<string, ContentType>(s_defaultDictionaryInitialSize, s_extensionEqualityComparer);

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
                    return s_contentTypesFile;
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
                    else
                    {
                        // delete and re-create entry for content part.  When writing this, the stream will not truncate the content
                        // if the XML is shorter than the existing content part.
                        var contentTypefullName = _contentTypeZipArchiveEntry.FullName;
                        var thisArchive = _contentTypeZipArchiveEntry.Archive;
                        _zipStreamManager.Close(_contentTypeZipArchiveEntry);
                        _contentTypeZipArchiveEntry.Delete();
                        _contentTypeZipArchiveEntry = thisArchive.CreateEntry(contentTypefullName);
                    }

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
                        && (string.CompareOrdinal(reader.NamespaceURI, s_typesNamespaceUri) == 0)
                        && (string.CompareOrdinal(reader.Name, s_typesTagName) == 0))
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
                                && (string.CompareOrdinal(reader.NamespaceURI, s_typesNamespaceUri) == 0)
                                && (string.CompareOrdinal(reader.Name, s_defaultTagName) == 0))
                            {
                                ProcessDefaultTagAttributes(reader);
                            }
                            else
                                if (reader.NodeType == XmlNodeType.Element
                                    && reader.Depth == 1
                                    && (string.CompareOrdinal(reader.NamespaceURI, s_typesNamespaceUri) == 0)
                                    && (string.CompareOrdinal(reader.Name, s_overrideTagName) == 0))
                                {
                                    ProcessOverrideTagAttributes(reader);
                                }
                                else
                                    if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == 0 && string.CompareOrdinal(reader.Name, s_typesTagName) == 0)
                                        continue;
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
            private Stream OpenContentTypeStream(System.Collections.ObjectModel.ReadOnlyCollection<ZipArchiveEntry> zipFiles)
            {
                foreach (ZipArchiveEntry zipFileInfo in zipFiles)
                {
                    if (zipFileInfo.Name.ToUpperInvariant().StartsWith(s_contentTypesFileUpperInvariant, StringComparison.Ordinal))
                    {
                        // Atomic name.
                        if (zipFileInfo.Name.Length == ContentTypeFileName.Length)
                        {
                            // Record the file info.
                            _contentTypeZipArchiveEntry = zipFileInfo;
                        }
                    }
                }


                // If an atomic file was found, open a stream on it.
                if (_contentTypeZipArchiveEntry != null)
                {
                    _contentTypeStreamExists = true;
                    return _zipStreamManager.Open(_contentTypeZipArchiveEntry, _packageFileMode, FileAccess.ReadWrite);
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
            }

            // Process the attributes for the Default tag
            private void ProcessOverrideTagAttributes(XmlReader reader)
            {
                //There could be a namespace Attribute present at this level. 
                //Also any other attribute on the <Override> tag is an error including xml: and xsi: attributes
                if (PackagingUtilities.GetNonXmlnsAttributeCount(reader) != 2)
                    throw new XmlException(SR.OverrideTagDoesNotMatchSchema, null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);

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
            }

            //If End element is present for Relationship then we process it
            private void ProcessEndElement(XmlReader reader, string elementName)
            {
                Debug.Assert(!reader.IsEmptyElement, "This method should only be called it the Relationship Element is not empty");

                reader.Read();

                //Skips over the following - ProcessingInstruction, DocumentType, Comment, Whitespace, or SignificantWhitespace
                reader.MoveToContent();

                if (reader.NodeType == XmlNodeType.EndElement && string.CompareOrdinal(elementName, reader.LocalName) == 0)
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
                if (attributeValue == string.Empty)
                    throw new XmlException(SR.Format(SR.RequiredAttributeEmpty, tagName, attributeName), null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
            }


            //Validate if the required Content type XML attribute is present
            //Content type of a part can be empty
            private void ThrowIfXmlAttributeMissing(string attributeName, string attributeValue, string tagName, XmlReader reader)
            {
                if (attributeValue == null)
                    throw new XmlException(SR.Format(SR.RequiredAttributeMissing, tagName, attributeName), null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
            }
            
            private Dictionary<PackUriHelper.ValidatedPartUri, ContentType> _overrideDictionary;
            private Dictionary<string, ContentType> _defaultDictionary;
            private ZipArchive _zipArchive;
            private FileMode _packageFileMode;
            private FileAccess _packageFileAccess;
            private ZipStreamManager _zipStreamManager;
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
        }
    }
}
