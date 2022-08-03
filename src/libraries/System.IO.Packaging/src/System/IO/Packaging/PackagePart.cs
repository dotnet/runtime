// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.IO.Packaging
{
    /// <summary>
    /// This class represents the a PackagePart within a container.
    /// This is a part of the Packaging Layer APIs
    /// </summary>
    public abstract class PackagePart
    {
        #region Protected Constructor

        /// <summary>
        /// Protected constructor for the abstract Base class.
        /// This is the current contract between the subclass and the base class
        /// If we decide some registration mechanism then this might change
        ///
        /// You should use this constructor in the rare case when you do not have
        /// the content type information related to this part and would prefer to
        /// obtain it later as required.
        ///
        /// These parts have the CompressionOption as NotCompressed by default.
        ///
        /// NOTE : If you are using this constructor from your subclass or passing a null
        /// for the content type parameter, be sure to implement the GetContentTypeCore
        /// method, as that will be called to get the content type value. This is provided
        /// to enable lazy initialization of the ContentType property.
        ///
        /// </summary>
        /// <param name="package">Package in which this part is being created</param>
        /// <param name="partUri">uri of the part</param>
        /// <exception cref="ArgumentNullException">If parameter "package" is null</exception>
        /// <exception cref="ArgumentNullException">If parameter "partUri" is null</exception>
        protected PackagePart(Package package, Uri partUri)
            : this(package, partUri, null, CompressionOption.NotCompressed)
        {
        }

        /// <summary>
        /// Protected constructor for the abstract Base class.
        /// This is the current contract between the subclass and the base class
        /// If we decide some registration mechanism then this might change
        ///
        /// These parts have the CompressionOption as NotCompressed by default.
        ///
        /// NOTE : If you are using this constructor from your subclass or passing a null
        /// for the content type parameter, be sure to implement the GetContentTypeCore
        /// method, as that will be called to get the content type value. This is provided
        /// to enable lazy initialization of the ContentType property.
        ///
        /// </summary>
        /// <param name="package">Package in which this part is being created</param>
        /// <param name="partUri">uri of the part</param>
        /// <param name="contentType">Content Type of the part, can be null if the value
        /// is unknown at the time of construction. However the value has to be made
        /// available anytime the ContentType property is called. A null value only indicates
        /// that the value will be provided later. Every PackagePart must have a valid
        /// Content Type</param>
        /// <exception cref="ArgumentNullException">If parameter "package" is null</exception>
        /// <exception cref="ArgumentNullException">If parameter "partUri" is null</exception>
        /// <exception cref="ArgumentException">If parameter "partUri" does not conform to the valid partUri syntax</exception>
        protected PackagePart(Package package, Uri partUri, string? contentType)
            : this(package, partUri, contentType, CompressionOption.NotCompressed)
        {
        }


        /// <summary>
        /// Protected constructor for the abstract Base class.
        /// This is the current contract between the subclass and the base class
        /// If we decide some registration mechanism then this might change
        ///
        /// NOTE : If you are using this constructor from your subclass or passing a null
        /// for the content type parameter, be sure to implement the GetContentTypeCore
        /// method, as that will be called to get the content type value. This is provided
        /// to enable lazy initialization of the ContentType property.
        ///
        /// </summary>
        /// <param name="package">Package in which this part is being created</param>
        /// <param name="partUri">uri of the part</param>
        /// <param name="contentType">Content Type of the part, can be null if the value
        /// is unknown at the time of construction. However the value has to be made
        /// available anytime the ContentType property is called. A null value only indicates
        /// that the value will be provided later. Every PackagePart must have a valid
        /// Content Type</param>
        /// <param name="compressionOption">compression option for this part</param>
        /// <exception cref="ArgumentNullException">If parameter "package" is null</exception>
        /// <exception cref="ArgumentNullException">If parameter "partUri" is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If CompressionOption enumeration [compressionOption] does not have one of the valid values</exception>
        /// <exception cref="ArgumentException">If parameter "partUri" does not conform to the valid partUri syntax</exception>
        protected PackagePart(Package package,
                                Uri partUri,
                                string? contentType,
                                CompressionOption compressionOption)
        {
            if (package is null)
            {
                throw new ArgumentNullException(nameof(package));
            }
            if (partUri is null)
            {
                throw new ArgumentNullException(nameof(partUri));
            }

            Package.ThrowIfCompressionOptionInvalid(compressionOption);

            _uri = PackUriHelper.ValidatePartUri(partUri);
            _container = package;

            if (contentType == null)
            {
                _contentType = null;
            }
            else
            {
                _contentType = new ContentType(contentType);
            }

            _requestedStreams = null;
            _compressionOption = compressionOption;
            _isRelationshipPart = PackUriHelper.IsRelationshipPartUri(partUri);
        }

        #endregion Protected Constructor

        #region Public Properties

        /// <summary>
        /// The Uri for this PackagePart. It is always relative to the Package Root
        /// The PackagePart properties can not be accessed if the parent container is closed.
        /// </summary>
        /// <value></value>
        /// <exception cref="InvalidOperationException">If this part has been deleted</exception>
        /// <exception cref="InvalidOperationException">If the parent package has been closed or disposed</exception>
        public Uri Uri
        {
            get
            {
                CheckInvalidState();
                return _uri;
            }
        }

        /// <summary>
        /// The Content type of the stream that is represented by this part.
        /// The PackagePart properties can not be accessed if the parent container is closed.
        /// The content type value can be provided by the underlying physical format
        /// implementation at the time of creation of the Part object ( constructor ) or
        /// We can initialize it in a lazy manner when the ContentType property is called
        /// called for the first time by calling the GetContentTypeCore method.
        /// Note: This method GetContentTypeCore() is only for lazy initialization of the Content
        /// type value and will only be called once. There is no way to change the content type of
        /// the part once it has been assigned.
        /// </summary>
        /// <value>Content Type of the Part [can never return null] </value>
        /// <exception cref="InvalidOperationException">If this part has been deleted</exception>
        /// <exception cref="InvalidOperationException">If the parent package has been closed or disposed</exception>
        /// <exception cref="InvalidOperationException">If the subclass fails to provide a non-null content type value.</exception>
        public string ContentType
        {
            get
            {
                CheckInvalidState();
                if (_contentType == null)
                {
                    //Lazy initialization for the content type
                    string contentType = GetContentTypeCore();

                    if (contentType == null)
                    {
                        // We have seen this bug in the past and have said that this should be
                        // treated as exception. If we get a null content type, it's an error.
                        // We want to throw this exception so that anyone sub-classing this class
                        // should not be setting the content type to null. Its like any other
                        // parameter validation. This is the only place we can validate it. We
                        // throw an ArgumentNullException, when the content type is set to null
                        // in the constructor.
                        //
                        // We cannot get rid of this exception. At most, we can change it to
                        // Debug.Assert. But then client code will see an Assert if they make
                        // a mistake and that is also not desirable.
                        //
                        // PackagePart is a public API.
                        throw new InvalidOperationException(SR.NullContentTypeProvided);
                    }
                    _contentType = new ContentType(contentType);
                }
                return _contentType.ToString();
            }
        }


        /// <summary>
        /// The parent container for this PackagePart
        /// The PackagePart properties can not be accessed if the parent container is closed.
        /// </summary>
        /// <value></value>
        /// <exception cref="InvalidOperationException">If this part has been deleted</exception>
        /// <exception cref="InvalidOperationException">If the parent package has been closed or disposed</exception>
        public Package Package
        {
            get
            {
                CheckInvalidState();
                return _container;
            }
        }

        /// <summary>
        /// CompressionOption class that was provided as a parameter during the original CreatePart call.
        /// The PackagePart properties can not be accessed if the parent container is closed.
        /// </summary>
        /// <exception cref="InvalidOperationException">If this part has been deleted</exception>
        /// <exception cref="InvalidOperationException">If the parent package has been closed or disposed</exception>
        public CompressionOption CompressionOption
        {
            get
            {
                CheckInvalidState();
                return _compressionOption;
            }
        }

        #endregion Public Properties

        #region Public Methods

        #region Content Type Method

        /// <summary>
        /// Custom Implementation for the GetContentType Method
        /// This method should only be implemented by those physical format implementors where
        /// the value for the content type cannot be provided at the time of construction of
        /// Part object and if calculating the content type value is a non-trivial or costly
        /// operation. The return value has to be a valid ContentType. This method will be used in
        /// real corner cases. The most common usage should be to provide the content type in the
        /// constructor.
        /// This method is only for lazy initialization of the Content type value and will only
        /// be called once. There is no way to change the content type of the part once it is
        /// assigned.
        /// </summary>
        /// <returns>Content type for the Part</returns>
        /// <exception cref="NotSupportedException">By default, this method throws a NotSupportedException. If a subclass wants to
        /// initialize the content type for a PackagePart in a lazy manner they must override this method.</exception>
        protected virtual string GetContentTypeCore()
        {
            throw new NotSupportedException(SR.GetContentTypeCoreNotImplemented);
        }


        #endregion Content Type Method

        #region Stream Methods

        /// <summary>
        /// Returns the underlying stream that is represented by this part
        /// with the default FileMode and FileAccess
        /// Note: If you are requesting a stream for a relationship part and
        /// at the same time using relationship APIs to manipulate relationships,
        /// the final persisted data will depend on which data gets flushed last.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If this part has been deleted</exception>
        /// <exception cref="InvalidOperationException">If the parent package has been closed or disposed</exception>
        /// <exception cref="IOException">If the subclass fails to provide a non-null stream object</exception>
        public Stream GetStream()
        {
            CheckInvalidState();
            return GetStream(FileMode.OpenOrCreate, _container.FileOpenAccess);
        }

        /// <summary>
        /// Returns the underlying stream in the specified mode and the
        /// default FileAccess
        /// Note: If you are requesting a stream for a relationship part for editing
        /// and at the same time using relationship APIs to manipulate relationships,
        /// the final persisted data will depend on which data gets flushed last.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If this part has been deleted</exception>
        /// <exception cref="InvalidOperationException">If the parent package has been closed or disposed</exception>
        /// <exception cref="ArgumentOutOfRangeException">If FileMode enumeration [mode] does not have one of the valid values</exception>
        /// <exception cref="IOException">If FileAccess.Read is provided and FileMode values are any of the following -
        /// FileMode.Create, FileMode.CreateNew, FileMode.Truncate, FileMode.Append</exception>
        /// <exception cref="IOException">If the mode and access for the Package and the Stream are not compatible</exception>
        /// <exception cref="IOException">If the subclass fails to provide a non-null stream object</exception>
        public Stream GetStream(FileMode mode)
        {
            CheckInvalidState();
            return GetStream(mode, _container.FileOpenAccess);
        }

        /// <summary>
        /// Returns the underlying stream that is represented by this part
        /// in the specified mode with the access.
        /// Note: If you are requesting a stream for a relationship part and
        /// at the same time using relationship APIs to manipulate relationships,
        /// the final persisted data will depend on which data gets flushed last.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="access"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If this part has been deleted</exception>
        /// <exception cref="InvalidOperationException">If the parent package has been closed or disposed</exception>
        /// <exception cref="ArgumentOutOfRangeException">If FileMode enumeration [mode] does not have one of the valid values</exception>
        /// <exception cref="ArgumentOutOfRangeException">If FileAccess enumeration [access] does not have one of the valid values</exception>
        /// <exception cref="IOException">If FileAccess.Read is provided and FileMode values are any of the following -
        /// FileMode.Create, FileMode.CreateNew, FileMode.Truncate, FileMode.Append</exception>
        /// <exception cref="IOException">If the mode and access for the Package and the Stream are not compatible</exception>
        /// <exception cref="IOException">If the subclass fails to provide a non-null stream object</exception>
        public Stream GetStream(FileMode mode, FileAccess access)
        {
            CheckInvalidState();
            ThrowIfOpenAccessModesAreIncompatible(mode, access);

            if (mode == FileMode.CreateNew)
                throw new ArgumentException(SR.CreateNewNotSupported);
            if (mode == FileMode.Truncate)
                throw new ArgumentException(SR.TruncateNotSupported);

            Stream? s = GetStreamCore(mode, access);

            if (s == null)
                throw new IOException(SR.NullStreamReturned);

            //Detect if any stream implementations are returning all three
            //properties - CanSeek, CanWrite and CanRead as false. Such a
            //stream should be pretty much useless. And as per current programming
            //practice, these properties are all false, when the stream has been
            //disposed.
            Debug.Assert(!IsStreamClosed(s));

            //Lazy init
            _requestedStreams ??= new List<Stream>(); //Default capacity is 4

            //Delete all the closed streams from the _requestedStreams list.
            //Each time a new stream is handed out, we go through the list
            //to clean up streams that were handed out and have been closed.
            //Thus those stream can be garbage collected and we will avoid
            //keeping around stream objects that have been disposed
            CleanUpRequestedStreamsList();

            _requestedStreams.Add(s);

            return s;
        }

        /// <summary>
        /// Custom Implementation for the GetSream Method
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="access"></param>
        /// <returns></returns>
        protected abstract Stream? GetStreamCore(FileMode mode, FileAccess access);

        #endregion Stream Methods

        #region PackageRelationship Methods
        /// <summary>
        /// Adds a relationship to this PackagePart with the Target PackagePart specified as the Uri
        /// Initial and trailing spaces in the name of the PackageRelationship are trimmed.
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="targetMode">Enumeration indicating the base uri for the target uri</param>
        /// <param name="relationshipType">PackageRelationship type, having uri like syntax that is used to
        /// uniquely identify the role of the relationship</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If this part has been deleted</exception>
        /// <exception cref="InvalidOperationException">If the parent package has been closed or disposed</exception>
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
        /// Adds a relationship to this PackagePart with the Target PackagePart specified as the Uri
        /// Initial and trailing spaces in the name of the PackageRelationship are trimmed.
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="targetMode">Enumeration indicating the base uri for the target uri</param>
        /// <param name="relationshipType">PackageRelationship type, having uri like syntax that is used to
        /// uniquely identify the role of the relationship</param>
        /// <param name="id">String that conforms to the xsd:ID datatype. Unique across the source's
        /// relationships. Null is OK (ID will be generated). An empty string is an invalid XML ID.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If this part has been deleted</exception>
        /// <exception cref="InvalidOperationException">If the parent package has been closed or disposed</exception>
        /// <exception cref="IOException">If the package is readonly, it cannot be modified</exception>
        /// <exception cref="ArgumentNullException">If parameter "targetUri" is null</exception>
        /// <exception cref="ArgumentNullException">If parameter "relationshipType" is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If parameter "targetMode" enumeration does not have a valid value</exception>
        /// <exception cref="ArgumentException">If TargetMode is TargetMode.Internal and the targetUri is an absolute Uri </exception>
        /// <exception cref="ArgumentException">If relationship is being targeted to a relationship part</exception>
        /// <exception cref="System.Xml.XmlException">If parameter "id" is not a valid Xsd Id</exception>
        /// <exception cref="System.Xml.XmlException">If an id is provided in the method, and its not unique</exception>
        public PackageRelationship CreateRelationship(Uri targetUri, TargetMode targetMode, string relationshipType, string? id)
        {
            CheckInvalidState();
            _container.ThrowIfReadOnly();
            EnsureRelationships();
            //All parameter validation is done in the following method
            return _relationships.Add(targetUri, targetMode, relationshipType, id);
        }

        /// <summary>
        /// Deletes a relationship from the PackagePart. This is done based on the
        /// relationship's ID. The target PackagePart is not affected by this operation.
        /// </summary>
        /// <param name="id">The ID of the relationship to delete. An invalid ID will not
        /// throw an exception, but nothing will be deleted.</param>
        /// <exception cref="InvalidOperationException">If this part has been deleted</exception>
        /// <exception cref="InvalidOperationException">If the parent package has been closed or disposed</exception>
        /// <exception cref="IOException">If the package is readonly, it cannot be modified</exception>
        /// <exception cref="ArgumentNullException">If parameter "id" is null</exception>
        /// <exception cref="System.Xml.XmlException">If parameter "id" is not a valid Xsd Id</exception>
        public void DeleteRelationship(string id)
        {
            CheckInvalidState();
            _container.ThrowIfReadOnly();

            if (id == null)
                throw new ArgumentNullException(nameof(id));

            InternalRelationshipCollection.ThrowIfInvalidXsdId(id);

            EnsureRelationships();
            _relationships.Delete(id);
        }

        /// <summary>
        /// Returns a collection of all the Relationships that are
        /// owned by this PackagePart
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If this part has been deleted</exception>
        /// <exception cref="InvalidOperationException">If the parent package has been closed or disposed</exception>
        /// <exception cref="IOException">If the package is write only, no information can be retrieved from it</exception>
        public PackageRelationshipCollection GetRelationships()
        {
            //All the validations for dispose and file access are done in the
            //GetRelationshipsHelper method.

            return GetRelationshipsHelper(null);
        }

        /// <summary>
        /// Returns a collection of filtered Relationships that are
        /// owned by this PackagePart
        /// The relationshipType string is compared with the type of the relationships
        /// in a case sensitive and culture ignorant manner.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If this part has been deleted</exception>
        /// <exception cref="InvalidOperationException">If the parent package has been closed or disposed</exception>
        /// <exception cref="IOException">If the package is write only, no information can be retrieved from it</exception>
        /// <exception cref="ArgumentNullException">If parameter "relationshipType" is null</exception>
        /// <exception cref="ArgumentException">If parameter "relationshipType" is an empty string</exception>
        public PackageRelationshipCollection GetRelationshipsByType(string relationshipType)
        {
            //These checks are made in the GetRelationshipsHelper as well, but we make them
            //here as we need to perform parameter validation
            CheckInvalidState();
            _container.ThrowIfWriteOnly();

            if (relationshipType == null)
                throw new ArgumentNullException(nameof(relationshipType));

            InternalRelationshipCollection.ThrowIfInvalidRelationshipType(relationshipType);

            return GetRelationshipsHelper(relationshipType);
        }

        /// <summary>
        /// Retrieve a relationship per ID.
        /// </summary>
        /// <param name="id">The relationship ID.</param>
        /// <returns>The relationship with ID 'id' or throw an exception if not found.</returns>
        /// <exception cref="InvalidOperationException">If this part has been deleted</exception>
        /// <exception cref="InvalidOperationException">If the parent package has been closed or disposed</exception>
        /// <exception cref="IOException">If the package is write only, no information can be retrieved from it</exception>
        /// <exception cref="ArgumentNullException">If parameter "id" is null</exception>
        /// <exception cref="System.Xml.XmlException">If parameter "id" is not a valid Xsd Id</exception>
        /// <exception cref="InvalidOperationException">If the requested relationship does not exist in the Package</exception>
        public PackageRelationship GetRelationship(string id)
        {
            //All the validations for dispose and file access are done in the
            //GetRelationshipHelper method.

            PackageRelationship? returnedRelationship = GetRelationshipHelper(id);
            if (returnedRelationship == null)
                throw new InvalidOperationException(SR.PackagePartRelationshipDoesNotExist);
            else
                return returnedRelationship;
        }

        /// <summary>
        /// Returns whether there is a relationship with the specified ID.
        /// </summary>
        /// <param name="id">The relationship ID.</param>
        /// <returns>true iff a relationship with ID 'id' is defined on this source.</returns>
        /// <exception cref="InvalidOperationException">If this part has been deleted</exception>
        /// <exception cref="InvalidOperationException">If the parent package has been closed or disposed</exception>
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

        #region Internal Properties

        internal bool IsRelationshipPart
        {
            get
            {
                return _isRelationshipPart;
            }
        }

        //This property can be set to indicate if the part has been deleted
        internal bool IsDeleted
        {
            get
            {
                return _deleted;
            }
            set
            {
                _deleted = value;
            }
        }

        //This property can be set to indicate if the part has been deleted
        internal bool IsClosed
        {
            get
            {
                return _disposed;
            }
        }

        /// <summary>
        /// This property returns the content type of the part
        /// as a validated strongly typed ContentType object
        /// </summary>
        internal ContentType ValidatedContentType
        {
            get
            {
                return _contentType!;
            }
        }

        #endregion Internal Properties

        #region Internal Methods

        //Delete all the relationships for this part
        internal void ClearRelationships()
        {
            _relationships?.Clear();
        }

        //Flush all the streams that are currently opened for this part and the relationships for this part
        //Note: This method is never be called on a deleted part
        internal void Flush()
        {
            Debug.Assert(_deleted != true, "PackagePart.Flush should never be called on a deleted part");

            if (_requestedStreams != null)
            {
                foreach (Stream s in _requestedStreams)
                {
                    // Streams in this list are never set to null, so we do not need to check for
                    // stream being null; However it could be closed by some external code. In that case
                    // this property (CanWrite) will still be accessible and we can check to see
                    // whether we can call flush or no.
                    if (s.CanWrite)
                        s.Flush();
                }
            }

            // Relationships for this part should have been flushed earlier in the Package.Flush method.
        }

        //Close all the streams that are open for this part.
        internal void Close()
        {
            if (!_disposed)
            {
                try
                {
                    if (_requestedStreams != null)
                    {
                        //Adding this extra check here to optimize delete operation
                        //Every time we delete a part we close it before deleting to
                        //ensure that its deleted in a valid state. However, we do not
                        //need to persist any changes if the part is being deleted.
                        if (!_deleted)
                        {
                            foreach (Stream s in _requestedStreams)
                            {
                                s.Dispose();
                            }
                        }
                        _requestedStreams.Clear();
                    }

                    // Relationships for this part should have been flushed/closed earlier in the Package.Close method.
                }
                finally
                {
                    _requestedStreams = null;

                    //InternalRelationshipCollection is not required any more
                    _relationships = null;

                    //Once the container is closed there is no way to get to the stream or any other part
                    //in the container.
                    _container = null!;

                    //We do not need to explicitly call GC.SuppressFinalize(this)

                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// write the relationships part
        /// </summary>
        /// <remarks>
        /// </remarks>
        internal void FlushRelationships()
        {
            Debug.Assert(_deleted != true, "PackagePart.FlushRelationsips should never be called on a deleted part");

            // flush relationships
            if (_relationships != null && _container.FileOpenAccess != FileAccess.Read)
            {
                _relationships.Flush();
            }
        }


        internal void CloseRelationships()
        {
            if (!_deleted)
            {
                //Flush the relationships for this part.
                FlushRelationships();
            }
        }

        #endregion Internal Methods

        #region Private Methods

        // lazy init
        [MemberNotNull(nameof(_relationships))]
        private void EnsureRelationships()
        {
            if (_relationships == null)
            {
                // check here
                ThrowIfRelationship();

                // obtain the relationships from the PackageRelationship part (if available)
                _relationships = new InternalRelationshipCollection(this);
            }
        }

        //Make sure that the access modes for the container and the part are compatible
        private void ThrowIfOpenAccessModesAreIncompatible(FileMode mode, FileAccess access)
        {
            Package.ThrowIfFileModeInvalid(mode);
            Package.ThrowIfFileAccessInvalid(access);

            //Creating a part using a readonly stream.
            if (access == FileAccess.Read &&
                (mode == FileMode.Create || mode == FileMode.CreateNew || mode == FileMode.Truncate || mode == FileMode.Append))
                throw new IOException(SR.UnsupportedCombinationOfModeAccess);

            //Incompatible access modes between container and part stream.
            if ((_container.FileOpenAccess == FileAccess.Read && access != FileAccess.Read) ||
                (_container.FileOpenAccess == FileAccess.Write && access != FileAccess.Write))
                throw new IOException(SR.ContainerAndPartModeIncompatible);
        }

        //Check if the part is in an invalid state
        private void CheckInvalidState()
        {
            ThrowIfPackagePartDeleted();
            ThrowIfParentContainerClosed();
        }

        //If the parent container is closed then the operations on this part like getting stream make no sense
        private void ThrowIfParentContainerClosed()
        {
            if (_container == null)
                throw new InvalidOperationException(SR.ParentContainerClosed);
        }

        //If the part has been deleted then we throw
        private void ThrowIfPackagePartDeleted()
        {
            if (_deleted)
                throw new InvalidOperationException(SR.PackagePartDeleted);
        }

        // some operations are invalid if we are a relationship part
        private void ThrowIfRelationship()
        {
            if (IsRelationshipPart)
                throw new InvalidOperationException(SR.RelationshipPartsCannotHaveRelationships);
        }

        /// <summary>
        /// Retrieve a relationship per ID.
        /// </summary>
        /// <param name="id">The relationship ID.</param>
        /// <returns>The relationship with ID 'id' or null if not found.</returns>
        private PackageRelationship? GetRelationshipHelper(string id)
        {
            CheckInvalidState();
            _container.ThrowIfWriteOnly();

            if (id == null)
                throw new ArgumentNullException(nameof(id));

            InternalRelationshipCollection.ThrowIfInvalidXsdId(id);

            EnsureRelationships();
            return _relationships.GetRelationship(id);
        }

        /// <summary>
        /// Returns a collection of all the Relationships that are
        /// owned by this PackagePart, based on the filter string
        /// </summary>
        /// <returns></returns>
        private PackageRelationshipCollection GetRelationshipsHelper(string? filterString)
        {
            CheckInvalidState();
            _container.ThrowIfWriteOnly();
            EnsureRelationships();
            //Internally null is used to indicate that no filter string was specified and
            //and all the relationships should be returned.
            return new PackageRelationshipCollection(_relationships, filterString);
        }

        //Deletes all the streams that have been closed from the _requestedStreams list.
        private void CleanUpRequestedStreamsList()
        {
            if (_requestedStreams != null)
            {
                for (int i = _requestedStreams.Count - 1; i >= 0; i--)
                {
                    if (IsStreamClosed(_requestedStreams[i]))
                        _requestedStreams.RemoveAt(i);
                }
            }
        }

        //Detect if the stream has been closed.
        //When a stream is closed the three flags - CanSeek, CanRead and CanWrite
        //return false. These properties do not throw ObjectDisposedException.
        //So we rely on the values of these properties to determine if a stream
        //has been closed.
        private static bool IsStreamClosed(Stream s)
        {
            return !s.CanRead && !s.CanSeek && !s.CanWrite;
        }

        #endregion Private Methods

        #region Private Members

        private readonly PackUriHelper.ValidatedPartUri _uri;
        private Package _container;
        private ContentType? _contentType;
        private List<Stream>? _requestedStreams;
        private InternalRelationshipCollection? _relationships;
        private readonly CompressionOption _compressionOption = CompressionOption.NotCompressed;
        private bool _disposed;
        private bool _deleted;
        private readonly bool _isRelationshipPart;

        #endregion Private Members
    }
}
