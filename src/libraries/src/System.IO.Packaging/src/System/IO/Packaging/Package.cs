// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//-----------------------------------------------------------------------------
//
// Description:
//  This is a base abstract class for Package. This is a part of the
//  Packaging Layer.
//
// History:
//  01/03/2004: SarjanaS: Initial creation. [Stubs only]
//  03/01/2004: SarjanaS: Implemented the functionality for all the members.
//  03/17/2004: BruceMac: Initial implementation or PackageRelationship methods
//
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;   // For SortedList<>
using System.Windows;               // For Exception strings - SRID
using System.Diagnostics;
using System.Security;           // For Debug.Assert

namespace System.IO.Packaging
{
    /// <summary>
    /// Abstract Base class for the Package.
    /// This is a part of the Packaging Layer APIs
    /// </summary>
    public abstract class Package : IDisposable
    {
        //------------------------------------------------------
        //
        //  Public Constructors
        //
        //------------------------------------------------------

        #region Protected Constructor

        /// <summary>
        /// Protected constructor for the abstract Base class.
        /// This is the current contract between the subclass and the base class
        /// If we decide some registration mechanism then this might change
        /// </summary>
        /// <param name="openFileAccess"></param>
        /// <exception cref="ArgumentOutOfRangeException">If FileAccess enumeration does not have one of the valid values</exception>
        protected Package(FileAccess openFileAccess)
            : this(openFileAccess, false /* not streaming by default */)
        {
        }

        /// <summary>
        /// Protected constructor for the abstract Base class.
        /// This is the current contract between the subclass and the base class
        /// If we decide some registration mechanism then this might change
        /// </summary>
        /// <param name="openFileAccess"></param>
        /// <param name="streaming">Whether the package is being opened for streaming.</param>
        /// <exception cref="ArgumentOutOfRangeException">If FileAccess enumeration does not have one of the valid values</exception>
        protected Package(FileAccess openFileAccess, bool streaming)
        {
            ThrowIfFileAccessInvalid(openFileAccess);

            _openFileAccess = openFileAccess;

            //PackUriHelper.ValidatedPartUri implements the IComparable interface.
            _partList = new SortedList<PackUriHelper.ValidatedPartUri, PackagePart>(); // initial default is zero
            _partCollection = null;
            _disposed = false;
            _inStreamingCreation = (openFileAccess == FileAccess.Write && streaming);
        }

        #endregion Protected Constructor

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Gets the FileAccess with which the package was opened. This is a read only property.
        /// This property gets set when the package is opened.
        /// </summary>
        /// <value>FileAccess</value>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        public FileAccess FileOpenAccess
        {
            get
            {
                ThrowIfObjectDisposed();
                return _openFileAccess;
            }
        }

        /// <summary>
        /// The package properties are a subset of the standard OLE property sets
        /// SummaryInformation and DocumentSummaryInformation, and include such properties
        /// as Title and Subject.
        /// </summary>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        public PackageProperties PackageProperties
        {
            get
            {
                ThrowIfObjectDisposed();

                if (_packageProperties == null)
                    _packageProperties = new PartBasedPackageProperties(this);
                return _packageProperties;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        #region OpenOnFileMethods

        /// <summary>
        /// Opens a package at the specified Path. This method calls the overload which accepts all the parameters
        /// with the following defaults -
        /// FileMode - FileMode.OpenOrCreate,
        /// FileAccess - FileAccess.ReadWrite
        /// FileShare  - FileShare.None
        /// </summary>
        /// <param name="path">Path to the package</param>
        /// <returns>Package</returns>
        /// <exception cref="ArgumentNullException">If path parameter is null</exception>
        public static Package Open(string path)
        {
            return Open(path, s_defaultFileMode, s_defaultFileAccess, s_defaultFileShare);
        }

        /// <summary>
        /// Opens a package at the specified Path in the given mode. This method calls the overload which
        /// accepts all the parameters with the following defaults -
        /// FileAccess - FileAccess.ReadWrite
        /// FileShare  - FileShare.None
        /// </summary>
        /// <param name="path">Path to the package</param>
        /// <param name="packageMode">FileMode in which the package should be opened</param>
        /// <returns>Package</returns>
        /// <exception cref="ArgumentNullException">If path parameter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If FileMode enumeration [packageMode] does not have one of the valid values</exception>
        public static Package Open(string path, FileMode packageMode)
        {
            return Open(path, packageMode, s_defaultFileAccess, s_defaultFileShare);
        }

        /// <summary>
        /// Opens a package at the specified Path in the given mode with the specified access. This method calls
        /// the overload which accepts all the parameters with the following defaults -
        /// FileShare  - FileShare.None
        /// </summary>
        /// <param name="path">Path to the package</param>
        /// <param name="packageMode">FileMode in which the package should be opened</param>
        /// <param name="packageAccess">FileAccess with which the package should be opened</param>
        /// <returns>Package</returns>
        /// <exception cref="ArgumentNullException">If path parameter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If FileMode enumeration [packageMode] does not have one of the valid values</exception>
        /// <exception cref="ArgumentOutOfRangeException">If FileAccess enumeration [packageAccess] does not have one of the valid values</exception>
        public static Package Open(string path, FileMode packageMode, FileAccess packageAccess)
        {
            return Open(path, packageMode, packageAccess, s_defaultFileShare);
        }

        /// <summary>
        /// Opens the package with the specified parameters.
        ///
        /// Note:-
        /// Since currently there is no plan to implement a generic registration mechanism, this method
        /// has some hard coded knowledge about the sub classes. This might change later if we come up
        /// with a registration mechanism.
        /// There is no caching mechanism in case the FileShare is specified to ReadWrite/Write.
        /// We do not have any refresh mechanism, to make sure that the cached parts reflect the actual parts,
        /// in the case where there might be more than one processes writing to the same underlying package.
        /// </summary>
        /// <param name="path">Path to the package</param>
        /// <param name="packageMode">FileMode in which the package should be opened</param>
        /// <param name="packageAccess">FileAccess with which the package should be opened</param>
        /// <param name="packageShare">FileShare with which the package is opened.</param>
        /// <returns>Package</returns>
        /// <exception>InvalidArgumentException - If the combination of the FileMode,
        /// FileAccess and FileShare parameters is not meaningful.</exception>
        /// <exception cref="ArgumentNullException">If path parameter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If FileMode enumeration [packageMode] does not have one of the valid values</exception>
        /// <exception cref="ArgumentOutOfRangeException">If FileAccess enumeration [packageAccess] does not have one of the valid values</exception>
        public static Package Open(string path, FileMode packageMode, FileAccess packageAccess, FileShare packageShare)
        {
            return Open(path, packageMode, packageAccess, packageShare, false /* not in streaming mode */);
        }

        #endregion OpenOnFileMethods

        #region OpenOnStreamMethods

        /// <summary>
        /// Open a package on this stream. This method calls the overload which accepts all the parameters
        /// with the following defaults -
        /// FileMode - FileMode.Open
        /// FileAccess - FileAccess.Read
        /// </summary>
        /// <param name="stream">Stream on which the package is to be opened</param>
        /// <returns>Package</returns>
        /// <exception cref="ArgumentNullException">If stream parameter is null</exception>
        /// <exception cref="IOException">If package to be created should have readwrite/read access and underlying stream is write only</exception>
        /// <exception cref="IOException">If package to be created should have readwrite/write access and underlying stream is read only</exception>
        public static Package Open(Stream stream)
        {
            return Open(stream, s_defaultStreamMode, s_defaultStreamAccess);
        }

        /// <summary>
        /// Open a package on this stream. This method calls the overload which accepts all the parameters
        /// with the following defaults -
        /// FileAccess - FileAccess.ReadWrite
        /// </summary>
        /// <param name="stream">Stream on which the package is to be opened</param>
        /// <param name="packageMode">FileMode in which the package should be opened.</param>
        /// <returns>Package</returns>
        /// <exception cref="ArgumentNullException">If stream parameter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If FileMode enumeration [packageMode] does not have one of the valid values</exception>
        /// <exception cref="IOException">If package to be created should have readwrite/read access and underlying stream is write only</exception>
        /// <exception cref="IOException">If package to be created should have readwrite/write access and underlying stream is read only</exception>
        public static Package Open(Stream stream, FileMode packageMode)
        {
            //If the user is providing a FileMode, in all the modes, except FileMode.Open,
            //its most likely that the user intends to write to the stream.
            return Open(stream, packageMode, s_defaultFileAccess);
        }

        /// <summary>
        /// Opens a package on this stream. The package is opened in the specified mode and with the access
        /// specified.
        /// </summary>
        /// <param name="stream">Stream on which the package is created</param>
        /// <param name="packageMode">FileMode in which the package is to be opened</param>
        /// <param name="packageAccess">FileAccess on the package that is opened</param>
        /// <returns>Package</returns>
        /// <exception cref="ArgumentNullException">If stream parameter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If FileMode enumeration [packageMode] does not have one of the valid values</exception>
        /// <exception cref="ArgumentOutOfRangeException">If FileAccess enumeration [packageAccess] does not have one of the valid values</exception>
        /// <exception cref="IOException">If package to be created should have readwrite/read access and underlying stream is write only</exception>
        /// <exception cref="IOException">If package to be created should have readwrite/write access and underlying stream is read only</exception>
        public static Package Open(Stream stream, FileMode packageMode, FileAccess packageAccess)
        {
            return Open(stream, packageMode, packageAccess, false /* not in streaming mode */);
        }
        #endregion OpenOnStreamMethods

        #region PackagePart Methods

        /// <summary>
        /// Creates a new part in the package. An empty stream corresponding to this part will be created in the
        /// package. If a part with the specified uri already exists then we throw an exception.
        /// This methods will call the CreatePartCore method which will create the actual PackagePart in the package.
        /// </summary>
        /// <param name="partUri">Uri of the PackagePart that is to be added</param>
        /// <param name="contentType">ContentType of the stream to be added</param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is readonly, it cannot be modified</exception>
        /// <exception cref="ArgumentNullException">If partUri parameter is null</exception>
        /// <exception cref="ArgumentNullException">If contentType parameter is null</exception>
        /// <exception cref="ArgumentException">If partUri parameter does not conform to the valid partUri syntax</exception>
        /// <exception cref="InvalidOperationException">If a PackagePart with the given partUri already exists in the Package</exception>
        public PackagePart CreatePart(Uri partUri, string contentType)
        {
            return CreatePart(partUri, contentType, CompressionOption.NotCompressed);
        }

        /// <summary>
        /// Creates a new part in the package. An empty stream corresponding to this part will be created in the
        /// package. If a part with the specified uri already exists then we throw an exception.
        /// This methods will call the CreatePartCore method which will create the actual PackagePart in the package.
        /// </summary>
        /// <param name="partUri">Uri of the PackagePart that is to be added</param>
        /// <param name="contentType">ContentType of the stream to be added</param>
        /// <param name="compressionOption">CompressionOption  describing compression configuration
        /// for the new part. This compression apply only to the part, it doesn't affect relationship parts or related parts.
        /// This parameter is optional. </param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is readonly, it cannot be modified</exception>
        /// <exception cref="ArgumentNullException">If partUri parameter is null</exception>
        /// <exception cref="ArgumentNullException">If contentType parameter is null</exception>
        /// <exception cref="ArgumentException">If partUri parameter does not conform to the valid partUri syntax</exception>
        /// <exception cref="ArgumentOutOfRangeException">If CompressionOption enumeration [compressionOption] does not have one of the valid values</exception>
        /// <exception cref="InvalidOperationException">If a PackagePart with the given partUri already exists in the Package</exception>
        public PackagePart CreatePart(Uri partUri,
                            string contentType,
                            CompressionOption compressionOption)
        {
            ThrowIfObjectDisposed();
            ThrowIfReadOnly();

            if (partUri == null)
                throw new ArgumentNullException("partUri");

            if (contentType == null)
                throw new ArgumentNullException("contentType");

            ThrowIfCompressionOptionInvalid(compressionOption);

            PackUriHelper.ValidatedPartUri validatedPartUri = PackUriHelper.ValidatePartUri(partUri);

            if (_partList.ContainsKey(validatedPartUri))
                throw new InvalidOperationException(SR.Get(SRID.PartAlreadyExists));

            // Add the part to the _partList if there is no prefix collision
            // Note: This is the only place where we pass a null to this method for the part and if the
            // methods returns successfully then we replace the null with an actual part.
            AddIfNoPrefixCollisionDetected(validatedPartUri, null /* since we don't have a part yet */);

            PackagePart addedPart = CreatePartCore(validatedPartUri,
                                                            contentType,
                                                            compressionOption);

            //Set the entry for this Uri with the actual part
            _partList[validatedPartUri] = addedPart;

            return addedPart;
        }



        /// <summary>
        /// Returns a part that already exists in the package. If the part
        /// Corresponding to the URI does not exist in the package then an exception is
        /// thrown. The method calls the GetPartCore method which actually fetches the part.
        /// </summary>
        /// <param name="partUri"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is write only, information cannot be retrieved from it</exception>
        /// <exception cref="ArgumentNullException">If partUri parameter is null</exception>
        /// <exception cref="ArgumentException">If partUri parameter does not conform to the valid partUri syntax</exception>
        /// <exception cref="InvalidOperationException">If the requested part does not exists in the Package</exception>
        public PackagePart GetPart(Uri partUri)
        {
            PackagePart returnedPart = GetPartHelper(partUri);
            if (returnedPart == null)
                throw new InvalidOperationException(SR.Get(SRID.PartDoesNotExist));
            else
                return returnedPart;
        }


        /// <summary>
        /// This is a convenient method to check whether a given part exists in the
        /// package. This will have a default implementation that will try to retrieve
        /// the part and then if successful, it will return true.
        /// If the custom file format has an easier way to do this, they can override this method
        /// to get this information in a more efficient way.
        /// </summary>
        /// <param name="partUri"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is write only, information cannot be retrieved from it</exception>
        /// <exception cref="ArgumentNullException">If partUri parameter is null</exception>
        /// <exception cref="ArgumentException">If partUri parameter does not conform to the valid partUri syntax</exception>
        public virtual bool PartExists(Uri partUri)
        {
            return (GetPartHelper(partUri) != null);
        }


        /// <summary>
        /// This method will do all the house keeping required when a part is deleted
        /// Then the DeletePartCore method will be called which will have the actual logic to
        /// do the work specific to the underlying file format and will actually delete the
        /// stream corresponding to this part. This method does not throw if the specified
        /// part does not exist. This is in conformance with the FileInfo.Delete call.
        /// </summary>
        /// <param name="partUri"></param>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is readonly, it cannot be modified</exception>
        /// <exception cref="ArgumentNullException">If partUri parameter is null</exception>
        /// <exception cref="ArgumentException">If partUri parameter does not conform to the valid partUri syntax</exception>
        public void DeletePart(Uri partUri)
        {
            ThrowIfObjectDisposed();
            ThrowIfReadOnly();
            ThrowIfInStreamingCreation("DeletePart");

            if (partUri == null)
                throw new ArgumentNullException("partUri");

            PackUriHelper.ValidatedPartUri validatedPartUri = (PackUriHelper.ValidatedPartUri)PackUriHelper.ValidatePartUri(partUri);

            if (_partList.ContainsKey(validatedPartUri))
            {
                //This will get the actual casing of the part that
                //is stored in the partList which is equivalent to the
                //partUri provided by the user
                validatedPartUri = (PackUriHelper.ValidatedPartUri)_partList[validatedPartUri].Uri;
                _partList[validatedPartUri].IsDeleted = true;
                _partList[validatedPartUri].Close();

                //Call the Subclass to delete the part

                //!!Important Note: The order of this call is important as one of the
                //sub-classes - ZipPackage relies upon the abstract layer to be
                //able to provide the ZipPackagePart in order to do the proper
                //clean up and delete operation.
                //The dependency is in ZipPackagePart.DeletePartCore method.
                //Ideally we would have liked to avoid this kind of a restriction
                //but due to the current class interfaces and data structure ownerships
                //between these objects, it tough to re-design at this point.
                DeletePartCore(validatedPartUri);

                //Finally remove it from the list of parts in the cache
                _partList.Remove(validatedPartUri);
            }
            else
                //If the part is not in memory we still call the underlying layer
                //to delete the part if it exists
                DeletePartCore(validatedPartUri);

            if (PackUriHelper.IsRelationshipPartUri(validatedPartUri))
            {
                //We clear the in-memory data structure corresponding to that relationship part
                //This will ensure that the intention of the user to delete the part, is respected.
                //And thus we will not try to recreate it just in case there was some data in the
                //memory structure.

                Uri owningPartUri = PackUriHelper.GetSourcePartUriFromRelationshipPartUri(validatedPartUri);
                //Package-level relationships in /_rels/.rels
                if (Uri.Compare(owningPartUri, PackUriHelper.PackageRootUri, UriComponents.SerializationInfoString, UriFormat.UriEscaped, StringComparison.Ordinal) == 0)
                {
                    //Clear any data in memory
                    this.ClearRelationships();
                }
                else
                {
                    //Clear any data in memory
                    if (this.PartExists(owningPartUri))
                    {
                        PackagePart owningPart = this.GetPart(owningPartUri);
                        owningPart.ClearRelationships();
                    }
                }
            }
            else
            {
                // remove any relationship part
                DeletePart(PackUriHelper.GetRelationshipPartUri(validatedPartUri));
            }
        }

        /// <summary>
        /// This returns a collection of all the Parts within the package.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is writeonly, no information can be retrieved from it</exception>
        public PackagePartCollection GetParts()
        {
            ThrowIfObjectDisposed();
            ThrowIfWriteOnly();

            //Ideally we should decide whether we should query the underlying layer for parts based on the
            //FileShare enum. But since we do not have that information, currently the design is to just
            //query the underlying layer once.
            //Note:
            //Currently the incremental behavior for GetPart method is not consistent with the GetParts method
            //which just queries the underlying layer once.
            if (_partCollection == null)
            {
                PackagePart[] parts = GetPartsCore();

                //making sure that we get a valid array
                Debug.Assert((parts != null),
                    "Subclass is expected to return an array [an empty one if there are no parts] as a result of GetPartsCore method call. ");

                PackUriHelper.ValidatedPartUri partUri;

                //We need this dictionary to detect any collisions that might be present in the
                //list of parts that was given to us from the underlying physical layer, as more than one
                //partnames can be mapped to the same normalized part.
                //Note: We cannot use the _partList member variable, as that gets updated incrementally and so its
                //not possible to find the collisions using that list.
                //PackUriHelper.ValidatedPartUri implements the IComparable interface.
                Dictionary<PackUriHelper.ValidatedPartUri, PackagePart> seenPartUris = new Dictionary<PackUriHelper.ValidatedPartUri, PackagePart>(parts.Length);

                for (int i = 0; i < parts.Length; i++)
                {
                    partUri = (PackUriHelper.ValidatedPartUri)parts[i].Uri;

                    if (seenPartUris.ContainsKey(partUri))
                        throw new FileFormatException(SR.Get(SRID.BadPackageFormat));
                    else
                    {
                        // Add the part to the list of URIs that we have already seen
                        seenPartUris.Add(partUri, parts[i]);

                        if (!_partList.ContainsKey(partUri))
                        {
                            // Add the part to the _partList if there is no prefix collision
                            AddIfNoPrefixCollisionDetected(partUri, parts[i]);
                        }
                    }
                }
                _partCollection = new PackagePartCollection(_partList);
            }
            return _partCollection;
        }

        #endregion PackagePart Methods

        #region IDisposable Methods

        /// <summary>
        /// Member of the IDisposable interface. This method will clean up all the resources.
        /// It calls the Flush method to make sure that all the changes made get persisted.
        /// Note - subclasses should only override Dispose(bool) if they have resources to release.
        /// See the Design Guidelines for the Dispose() pattern.
        /// </summary>
        [SecurityCritical]
        void IDisposable.Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    // put our house in order before involving the subclass

                    // close core properties
                    // This method will write out the core properties to the stream
                    // In non-streaming mode - These will get flushed to the disk as a part of the DoFlush operation
                    if (_packageProperties != null)
                        _packageProperties.Close();

                    // flush relationships
                    if (InStreamingCreation)
                        ClosePackageRelationships();
                    else
                        FlushRelationships();

                    //Write out the Relationship XML for the parts
                    //These streams will get flushed in the DoClose operation.
                    DoOperationOnEachPart(DoCloseRelationshipsXml);

                    // Close all the parts that are currently open
                    DoOperationOnEachPart(DoClose);

                    Dispose(true);
                }
                finally
                {
                    // do this no matter what (handles case of poorly behaving subclass that doesn't call back into Dispose(bool)
                    _disposed = true;
                }

                //Since all the resources we care about are freed at this point.
                GC.SuppressFinalize(this);
            }
        }

        #endregion IDisposable Methods

        #region Other Methods

        /// <summary>
        /// Closes the package and all the underlying parts and relationships.
        /// Calls the Dispose Method, since they have the same semantics
        /// </summary>
        public void Close()
        {
            ((IDisposable)this).Dispose();
        }

        /// <summary>
        /// Flushes the contents of the parts and the relationships to the package.
        /// This method will call the FlushCore method which will do the actual flushing of contents.
        /// </summary>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is readonly, it cannot be modified</exception>
        public void Flush()
        {
            ThrowIfObjectDisposed();
            ThrowIfReadOnly();

            // Flush core properties (in streaming production, has to be done before parts get flushed).
            // Write core properties (in streaming production, has to be done before parts get flushed).
            // This call will write out the xml for the core properties to the stream
            // In non-streaming mode - These properties will get flushed to disk as a part of the DoFlush operation
            if (_packageProperties != null)
                _packageProperties.Flush();

            // Write package relationships XML to the relationship part stream.
            // These will get flushed to disk as a part of the DoFlush operation
            if (InStreamingCreation)
                FlushPackageRelationships(); // Create a piece.
            else
                FlushRelationships(); // Flush into .rels part.

            //Write out the Relationship XML for the parts
            //These streams will get flushed in the DoFlush operation.
            DoOperationOnEachPart(DoWriteRelationshipsXml);

            // Flush all the parts that are currently open.
            // This will flush part relationships.
            DoOperationOnEachPart(DoFlush);

            FlushCore();
        }

        #endregion Other Methods

        #region PackageRelationship Methods

        /// <summary>
        /// Creates a relationship at the Package level with the Target PackagePart specified as the Uri
        /// </summary>
        /// <param name="targetUri">Target's URI</param>
        /// <param name="targetMode">Enumeration indicating the base uri for the target uri</param>
        /// <param name="relationshipType">PackageRelationship type, having uri like syntax that is used to
        /// uniquely identify the role of the relationship</param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is readonly, it cannot be modified</exception>
        /// <exception cref="ArgumentNullException">If parameter "targetUri" is null</exception>
        /// <exception cref="ArgumentNullException">If parameter "relationshipType" is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If parameter "targetMode" enumeration does not have a valid value</exception>
        /// <exception cref="ArgumentException">If TargetMode is TargetMode.Internal and the targetUri is an absolute Uri </exception>
        /// <exception cref="ArgumentException">If relationship is being targeted to a relationship part</exception>
        public PackageRelationship CreateRelationship(Uri targetUri, TargetMode targetMode, string relationshipType)
        {
            return CreateRelationship(targetUri, targetMode, relationshipType, null);
        }

        /// <summary>
        /// Creates a relationship at the Package level with the Target PackagePart specified as the Uri
        /// </summary>
        /// <param name="targetUri">Target's URI</param>
        /// <param name="targetMode">Enumeration indicating the base uri for the target uri</param>
        /// <param name="relationshipType">PackageRelationship type, having uri like syntax that is used to
        /// uniquely identify the role of the relationship</param>
        /// <param name="id">String that conforms to the xsd:ID datatype. Unique across the source's
        /// relationships. Null is OK (ID will be generated). An empty string is an invalid XML ID.</param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is readonly, it cannot be modified</exception>
        /// <exception cref="ArgumentNullException">If parameter "targetUri" is null</exception>
        /// <exception cref="ArgumentNullException">If parameter "relationshipType" is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If parameter "targetMode" enumeration does not have a valid value</exception>
        /// <exception cref="ArgumentException">If TargetMode is TargetMode.Internal and the targetUri is an absolute Uri </exception>
        /// <exception cref="ArgumentException">If relationship is being targeted to a relationship part</exception>
        /// <exception cref="System.Xml.XmlException">If parameter "id" is not a valid Xsd Id</exception>
        /// <exception cref="System.Xml.XmlException">If an id is provided in the method, and its not unique</exception>
        public PackageRelationship CreateRelationship(Uri targetUri, TargetMode targetMode, string relationshipType, String id)
        {
            ThrowIfObjectDisposed();
            ThrowIfReadOnly();
            EnsureRelationships();
            //All parameter validation is done in the following call
            return _relationships.Add(targetUri, targetMode, relationshipType, id);
        }

        /// <summary>
        /// Deletes a relationship from the Package. This is done based on the
        /// relationship's ID. The target PackagePart is not affected by this operation.
        /// </summary>
        /// <param name="id">The ID of the relationship to delete. An invalid ID will not
        /// throw an exception, but nothing will be deleted.</param>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is readonly, it cannot be modified</exception>
        /// <exception cref="ArgumentNullException">If parameter "id" is null</exception>
        /// <exception cref="System.Xml.XmlException">If parameter "id" is not a valid Xsd Id</exception>
        public void DeleteRelationship(String id)
        {
            ThrowIfObjectDisposed();
            ThrowIfReadOnly();
            ThrowIfInStreamingCreation("DeleteRelationship");

            if (id == null)
                throw new ArgumentNullException("id");

            InternalRelationshipCollection.ThrowIfInvalidXsdId(id);

            EnsureRelationships();
            _relationships.Delete(id);
        }

        /// <summary>
        /// Returns a collection of all the Relationships that are
        /// owned by the package
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is write only, no information can be retrieved from it</exception>
        public PackageRelationshipCollection GetRelationships()
        {
            //All the validations for dispose and file access are done in the
            //GetRelationshipsHelper method.
            return GetRelationshipsHelper(null);
        }

        /// <summary>
        /// Returns a collection of filtered Relationships that are
        /// owned by the package
        /// The filter string is compared with the type of the relationships
        /// in a case sensitive and culture ignorant manner.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is write only, no information can be retrieved from it</exception>
        /// <exception cref="ArgumentNullException">If parameter "relationshipType" is null</exception>
        /// <exception cref="ArgumentException">If parameter "relationshipType" is an empty string</exception>
        public PackageRelationshipCollection GetRelationshipsByType(string relationshipType)
        {
            //These checks are made in the GetRelationshipsHelper as well, but we make them
            //here as we need to perform parameter validation
            ThrowIfObjectDisposed();
            ThrowIfWriteOnly();

            if (relationshipType == null)
                throw new ArgumentNullException("relationshipType");

            InternalRelationshipCollection.ThrowIfInvalidRelationshipType(relationshipType);

            return GetRelationshipsHelper(relationshipType);
        }

        /// <summary>
        /// Retrieve a relationship per ID.
        /// </summary>
        /// <param name="id">The relationship ID.</param>
        /// <returns>The relationship with ID 'id' or throw an exception if not found.</returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is write only, no information can be retrieved from it</exception>
        /// <exception cref="ArgumentNullException">If parameter "id" is null</exception>
        /// <exception cref="System.Xml.XmlException">If parameter "id" is not a valid Xsd Id</exception>
        /// <exception cref="InvalidOperationException">If the requested relationship does not exist in the Package</exception>
        public PackageRelationship GetRelationship(string id)
        {
            //All the validations for dispose and file access are done in the
            //GetRelationshipHelper method.

            PackageRelationship returnedRelationship = GetRelationshipHelper(id);
            if (returnedRelationship == null)
                throw new InvalidOperationException(SR.Get(SRID.PackageRelationshipDoesNotExist));
            else
                return returnedRelationship;
        }

        /// <summary>
        /// Returns whether there is a relationship with the specified ID.
        /// </summary>
        /// <param name="id">The relationship ID.</param>
        /// <returns>true iff a relationship with ID 'id' is defined on this source.</returns>
        /// <exception cref="ObjectDisposedException">If this Package object has been disposed</exception>
        /// <exception cref="IOException">If the package is write only, no information can be retrieved from it</exception>
        /// <exception cref="ArgumentNullException">If parameter "id" is null</exception>
        /// <exception cref="System.Xml.XmlException">If parameter "id" is not a valid Xsd Id</exception>
        public bool RelationshipExists(string id)
        {
            //All the validations for dispose and file access are done in the
            //GetRelationshipHelper method.

            return (GetRelationshipHelper(id) != null);
        }

        #endregion PackageRelationship Methods

        #endregion Public Methods

        #region Protected Abstract Methods

        /// <summary>
        /// This method is for custom implementation corresponding to the underlying file format.
        /// This method will actually add a new part to the package. An empty part should be
        /// created as a result of this call.
        /// </summary>
        /// <param name="partUri"></param>
        /// <param name="contentType"></param>
        /// <param name="compressionOption"></param>
        /// <returns></returns>
        protected abstract PackagePart CreatePartCore(Uri partUri,
                                                            string contentType,
                                                            CompressionOption compressionOption);

        /// <summary>
        /// This method is for custom implementation corresponding to the underlying file format.
        /// This method will actually return the part after reading the actual physical bits.
        /// If the PackagePart does not exists in the underlying package then this method should return a null.
        /// This method must not throw an exception if a part does not exist.
        /// </summary>
        /// <param name="partUri"></param>
        /// <returns></returns>
        protected abstract PackagePart GetPartCore(Uri partUri);

        /// <summary>
        /// This method is for custom implementation corresponding to the underlying file format.
        /// This method will actually delete the part from the underlying package.
        /// This method should not throw if the specified part does not exist.
        /// This is in conformance with the FileInfo.Delete call.
        /// </summary>
        /// <param name="partUri"></param>
        protected abstract void DeletePartCore(Uri partUri);

        /// <summary>
        /// This method is for custom implementation corresponding to the underlying file format.
        /// This is the method that knows how to get the actual parts. If there are no parts,
        /// this method should return an empty array.
        /// </summary>
        /// <returns></returns>
        protected abstract PackagePart[] GetPartsCore();

        /// <summary>
        /// This method is for custom implementation corresponding to the underlying file format.
        /// This method should be used to dispose the resources that are specific to the file format.
        /// Also everything should be flushed to the disc before closing the package.
        /// </summary>
        /// <remarks>Subclasses that manage non-memory resources should override this method and free these resources.
        /// Any override should be careful to always call base.Dispose(disposing) to ensure orderly cleanup.</remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_partList != null)
                {
                    _partList.Clear();
                }

                if (_packageProperties != null)
                {
                    _packageProperties.Dispose();
                    _packageProperties = null;
                }

                //release objects
                _partList = null;
                _partCollection = null;
                _relationships = null;

                _disposed = true;
            }
        }

        /// <summary>
        /// This method is for custom implementation corresponding to the underlying file format.
        /// This method flushes the contents of the package to the disc.
        /// </summary>
        protected abstract void FlushCore();

        #endregion Protected Abstract Methods

        //------------------------------------------------------
        //
        //  Internal Constructors
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //   Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// true iff the package was opened for streaming.
        /// </summary>
        internal bool InStreamingCreation
        {
            get
            {
                return _inStreamingCreation;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods
        // Some operations are not supported while producing a package in streaming mode.
        internal void ThrowIfInStreamingCreation(string methodName)
        {
            if (_inStreamingCreation)
                throw new IOException(SR.Get(SRID.OperationIsNotSupportedInStreamingProduction, methodName));
        }

        // Some operations are supported only while producing a package in streaming mode.
        internal void ThrowIfNotInStreamingCreation(string methodName)
        {
            if (!InStreamingCreation)
                throw new IOException(SR.Get(SRID.MethodAvailableOnlyInStreamingCreation, methodName));
        }

        //If the container is readonly then we cannot add/delete to it
        internal void ThrowIfReadOnly()
        {
            if (_openFileAccess == FileAccess.Read)
                throw new IOException(SR.Get(SRID.CannotModifyReadOnlyContainer));
        }

        // If the container is writeonly, parts cannot be retrieved from it
        internal void ThrowIfWriteOnly()
        {
            if (_openFileAccess == FileAccess.Write)
                throw new IOException(SR.Get(SRID.CannotRetrievePartsOfWriteOnlyContainer));
        }

        // return true to continue
        internal delegate bool PartOperation(PackagePart p);

        internal static void ThrowIfFileModeInvalid(FileMode mode)
        {
            //We do the enum check as suggested by the following condition for performance reasons.
            if (mode < FileMode.CreateNew || mode > FileMode.Append)
                throw new ArgumentOutOfRangeException("mode");
        }

        internal static void ThrowIfFileAccessInvalid(FileAccess access)
        {
            //We do the enum check as suggested by the following condition for performance reasons.
            if (access < FileAccess.Read || access > FileAccess.ReadWrite)
                throw new ArgumentOutOfRangeException("access");
        }

        internal static void ThrowIfCompressionOptionInvalid(CompressionOption compressionOption)
        {
            //We do the enum check as suggested by the following condition for performance reasons.
            if (compressionOption < CompressionOption.NotCompressed || compressionOption > CompressionOption.SuperFast)
                throw new ArgumentOutOfRangeException("compressionOption");
        }

        #region Write-time streaming API

        /// <summary>
        /// This method gives the possibility of opening a package in streaming mode.
        /// When 'streaming' is true, the only allowed file modes are Create and CreateNew,
        /// the only allowed file access is Write and the only allowed FileShare values are
        /// Null and Read.
        /// </summary>
        /// <param name="path">Path to the package.</param>
        /// <param name="packageMode">FileMode in which the package should be opened.</param>
        /// <param name="packageAccess">FileAccess with which the package should be opened.</param>
        /// <param name="packageShare">FileShare with which the package is opened.</param>
        /// <param name="streaming">Whether to allow the creation of part pieces while enforcing write-once access.</param>
        /// <returns>Package</returns>
        /// <exception cref="ArgumentNullException">If path parameter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If FileAccess enumeration [packageAccess] does not have one of the valid values</exception>
        /// <exception cref="ArgumentOutOfRangeException">If FileMode enumeration [packageMode] does not have one of the valid values</exception>
        internal static Package Open(
            string path,
            FileMode packageMode,
            FileAccess packageAccess,
            FileShare packageShare,
            bool streaming)
        {
            Package package = null;
            try
            {
                if (path == null)
                    throw new ArgumentNullException("path");

                ThrowIfFileModeInvalid(packageMode);
                ThrowIfFileAccessInvalid(packageAccess);

#if false
                // todo ew no streaming
                ValidateStreamingAccess(packageMode, packageAccess, packageShare, streaming);
#endif

                //Note: FileShare enum is not being verfied at this stage, as we do not interpret the flag in this
                //code at all and just pass it on to the next layer, where the necessary validation can be
                //performed. Also, there is no meaningful way to check this parameter at this layer, as the
                //FileShare enumeration is a set of flags and flags/Bit-fields can be combined using a
                //bitwise OR operation to create different values, and validity of these values is specific to
                //the actual physical implementation.

                //Verify if this is valid for filenames
                FileInfo packageFileInfo = new FileInfo(path);

                try
                {
                    package = new ZipPackage(packageFileInfo.FullName, packageMode, packageAccess, packageShare, streaming);

#if false
                    // todo ew no streaming
                    if (!package._inStreamingCreation) // No read operation in streaming production.
                    {
#endif
                        //We need to get all the parts if any exists from the underlying file
                        //so that we have the names in the Normalized form in our in-memory
                        //data structures.
                        //Note: If ever this call is removed, each individual call to GetPartCore,
                        //may result in undefined behavior as the underlying ZipArchive, maintains the
                        //files list as being case-sensitive.
                        if (package.FileOpenAccess == FileAccess.ReadWrite || package.FileOpenAccess == FileAccess.Read)
                            package.GetParts();
#if false
                    // todo ew no streaming
                    }
#endif
                }
                catch
                {
                    if (package != null)
                    {
                        package.Close();
                    }

                    throw;
                }
            }
            finally
            {
            }
            return package;
        }

        /// <summary>
        /// This method gives the possibility of opening a package in streaming mode.
        /// When 'streaming' is true, the only allowed file modes are Create and CreateNew,
        /// and the only allowed file access is Write.
        /// </summary>
        /// <param name="stream">Stream on which the package is created</param>
        /// <param name="packageMode">FileMode in which the package is to be opened</param>
        /// <param name="packageAccess">FileAccess on the package that is opened</param>
        /// <param name="streaming">Whether to allow the creation of part pieces while enforcing write-once access.</param>
        /// <returns>Package</returns>
        /// <exception cref="ArgumentNullException">If stream parameter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If FileMode enumeration [packageMode] does not have one of the valid values</exception>
        /// <exception cref="ArgumentOutOfRangeException">If FileAccess enumeration [packageAccess] does not have one of the valid values</exception>
        /// <exception cref="IOException">If package to be created should have readwrite/read access and underlying stream is write only</exception>
        /// <exception cref="IOException">If package to be created should have readwrite/write access and underlying stream is read only</exception>
        internal static Package Open(Stream stream, FileMode packageMode, FileAccess packageAccess, bool streaming)
        {
            Package package = null;
            try
            {
                if (stream == null)
                    throw new ArgumentNullException("stream");

#if false
                // todo ew no streaming
                ValidateStreamingAccess(packageMode, packageAccess, null /* no FileShare info */, streaming);
#endif

                //FileMode and FileAccess Enums are validated in the following call
                Stream ensuredStream = ValidateModeAndAccess(stream, packageMode, packageAccess);


                try
                {
                    // Today the Open(Stream) method is purely used for streams of Zip file format as
                    // that is the default underlying file format mapper implemented.
                    package = new ZipPackage(ensuredStream, packageMode, packageAccess, streaming);

                    if (!package._inStreamingCreation) // No read operation in streaming production.
                    {
                        //We need to get all the parts if any exists from the underlying file
                        //so that we have the names in the Normalized form in our in-memory
                        //data structures.
                        //Note: If ever this call is removed, each individual call to GetPartCore,
                        //may result in undefined behavior as the underlying ZipArchive, maintains the
                        //files list as being case-sensitive.
                        if (package.FileOpenAccess == FileAccess.ReadWrite || package.FileOpenAccess == FileAccess.Read)
                            package.GetParts();
                    }
                }
                catch
                {
                    if (package != null)
                    {
                        package.Close();
                    }

                    throw;
                }
            }
            finally
            {
            }

            return package;
        }

        /// <summary>
        /// Write a nonterminal piece for /_rels/.rels.
        /// </summary>
        internal void FlushPackageRelationships()
        {
            ThrowIfNotInStreamingCreation("FlushPackageRelationships");

            if (_relationships == null)
                return; // nothing to flush

            _relationships.Flush();
        }

        /// <summary>
        /// Write a terminal piece for /_rels/.rels.
        /// </summary>
        internal void ClosePackageRelationships()
        {
            ThrowIfNotInStreamingCreation("ClosePackageRelationships");

            if (_relationships == null)
                return; // no relationship part

            _relationships.CloseInStreamingCreationMode();
        }

        #endregion Write-time streaming API

        #endregion Internal Methods

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


        // This method is only when new part is added to the Package object.
        // This method will throw an exception if the name of the part being added is a
        // prefix of the name of an existing part.
        // Example - Say the following parts exist in the package
        // 1. /abc.xaml
        // 2. /xyz/pqr/a.jpg
        // As an example - Adding any of the following parts will throw an exception -
        // 1. /abc.xaml/new.xaml
        // 2. /xyz/pqr
        private void AddIfNoPrefixCollisionDetected(PackUriHelper.ValidatedPartUri partUri, PackagePart part)
        {
            //Add the Normalized Uri to the sorted _partList tentatively to see where it will get inserted
            _partList.Add(partUri, part);

            //Get the index of the entry at which this part was added
            int index = _partList.IndexOfKey(partUri);

            Invariant.Assert(index >= 0, "Given uri must be present in the dictionary");

            string normalizedPartName = partUri.NormalizedPartUriString;
            string precedingPartName = null;
            string followingPartName = null;

            if (index > 0)
            {
                precedingPartName = _partList.Keys[index - 1].NormalizedPartUriString;
            }

            if (index < _partList.Count - 1)
            {
                followingPartName = _partList.Keys[index + 1].NormalizedPartUriString;
            }

            if ((precedingPartName != null
                && normalizedPartName.StartsWith(precedingPartName, StringComparison.Ordinal)
                && normalizedPartName.Length > precedingPartName.Length
                && normalizedPartName[precedingPartName.Length] == PackUriHelper.ForwardSlashChar) ||
                (followingPartName != null
                && followingPartName.StartsWith(normalizedPartName, StringComparison.Ordinal)
                && followingPartName.Length > normalizedPartName.Length
                && followingPartName[normalizedPartName.Length] == PackUriHelper.ForwardSlashChar))
            {
                //Removing the invalid entry from the _partList.
                _partList.Remove(partUri);

                throw new InvalidOperationException(SR.Get(SRID.PartNamePrefixExists));
            }
        }

#if false
        // Test consistency of file opening parameters with the value of 'streaming', and record
        // whether the streaming mode is for consumption or production.
        // 
        private static void ValidateStreamingAccess(
            FileMode packageMode,
            FileAccess packageAccess,
            Nullable<FileShare> packageShare,
            bool streaming)
        {
            if (streaming)
            {
                if (packageMode == FileMode.Create || packageMode == FileMode.CreateNew)
                {
                    if (packageAccess != FileAccess.Write)
                        throw new IOException(SR.Get(SRID.StreamingPackageProductionImpliesWriteOnlyAccess));
                    if (packageShare != null
                        && packageShare != FileShare.Read && packageShare != FileShare.None)
                        throw new IOException(SR.Get(SRID.StreamingPackageProductionRequiresSingleWriter));
                }
                else
                {
                    // Blanket exception pending design of streaming consumption.
                    throw new NotSupportedException(SR.Get(SRID.StreamingModeNotSupportedForConsumption));
                }
            }
        }
#endif

        //Checking if the mode and access parameters are compatible with the provided stream.
        private static Stream ValidateModeAndAccess(Stream s, FileMode mode, FileAccess access)
        {
            ThrowIfFileModeInvalid(mode);
            ThrowIfFileAccessInvalid(access);

            //asking for more permissions than the underlying stream.
            // Stream cannot write, but package to be created should have write access
            if (!s.CanWrite && (access == FileAccess.ReadWrite || access == FileAccess.Write))
                throw new IOException(SR.Get(SRID.IncompatibleModeOrAccess));

            //asking for more permissions than the underlying stream.
            // Stream cannot read, but the package to be created should have read access
            if (!s.CanRead && (access == FileAccess.ReadWrite || access == FileAccess.Read))
                throw new IOException(SR.Get(SRID.IncompatibleModeOrAccess));

            //asking for less restricted access to the underlying stream
            //stream is ReadWrite but the package is either readonly, or writeonly
            if ((s.CanRead && s.CanWrite) && (access == FileAccess.Read || access == FileAccess.Write))
            {
                return new RestrictedStream(s, access);
            }
            else
                return s;
        }

        //Throw if the object is in a disposed state
        [SecurityCritical]
        private void ThrowIfObjectDisposed()
        {
            if (_disposed == true)
                throw new ObjectDisposedException(null, SR.Get(SRID.ObjectDisposed));
        }

        private void EnsureRelationships()
        {
            // once per package
            if (_relationships == null)
            {
                _relationships = new InternalRelationshipCollection(this);
            }
        }

        //Delete All Package-level Relationships
        private void ClearRelationships()
        {
            if (_relationships != null)
                _relationships.Clear();
        }

        //Flush the relationships at package level
        private void FlushRelationships()
        {
            // flush relationships
            if (_relationships != null && _openFileAccess != FileAccess.Read)
            {
                _relationships.Flush();
            }
        }

        //We do the close or the flush operation per part
        private void DoOperationOnEachPart(PartOperation operation)
        {
            //foreach (PackagePart p in _partList.Values)
            //    p.Close();  - this throws
            // Make local copy of part names to prevent exception during enumeration when
            // a new relationship part gets created (flushing relationships can cause part creation).
            // This code throws in such a case:
            //
            //            foreach (PackagePart p in _partList.Values)
            //                p.Flush();
            //
            if (_partList.Count > 0)
            {
                int partCount = 0;
                PackUriHelper.ValidatedPartUri[] partKeys = new PackUriHelper.ValidatedPartUri[_partList.Keys.Count];

                foreach (PackUriHelper.ValidatedPartUri uri in _partList.Keys)
                {
                    partKeys[partCount++] = uri;
                }

                // this throws an exception in certain cases (when a part has been deleted)
                //
                //     _partList.Keys.CopyTo(keys, 0);

                for (int i = 0; i < _partList.Keys.Count; i++)
                {
                    // Some of these may disappear during above close because the list contains "relationship parts"
                    // and these are removed if their parts' relationship collection is empty
                    // This fails:
                    //                _partList[keys[i]].Flush();

                    PackagePart p;
                    if (_partList.TryGetValue(partKeys[i], out p))
                    {
                        if (!operation(p))
                            break;
                    }
                }
            }
        }

        //We needed to separate the rels parts from the other parts
        //because if a rels part for a part occured earlier than the part itself in the array,
        //the rels part would be closed and then when close the part and try to persist the relationships
        //for the particular part, it would throw an exception
        private bool DoClose(PackagePart p)
        {
            if (!p.IsClosed)
            {
                if (PackUriHelper.IsRelationshipPartUri(p.Uri) && PackUriHelper.ComparePartUri(p.Uri, PackageRelationship.ContainerRelationshipPartName) != 0)
                {
                    //First we close the source part.
                    //Note - we can safely do this as DoClose is being called on all parts. So ultimately we will end up
                    //closing the source part as well.
                    //This logic only takes care of out of order parts.
                    PackUriHelper.ValidatedPartUri owningPartUri =
                        (PackUriHelper.ValidatedPartUri)PackUriHelper.GetSourcePartUriFromRelationshipPartUri(p.Uri);
                    //If the source part for this rels part exists then we close it.
                    PackagePart sourcePart;
                    if (_partList.TryGetValue(owningPartUri, out sourcePart))
                        sourcePart.Close();
                }
                p.Close();
            }
            return true;
        }

        private bool DoFlush(PackagePart p)
        {
            p.Flush();
            return true;
        }

        private bool DoWriteRelationshipsXml(PackagePart p)
        {
            if (!p.IsRelationshipPart)
            {
                p.FlushRelationships();
            }
            return true;
        }

        private bool DoCloseRelationshipsXml(PackagePart p)
        {
            if (!p.IsRelationshipPart)
            {
                p.CloseRelationships();
            }
            return true;
        }

        private PackagePart GetPartHelper(Uri partUri)
        {
            ThrowIfObjectDisposed();
            ThrowIfWriteOnly();

            if (partUri == null)
                throw new ArgumentNullException("partUri");

            PackUriHelper.ValidatedPartUri validatePartUri = PackUriHelper.ValidatePartUri(partUri);

            if (_partList.ContainsKey(validatePartUri))
                return _partList[validatePartUri];
            else
            {
                //Ideally we should decide whether we should query the underlying layer for the part based on the
                //FileShare enum. But since we do not have that information, currently the design is to always
                //ask the underlying layer, this allows for incremental access to the package.
                //Note:
                //Currently this incremental behavior for GetPart is not consistent with the GetParts method
                //which just queries the underlying layer once.
                PackagePart returnedPart = GetPartCore(validatePartUri);

                if (returnedPart != null)
                {
                    // Add the part to the _partList if there is no prefix collision
                    AddIfNoPrefixCollisionDetected(validatePartUri, returnedPart);
                }

                return returnedPart;
            }
        }

        /// <summary>
        /// Retrieve a relationship per ID.
        /// </summary>
        /// <param name="id">The relationship ID.</param>
        /// <returns>The relationship with ID 'id' or null if not found.</returns>
        private PackageRelationship GetRelationshipHelper(string id)
        {
            ThrowIfObjectDisposed();
            ThrowIfWriteOnly();

            if (id == null)
                throw new ArgumentNullException("id");

            InternalRelationshipCollection.ThrowIfInvalidXsdId(id);

            EnsureRelationships();
            return _relationships.GetRelationship(id);
        }

        /// <summary>
        /// Returns a collection of all the Relationships that are
        /// owned by the package based on the filter string.
        /// </summary>
        /// <returns></returns>
        private PackageRelationshipCollection GetRelationshipsHelper(string filterString)
        {
            ThrowIfObjectDisposed();
            ThrowIfWriteOnly();
            EnsureRelationships();

            //Internally null is used to indicate that no filter string was specified and
            //and all the relationships should be returned.
            return new PackageRelationshipCollection(_relationships, filterString);
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Members

        // Default values for the Package.Open method overloads
        private static readonly FileMode s_defaultFileMode = FileMode.OpenOrCreate;
        private static readonly FileAccess s_defaultFileAccess = FileAccess.ReadWrite;
        private static readonly FileShare s_defaultFileShare = FileShare.None;

        private static readonly FileMode s_defaultStreamMode = FileMode.Open;
        private static readonly FileAccess s_defaultStreamAccess = FileAccess.Read;

        private bool _inStreamingCreation;       // false by default
        private FileAccess _openFileAccess;
        private bool _disposed;
        private SortedList<PackUriHelper.ValidatedPartUri, PackagePart> _partList;
        private PackagePartCollection _partCollection;
        private InternalRelationshipCollection _relationships;
        private PartBasedPackageProperties _packageProperties;


        #endregion Private Members

        //------------------------------------------------------
        //
        //  Private Class
        //
        //------------------------------------------------------

        #region Private Class: Restricted Stream

        /// <summary>
        /// This implementation of Stream class is a simple wrapper to restrict the
        /// read or write access to the underlying stream, as per user request.
        /// No validation for the stream method calls is done in this wrapper, the calls
        /// are passed onto the underlying stream object, which should do the
        /// validation as required.
        /// </summary>
        private sealed class RestrictedStream : Stream
        {
            #region Constructor

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="access"></param>
            internal RestrictedStream(Stream stream, FileAccess access)
            {
                if (stream == null)
                    throw new ArgumentNullException("stream");

                //Verifying if the FileAccess enum is valid
                //This constructor will never be called with FileAccess.ReadWrite
                Debug.Assert(access == FileAccess.Read || access == FileAccess.Write,
                    "The constructor of this private class is expected to be called with FileAccess.Read or FileAccess.Write");

                _stream = stream;

                if (access == FileAccess.Read)
                {
                    _canRead = true;
                    _canWrite = false;
                }
                else
                if (access == FileAccess.Write)
                {
                    _canRead = false;
                    _canWrite = true;
                }
            }

            #endregion Constructor

            #region Properties

            /// <summary>
            /// Member of the abstract Stream class
            /// </summary>
            /// <value>Bool, true if the stream can be read from, else false</value>
            public override bool CanRead
            {
                get
                {
                    if (!_disposed)
                        return _canRead;
                    else
                        return false;
                }
            }

            /// <summary>
            /// Member of the abstract Stream class
            /// </summary>
            /// <value>Bool, true if the stream can be seeked, else false</value>
            public override bool CanSeek
            {
                get
                {
                    if (!_disposed)
                        return _stream.CanSeek;
                    else
                        return false;
                }
            }

            /// <summary>
            /// Member of the abstract Stream class
            /// </summary>
            /// <value>Bool, true if the stream can be written to, else false</value>
            public override bool CanWrite
            {
                get
                {
                    if (!_disposed)
                        return _canWrite;
                    else
                        return false;
                }
            }

            /// <summary>
            /// Member of the abstract Stream class
            /// </summary>
            /// <value>Long value indicating the length of the stream</value>
            public override long Length
            {
                get
                {
                    ThrowIfStreamDisposed();
                    return _stream.Length;
                }
            }

            /// <summary>
            /// Member of the abstract Stream class
            /// </summary>
            /// <value>Long value indicating the current position in the stream</value>
            public override long Position
            {
                get
                {
                    ThrowIfStreamDisposed();
                    return _stream.Position;
                }
                set
                {
                    ThrowIfStreamDisposed();
                    _stream.Position = value;
                }
            }

            #endregion Properties

            #region Methods

            /// <summary>
            /// Member of the abstract Stream class
            /// </summary>
            /// <param name="offset">only zero is supported</param>
            /// <param name="origin">only SeekOrigin.Begin is supported</param>
            /// <returns>zero</returns>
            public override long Seek(long offset, SeekOrigin origin)
            {
                ThrowIfStreamDisposed();
                return _stream.Seek(offset, origin);
            }

            /// <summary>
            /// Member of the abstract Stream class
            /// </summary>
            /// <param name="newLength"></param>
            public override void SetLength(long newLength)
            {
                ThrowIfStreamDisposed();
                if (_canWrite)
                    _stream.SetLength(newLength);
                else
                    throw new NotSupportedException(SR.Get(SRID.ReadOnlyStream));
            }

            /// <summary>
            /// Member of the abstract Stream class
            /// </summary>
            /// <param name="buffer"></param>
            /// <param name="offset"></param>
            /// <param name="count"></param>
            /// <returns></returns>
            /// <remarks>
            /// The standard Stream.Read semantics, and in particular the restoration of the current
            /// position in case of an exception, is implemented by the underlying stream.
            /// </remarks>
            public override int Read(byte[] buffer, int offset, int count)
            {
                ThrowIfStreamDisposed();
                if (_canRead)
                    return _stream.Read(buffer, offset, count);
                else
                    throw new NotSupportedException(SR.Get(SRID.WriteOnlyStream));
            }

            /// <summary>
            /// Member of the abstract Stream class
            /// </summary>
            /// <param name="buf"></param>
            /// <param name="offset"></param>
            /// <param name="count"></param>
            public override void Write(byte[] buf, int offset, int count)
            {
                ThrowIfStreamDisposed();
                if (_canWrite)
                    _stream.Write(buf, offset, count);
                else
                    throw new NotSupportedException(SR.Get(SRID.ReadOnlyStream));
            }

            /// <summary>
            /// Member of the abstract Stream class
            /// </summary>
            public override void Flush()
            {
                ThrowIfStreamDisposed();
                if (_canWrite)
                    _stream.Flush();
            }
            #endregion Methods

            //------------------------------------------------------
            //
            //  Protected Methods
            //
            //------------------------------------------------------
            /// <summary>
            /// Dispose(bool)
            /// </summary>
            /// <param name="disposing"></param>
            protected override void Dispose(bool disposing)
            {
                try
                {
                    if (disposing)
                    {
                        if (!_disposed)
                        {
                            _stream.Close();
                        }
                    }
                }
                finally
                {
                    _disposed = true;
                    base.Dispose(disposing);
                }
            }


            #region Private Methods

            private void ThrowIfStreamDisposed()
            {
                if (_disposed)
                    throw new ObjectDisposedException(null, SR.Get(SRID.StreamObjectDisposed));
            }

            #endregion Private Methods

            #region Private Variables

            private Stream _stream;
            private bool _canRead;
            private bool _canWrite;
            private bool _disposed;

            #endregion Private Variables
        }

        #endregion Private Class: Restricted Stream
    }
}
