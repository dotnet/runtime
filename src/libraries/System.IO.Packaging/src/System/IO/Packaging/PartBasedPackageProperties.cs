// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//-----------------------------------------------------------------------------
//
// Description:
//  The package properties are a subset of the standard OLE property sets
//  SummaryInformation and DocumentSummaryInformation, and include such properties
//  as Title and Subject.
//
// History:
//  06/23/2005: JohnLarc:   Initial implementation.
//  09/23/2005: JohnLarc:   Removed from public surface.
//
//-----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Windows;                   // for ExceptionStringTable
using System.Globalization;             // For CultureInfo

namespace System.IO.Packaging
{
    /// <summary>
    ///  The package properties are a subset of the standard OLE property sets
    ///  SummaryInformation and DocumentSummaryInformation, and include such properties
    ///  as Title and Subject.
    /// </summary>
    /// <remarks>
    /// <para>No specific API is provided to interleave the properties part in streaming creation.
    /// However, Package.Flush is expected to persist package-wide data, in particular metadata.
    /// Therefore, in streaming creation, a write-only write-once discipline has to be enforced.</para>
    /// <para>Setting a property to null deletes this property. 'null' is never strictly speaking
    /// a property value, but an absence indicator.</para>
    /// </remarks>
    internal class PartBasedPackageProperties : PackageProperties
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal PartBasedPackageProperties(Package package)
        {
            _package = package;

            // Initialize literals as Xml Atomic strings.
            _nameTable = PackageXmlStringTable.NameTable;

            ReadPropertyValuesFromPackage();

            // No matter what happens during initialization, the dirty flag should not be set.
            _dirty = false;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <value>
        /// The primary creator. The identification is environment-specific and
        /// can consist of a name, email address, employee ID, etc. It is
        /// recommended that this value be only as verbose as necessary to
        /// identify the individual.
        /// </value>
        public override string Creator
        {
            get
            {
                return (string)GetPropertyValue(PackageXmlEnum.Creator);
            }
            set
            {
                RecordNewBinding(PackageXmlEnum.Creator, value);
            }
        }

        /// <value>
        /// The title.
        /// </value>
        public override string Title
        {
            get
            {
                return (string)GetPropertyValue(PackageXmlEnum.Title);
            }
            set
            {
                RecordNewBinding(PackageXmlEnum.Title, value);
            }
        }

        /// <value>
        /// The topic of the contents.
        /// </value>
        public override string Subject
        {
            get
            {
                return (string)GetPropertyValue(PackageXmlEnum.Subject);
            }
            set
            {
                RecordNewBinding(PackageXmlEnum.Subject, value);
            }
        }

        /// <value>
        /// The category. This value is typically used by UI applications to create navigation
        /// controls.
        /// </value>
        public override string Category
        {
            get
            {
                return (string)GetPropertyValue(PackageXmlEnum.Category);
            }
            set
            {
                RecordNewBinding(PackageXmlEnum.Category, value);
            }
        }

        /// <value>
        /// A delimited set of keywords to support searching and indexing. This
        /// is typically a list of terms that are not available elsewhere in the
        /// properties.
        /// </value>
        public override string Keywords
        {
            get
            {
                return (string)GetPropertyValue(PackageXmlEnum.Keywords);
            }
            set
            {
                RecordNewBinding(PackageXmlEnum.Keywords, value);
            }
        }

        /// <value>
        /// The description or abstract of the contents.
        /// </value>
        public override string Description
        {
            get
            {
                return (string)GetPropertyValue(PackageXmlEnum.Description);
            }
            set
            {
                RecordNewBinding(PackageXmlEnum.Description, value);
            }
        }

        /// <value>
        /// The type of content represented, generally defined by a specific
        /// use and intended audience. Example values include "Whitepaper",
        /// "Security Bulletin", and "Exam". (This property is distinct from
        /// MIME content types as defined in RFC 2616.) 
        /// </value>
        public override string ContentType
        {
            get
            {
                string contentType = GetPropertyValue(PackageXmlEnum.ContentType) as string;

                return contentType;
            }
            set
            {
                RecordNewBinding(PackageXmlEnum.ContentType, value);
            }
        }

        /// <value>
        /// The status of the content. Example values include "Draft",
        /// "Reviewed", and "Final".
        /// </value>
        public override string ContentStatus
        {
            get
            {
                return (string)GetPropertyValue(PackageXmlEnum.ContentStatus);
            }
            set
            {
                RecordNewBinding(PackageXmlEnum.ContentStatus, value);
            }
        }

        /// <value>
        /// The version number. This value is set by the user or by the application.
        /// </value>
        public override string Version
        {
            get
            {
                return (string)GetPropertyValue(PackageXmlEnum.Version);
            }
            set
            {
                RecordNewBinding(PackageXmlEnum.Version, value);
            }
        }

        /// <value>
        /// The revision number. This value indicates the number of saves or
        /// revisions. The application is responsible for updating this value
        /// after each revision.
        /// </value>
        public override string Revision
        {
            get
            {
                return (string)GetPropertyValue(PackageXmlEnum.Revision);
            }
            set
            {
                RecordNewBinding(PackageXmlEnum.Revision, value);
            }
        }

        /// <value>
        /// The creation date and time.
        /// </value>
        public override Nullable<DateTime> Created
        {
            get
            {
                return GetDateTimePropertyValue(PackageXmlEnum.Created);
            }
            set
            {
                RecordNewBinding(PackageXmlEnum.Created, value);
            }
        }

        /// <value>
        /// The date and time of the last modification.
        /// </value>
        public override Nullable<DateTime> Modified
        {
            get
            {
                return GetDateTimePropertyValue(PackageXmlEnum.Modified);
            }
            set
            {
                RecordNewBinding(PackageXmlEnum.Modified, value);
            }
        }

        /// <value>
        /// The user who performed the last modification. The identification is
        /// environment-specific and can consist of a name, email address,
        /// employee ID, etc. It is recommended that this value be only as
        /// verbose as necessary to identify the individual.
        /// </value>
        public override string LastModifiedBy
        {
            get
            {
                return (string)GetPropertyValue(PackageXmlEnum.LastModifiedBy);
            }
            set
            {
                RecordNewBinding(PackageXmlEnum.LastModifiedBy, value);
            }
        }

        /// <value>
        /// The date and time of the last printing.
        /// </value>
        public override Nullable<DateTime> LastPrinted
        {
            get
            {
                return GetDateTimePropertyValue(PackageXmlEnum.LastPrinted);
            }
            set
            {
                RecordNewBinding(PackageXmlEnum.LastPrinted, value);
            }
        }

        /// <value>
        /// A language of the intellectual content of the resource
        /// </value>
        public override string Language
        {
            get
            {
                return (string)GetPropertyValue(PackageXmlEnum.Language);
            }
            set
            {
                RecordNewBinding(PackageXmlEnum.Language, value);
            }
        }

        /// <value>
        /// A unique identifier.
        /// </value>
        public override string Identifier
        {
            get
            {
                return (string)GetPropertyValue(PackageXmlEnum.Identifier);
            }
            set
            {
                RecordNewBinding(PackageXmlEnum.Identifier, value);
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Invoked from Package.Flush.
        // The expectation is that whatever is currently dirty will get flushed.
        // In streaming production, this requires flushing to a piece.
        // Outside of streaming creation, the whole thing gets rewritten at every flush.
        internal void Flush()
        {
            if (!_dirty)
                return;

            // Make sure there is a part to write to and that it contains
            // the expected start markup.
            EnsureXmlWriter();

            // Write the property elements and clear _dirty.
            // The whole property table gets cleared in streaming production.
            SerializeDirtyProperties();

            // In streaming mode, keep the writer around until we close.
            // Do a simple flush on the writer to make sure the content is written to a piece.
            if (_package.InStreamingCreation && _xmlWriter != null)
            {
                _xmlWriter.Flush();
            }
            // Else, add closing markup and close the writer.
            else
            {
                CloseXmlWriter();
            }
        }

        // Invoked from Package.Close.
        // Carries out the same operations as Flush, except that, in streaming mode,
        // the writer has to be closed.
        internal void Close()
        {
            Flush();

            // The following has to be done even if all properties have been saved (_dirty == false).
            if (_package.InStreamingCreation && _xmlWriter != null)
                CloseXmlWriter();
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // The property store is implemented as a hash table of objects.
        // Keys are taken from the set of string constants defined in this
        // class and compared by their references rather than their values.

        private object GetPropertyValue(PackageXmlEnum propertyName)
        {
            _package.ThrowIfWriteOnly();

            if (!_propertyDictionary.ContainsKey(propertyName))
                return null;
            return _propertyDictionary[propertyName];
        }

        // Shim function to adequately cast the result of GetPropertyValue.
        private Nullable<DateTime> GetDateTimePropertyValue(PackageXmlEnum propertyName)
        {
            object valueObject = GetPropertyValue(propertyName);
            if (valueObject == null)
                return null;
            // If an object is there, it will be a DateTime (not a Nullable<DateTime>).
            return (Nullable<DateTime>)valueObject;
        }

        // Set new property value.
        // Override that sets the initializing flag to false to reflect the default
        // situation: recording a binding to implement a value assignment.
        private void RecordNewBinding(PackageXmlEnum propertyenum, object value)
        {
            RecordNewBinding(propertyenum, value, false /* not invoked at construction */, null);
        }

        // Set new property value.
        // Null value is passed for deleting a property.
        // While initializing, we are not assigning new values, and so the dirty flag should
        // stay untouched.
        private void RecordNewBinding(PackageXmlEnum propertyenum, object value, bool initializing, XmlTextReader reader)
        {
            // If we are reading values from the package, reader cannot be null
            Debug.Assert(!initializing || reader != null);

            if (!initializing)
                _package.ThrowIfReadOnly();

            // Case of an existing property.
            if (_propertyDictionary.ContainsKey(propertyenum))
            {
                // Parsing should detect redundant entries.
                if (initializing)
                {
                    throw new XmlException(SR.Get(SRID.DuplicateCorePropertyName, reader.Name),
                        null, reader.LineNumber, reader.LinePosition);
                }

                // No edit of existing properties in streaming production.
                if (_package.InStreamingCreation)
                    throw new InvalidOperationException(SR.Get(SRID.OperationViolatesWriteOnceSemantics));

                // Nullable<DateTime> values can be checked against null
                if (value == null) // a deletion
                {
                    _propertyDictionary.Remove(propertyenum);
                }
                else // an update
                {
                    _propertyDictionary[propertyenum] = value;
                }
                // If the binding is an assignment rather than an initialization, set the dirty flag.
                _dirty = !initializing;
            }
            // Case of a null value in the absence of an existing property.
            else if (value == null)
            {
                // Even thinking about setting a property to null in streaming production is a no-no.
                if (_package.InStreamingCreation)
                    throw new InvalidOperationException(SR.Get(SRID.OperationViolatesWriteOnceSemantics));

                // Outside of streaming production, deleting a non-existing property is a no-op.
            }
            // Case of an initial value being set for a property.
            else
            {
                _propertyDictionary.Add(propertyenum, value);
                // If the binding is an assignment rather than an initialization, set the dirty flag.
                _dirty = !initializing;
            }
        }

        // Initialize object from property values found in package.
        // All values will remain null if the package is not enabled for reading.
        private void ReadPropertyValuesFromPackage()
        {
            Invariant.Assert(_propertyPart == null); // This gets called exclusively from constructor.

            // Don't try to read properties from the package it does not have read access
            if (_package.FileOpenAccess == FileAccess.Write)
                return;

            _propertyPart = GetPropertyPart();
            if (_propertyPart == null)
                return;

            ParseCorePropertyPart(_propertyPart);
        }

        // Locate core properties part using the package relationship that points to it.
        private PackagePart GetPropertyPart()
        {
            // Find a package-wide relationship of type CoreDocumentPropertiesRelationshipType.
            PackageRelationship corePropertiesRelationship = GetCorePropertiesRelationship();
            if (corePropertiesRelationship == null)
                return null;

            // Retrieve the part referenced by its target URI.
            if (corePropertiesRelationship.TargetMode != TargetMode.Internal)
                throw new FileFormatException(SR.Get(SRID.NoExternalTargetForMetadataRelationship));

            PackagePart propertiesPart = null;
            Uri propertiesPartUri = PackUriHelper.ResolvePartUri(
                PackUriHelper.PackageRootUri,
                corePropertiesRelationship.TargetUri);

            if (!_package.PartExists(propertiesPartUri))
                throw new FileFormatException(SR.Get(SRID.DanglingMetadataRelationship));

            propertiesPart = _package.GetPart(propertiesPartUri);
            if (!propertiesPart.ValidatedContentType.AreTypeAndSubTypeEqual(s_coreDocumentPropertiesContentType))
            {
                throw new FileFormatException(SR.Get(SRID.WrongContentTypeForPropertyPart));
            }

            return propertiesPart;
        }

        // Find a package-wide relationship of type CoreDocumentPropertiesRelationshipType.
        private PackageRelationship GetCorePropertiesRelationship()
        {
            PackageRelationship propertiesPartRelationship = null;
            foreach (PackageRelationship rel
                in _package.GetRelationshipsByType(_coreDocumentPropertiesRelationshipType))
            {
                if (propertiesPartRelationship != null)
                {
                    throw new FileFormatException(SR.Get(SRID.MoreThanOneMetadataRelationships));
                }
                propertiesPartRelationship = rel;
            }
            return propertiesPartRelationship;
        }

        // Deserialize properties part.
        private void ParseCorePropertyPart(PackagePart part)
        {
            Stream stream = part.GetStream(FileMode.Open, FileAccess.Read);

            // Create a reader that uses _nameTable so as to use the set of tag literals
            // in effect as a set of atomic identifiers.
            XmlTextReader reader = new XmlTextReader(stream, _nameTable);

            //Prohibit DTD from the markup as per the OPC spec
#pragma warning disable 618
            reader.ProhibitDtd = true;
#pragma warning restore 618

            //This method expects the reader to be in ReadState.Initial.
            //It will make the first read call.
            PackagingUtilities.PerformInitailReadAndVerifyEncoding(reader);

            //Note: After the previous method call the reader should be at the first tag in the markup.
            //MoveToContent - Skips over the following - ProcessingInstruction, DocumentType, Comment, Whitespace, or SignificantWhitespace
            //If the reader is currently at a content node then this function call is a no-op
            if (reader.MoveToContent() != XmlNodeType.Element
                || (object)reader.NamespaceURI != PackageXmlStringTable.GetXmlStringAsObject(PackageXmlEnum.PackageCorePropertiesNamespace)
                || (object)reader.LocalName != PackageXmlStringTable.GetXmlStringAsObject(PackageXmlEnum.CoreProperties))
            {
                throw new XmlException(SR.Get(SRID.CorePropertiesElementExpected),
                    null, reader.LineNumber, reader.LinePosition);
            }

            // The schema is closed and defines no attributes on the root element.
            if (PackagingUtilities.GetNonXmlnsAttributeCount(reader) != 0)
            {
                throw new XmlException(SR.Get(SRID.PropertyWrongNumbOfAttribsDefinedOn, reader.Name),
                    null, reader.LineNumber, reader.LinePosition);
            }

            // Iterate through property elements until EOF. Note the proper closing of all
            // open tags is checked by the reader itself.
            // This loop deals only with depth-1 start tags. Handling of element content
            // is delegated to dedicated functions.
            int attributesCount;

            while (reader.Read() && reader.MoveToContent() != XmlNodeType.None)
            {
                // Ignore end-tags. We check element errors on opening tags.
                if (reader.NodeType == XmlNodeType.EndElement)
                    continue;

                // Any content markup that is not an element here is unexpected.
                if (reader.NodeType != XmlNodeType.Element)
                    throw new XmlException(SR.Get(SRID.PropertyStartTagExpected),
                        null, reader.LineNumber, reader.LinePosition);

                // Any element below the root should open at level 1 exclusively.
                if (reader.Depth != 1)
                    throw new XmlException(SR.Get(SRID.NoStructuredContentInsideProperties),
                        null, reader.LineNumber, reader.LinePosition);

                attributesCount = PackagingUtilities.GetNonXmlnsAttributeCount(reader);

                // Property elements can occur in any order (xsd:all).
                object localName = reader.LocalName;
                PackageXmlEnum xmlStringIndex = PackageXmlStringTable.GetEnumOf(localName);
                String valueType = PackageXmlStringTable.GetValueType(xmlStringIndex);

                if (Array.IndexOf(s_validProperties, xmlStringIndex) == -1)  // An unexpected element is an error.
                {
                    throw new XmlException(
                        SR.Get(SRID.InvalidPropertyNameInCorePropertiesPart, reader.LocalName),
                        null, reader.LineNumber, reader.LinePosition);
                }

                // Any element not in the valid core properties namespace is unexpected.
                // The following is an object comparison, not a string comparison.
                if ((object)reader.NamespaceURI != PackageXmlStringTable.GetXmlStringAsObject(PackageXmlStringTable.GetXmlNamespace(xmlStringIndex)))
                    throw new XmlException(SR.Get(SRID.UnknownNamespaceInCorePropertiesPart),
                        null, reader.LineNumber, reader.LinePosition);

                if (String.CompareOrdinal(valueType, "String") == 0)
                {
                    // The schema is closed and defines no attributes on this type of element.
                    if (attributesCount != 0)
                    {
                        throw new XmlException(SR.Get(SRID.PropertyWrongNumbOfAttribsDefinedOn, reader.Name),
                            null, reader.LineNumber, reader.LinePosition);
                    }

                    RecordNewBinding(xmlStringIndex, GetStringData(reader), true /*initializing*/, reader);
                }
                else if (String.CompareOrdinal(valueType, "DateTime") == 0)
                {
                    int allowedAttributeCount = (object)reader.NamespaceURI ==
                                                        PackageXmlStringTable.GetXmlStringAsObject(PackageXmlEnum.DublinCoreTermsNamespace)
                                                    ? 1 : 0;

                    // The schema is closed and defines no attributes on this type of element.
                    if (attributesCount != allowedAttributeCount)
                    {
                        throw new XmlException(SR.Get(SRID.PropertyWrongNumbOfAttribsDefinedOn, reader.Name),
                            null, reader.LineNumber, reader.LinePosition);
                    }

                    if (allowedAttributeCount != 0)
                    {
                        ValidateXsiType(reader,
                            PackageXmlStringTable.GetXmlStringAsObject(PackageXmlEnum.DublinCoreTermsNamespace),
                            _w3cdtf);
                    }

                    RecordNewBinding(xmlStringIndex, GetDateData(reader), true /*initializing*/, reader);
                }
                else  // An unexpected element is an error.
                {
                    Invariant.Assert(false, "Unknown value type for properties");
                }
            }
        }

        // This method validates xsi:type="dcterms:W3CDTF"
        // The valude of xsi:type is a qualified name. It should have a prefix that matches
        //  the xml namespace (ns) within the scope and the name that matches name
        // The comparisons should be case-sensitive comparisons
        internal static void ValidateXsiType(XmlTextReader reader, Object ns, string name)
        {
            // Get the value of xsi;type
            String typeValue = reader.GetAttribute(PackageXmlStringTable.GetXmlString(PackageXmlEnum.Type),
                                PackageXmlStringTable.GetXmlString(PackageXmlEnum.XmlSchemaInstanceNamespace));

            // Missing xsi:type
            if (typeValue == null)
            {
                throw new XmlException(SR.Get(SRID.UnknownDCDateTimeXsiType, reader.Name),
                    null, reader.LineNumber, reader.LinePosition);
            }

            int index = typeValue.IndexOf(':');

            // The valude of xsi:type is not a qualified name
            if (index == -1)
            {
                throw new XmlException(SR.Get(SRID.UnknownDCDateTimeXsiType, reader.Name),
                    null, reader.LineNumber, reader.LinePosition);
            }

            // Check the following conditions
            //  The namespace of the prefix (string before ":") matches "ns"
            //  The name (string after ":") matches "name"
            if (!Object.ReferenceEquals(ns, reader.LookupNamespace(typeValue.Substring(0, index)))
                    || String.CompareOrdinal(name, typeValue.Substring(index + 1, typeValue.Length - index - 1)) != 0)
            {
                throw new XmlException(SR.Get(SRID.UnknownDCDateTimeXsiType, reader.Name),
                    null, reader.LineNumber, reader.LinePosition);
            }
        }

        // Expect to find text data and return its value.
        private string GetStringData(XmlTextReader reader)
        {
            if (reader.IsEmptyElement)
                return string.Empty;

            reader.Read();
            if (reader.MoveToContent() == XmlNodeType.EndElement)
                return string.Empty;

            // If there is any content in the element, it should be text content and nothing else.
            if (reader.NodeType != XmlNodeType.Text)
                throw new XmlException(SR.Get(SRID.NoStructuredContentInsideProperties),
                    null, reader.LineNumber, reader.LinePosition);

            return reader.Value;
        }

        // Expect to find text data and return its value as DateTime.
        private Nullable<DateTime> GetDateData(XmlTextReader reader)
        {
            string data = GetStringData(reader);
            DateTime dateTime;

            try
            {
                // Note: No more than 7 second decimals are accepted by the
                // list of formats given. There currently is no method that
                // would perform XSD-compliant parsing.
                dateTime = XmlConvert.ToDateTime(data, s_dateTimeFormats);
            }
            catch (FormatException exc)
            {
                throw new XmlException(SR.Get(SRID.XsdDateTimeExpected),
                    exc, reader.LineNumber, reader.LinePosition);
            }
            return dateTime;
        }

        // Make sure there is a part to write to and that it contains
        // the expected start markup.
        private void EnsureXmlWriter()
        {
            // It does not make sense to reuse a writer outside of streaming creation.
            if (!_package.InStreamingCreation)
                Invariant.Assert(_xmlWriter == null);

            if (_xmlWriter != null)
                return;

            EnsurePropertyPart(); // Should succeed or throw an exception.

            Stream writerStream;

            if (_package.InStreamingCreation)
                writerStream = _propertyPart.GetStream(FileMode.Create, FileAccess.Write);
            else
                writerStream = new IgnoreFlushAndCloseStream(_propertyPart.GetStream(FileMode.Create, FileAccess.Write));

            _xmlWriter = new XmlTextWriter(writerStream, System.Text.Encoding.UTF8);

            WriteXmlStartTagsForPackageProperties();
        }

        // Create a property part if none exists yet.
        private void EnsurePropertyPart()
        {
            if (_propertyPart != null)
                return;

            // If _propertyPart is null, no property part existed when this object was created,
            // and this function is being called for the first time.
            // However, when read access is available, we can afford the luxury of checking whether
            // a property part and its referring relationship got correctly created in the meantime
            // outside of this class.
            // In write-only mode, it is impossible to perform this check, and the external creation
            // scenario will result in an exception being thrown.
            if (_package.FileOpenAccess == FileAccess.Read || _package.FileOpenAccess == FileAccess.ReadWrite)
            {
                _propertyPart = GetPropertyPart();
                if (_propertyPart != null)
                    return;
            }

            CreatePropertyPart();
        }

        // Create a new property relationship pointing to a new property part.
        // If only this class is used for manipulating property relationships, there cannot be a
        // pre-existing dangling property relationship.
        // No check is performed here for other classes getting misused insofar as this function
        // has to work in write-only mode.
        private void CreatePropertyPart()
        {
            _propertyPart = _package.CreatePart(GeneratePropertyPartUri(), s_coreDocumentPropertiesContentType.ToString());
            _package.CreateRelationship(_propertyPart.Uri, TargetMode.Internal,
                _coreDocumentPropertiesRelationshipType);
        }

        private Uri GeneratePropertyPartUri()
        {
            string propertyPartName = _defaultPropertyPartNamePrefix
                + Guid.NewGuid().ToString(_guidStorageFormatString, (IFormatProvider)null)
                + _defaultPropertyPartNameExtension;

            return PackUriHelper.CreatePartUri(new Uri(propertyPartName, UriKind.Relative));
        }

        private void WriteXmlStartTagsForPackageProperties()
        {
            _xmlWriter.WriteStartDocument();

            // <coreProperties
            _xmlWriter.WriteStartElement(PackageXmlStringTable.GetXmlString(PackageXmlEnum.CoreProperties),              // local name
                                         PackageXmlStringTable.GetXmlString(PackageXmlEnum.PackageCorePropertiesNamespace));                                      // namespace

            // xmlns:dc
            _xmlWriter.WriteAttributeString(PackageXmlStringTable.GetXmlString(PackageXmlEnum.XmlNamespacePrefix),
                                            PackageXmlStringTable.GetXmlString(PackageXmlEnum.DublinCorePropertiesNamespacePrefix),
                                            null,
                                            PackageXmlStringTable.GetXmlString(PackageXmlEnum.DublinCorePropertiesNamespace));

            // xmlns:dcterms
            _xmlWriter.WriteAttributeString(PackageXmlStringTable.GetXmlString(PackageXmlEnum.XmlNamespacePrefix),
                                            PackageXmlStringTable.GetXmlString(PackageXmlEnum.DublincCoreTermsNamespacePrefix),
                                            null,
                                            PackageXmlStringTable.GetXmlString(PackageXmlEnum.DublinCoreTermsNamespace));
            // xmlns:xsi
            _xmlWriter.WriteAttributeString(PackageXmlStringTable.GetXmlString(PackageXmlEnum.XmlNamespacePrefix),
                                            PackageXmlStringTable.GetXmlString(PackageXmlEnum.XmlSchemaInstanceNamespacePrefix),
                                            null,
                                            PackageXmlStringTable.GetXmlString(PackageXmlEnum.XmlSchemaInstanceNamespace));
        }

        // Write the property elements and clear _dirty.
        private void SerializeDirtyProperties()
        {
            // In streaming mode, nullify dictionary values.
            // As no property can be set to null through the API, this makes it possible to keep
            // track of all properties that have been set since the CoreProperties object was created.
            KeyValuePair<PackageXmlEnum, Object>[] entriesToNullify = null;
            int numEntriesToNullify = 0;
            if (_package.InStreamingCreation)
                entriesToNullify = new KeyValuePair<PackageXmlEnum, Object>[_propertyDictionary.Count];

            // Create a property element for each non-null entry.
            foreach (KeyValuePair<PackageXmlEnum, Object> entry in _propertyDictionary)
            {
                // If we are NOT in streaming mode, the property value should NOT be null
                Debug.Assert(entry.Value != null || _package.InStreamingCreation);

                if (_package.InStreamingCreation
                    && entry.Value == null) // already saved
                {
                    continue;
                }


                PackageXmlEnum propertyNamespace = PackageXmlStringTable.GetXmlNamespace(entry.Key);

                _xmlWriter.WriteStartElement(PackageXmlStringTable.GetXmlString(entry.Key),
                                                PackageXmlStringTable.GetXmlString(propertyNamespace));

                if (entry.Value is Nullable<DateTime>)
                {
                    if (propertyNamespace == PackageXmlEnum.DublinCoreTermsNamespace)
                    {
                        // xsi:type=
                        _xmlWriter.WriteStartAttribute(PackageXmlStringTable.GetXmlString(PackageXmlEnum.Type),
                                                        PackageXmlStringTable.GetXmlString(PackageXmlEnum.XmlSchemaInstanceNamespace));

                        // "dcterms:W3CDTF"
                        _xmlWriter.WriteQualifiedName(_w3cdtf,
                                                        PackageXmlStringTable.GetXmlString(PackageXmlEnum.DublinCoreTermsNamespace));

                        _xmlWriter.WriteEndAttribute();
                    }

                    // Use sortable ISO 8601 date/time pattern. Include second fractions down to the 100-nanosecond interval,
                    // which is the definition of a "tick" for the DateTime type.
                    _xmlWriter.WriteString(XmlConvert.ToString(((Nullable<DateTime>)entry.Value).Value.ToUniversalTime(), "yyyy-MM-ddTHH:mm:ss.fffffffZ"));
                }
                else
                {
                    // The following uses the fact that ToString is virtual.
                    _xmlWriter.WriteString(entry.Value.ToString());
                }

                _xmlWriter.WriteEndElement();

                if (_package.InStreamingCreation)
                    entriesToNullify[numEntriesToNullify++] = entry;
            }

            // Mark properties as saved.
            _dirty = false;

            // Detailed marking of saved properties for the streaming mode.
            if (_package.InStreamingCreation)
            {
                for (int i = 0; i < numEntriesToNullify; ++i)
                {
                    _propertyDictionary[entriesToNullify[i].Key] = null;
                }
            }
        }

        // Add end markup and close the writer.
        private void CloseXmlWriter()
        {
            // Close the root element.
            _xmlWriter.WriteEndElement();

            // Close the writer itself.
            _xmlWriter.Close();

            // Make sure we know it's closed.
            _xmlWriter = null;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //   Private fields
        //
        //------------------------------------------------------

        #region Private Fields

        private Package _package;
        private PackagePart _propertyPart;
        private XmlTextWriter _xmlWriter;

        // Table of objects from the closed set of literals defined below.
        // (Uses object comparison rather than string comparison.)
        private const int _numCoreProperties = 16;
        private Dictionary<PackageXmlEnum, Object> _propertyDictionary = new Dictionary<PackageXmlEnum, Object>(_numCoreProperties);
        private bool _dirty = false;

        // This System.Xml.NameTable makes sure that we use the same references to strings
        // throughout (including when parsing Xml) and so can perform reference comparisons
        // rather than value comparisons.
        private NameTable _nameTable;

        // Literals.
        private static readonly ContentType s_coreDocumentPropertiesContentType
            = new ContentType("application/vnd.openxmlformats-package.core-properties+xml");
        private const string _coreDocumentPropertiesRelationshipType
            = "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties";

        private const string _defaultPropertyPartNamePrefix =
            "/package/services/metadata/core-properties/";
        private const string _w3cdtf = "W3CDTF";
        private const string _defaultPropertyPartNameExtension = ".psmdcp";
        private const string _guidStorageFormatString = @"N";     // N - simple format without adornments

        private static PackageXmlEnum[] s_validProperties = new PackageXmlEnum[] {
                                                                                PackageXmlEnum.Creator,
                                                                                PackageXmlEnum.Identifier,
                                                                                PackageXmlEnum.Title,
                                                                                PackageXmlEnum.Subject,
                                                                                PackageXmlEnum.Description,
                                                                                PackageXmlEnum.Language,
                                                                                PackageXmlEnum.Created,
                                                                                PackageXmlEnum.Modified,
                                                                                PackageXmlEnum.ContentType,
                                                                                PackageXmlEnum.Keywords,
                                                                                PackageXmlEnum.Category,
                                                                                PackageXmlEnum.Version,
                                                                                PackageXmlEnum.LastModifiedBy,
                                                                                PackageXmlEnum.ContentStatus,
                                                                                PackageXmlEnum.Revision,
                                                                                PackageXmlEnum.LastPrinted
                                                                                };

        // Array of formats to supply to XmlConvert.ToDateTime or DateTime.ParseExact.
        // xsd:DateTime requires full date time in sortable (ISO 8601) format.
        // It can be expressed in local time, universal time (Z), or relative to universal time (zzz).
        // Negative years are accepted. 
        // IMPORTANT: Second fractions are recognized only down to 1 tenth of a microsecond because this is the resolution
        // of the DateTime type. The Xml standard, however, allows any number of decimals; but XmlConvert only offers
        // this very awkward API with an explicit pattern enumeration.
        private static readonly string[] s_dateTimeFormats = new string[] {
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:sszzz",
            @"\-yyyy-MM-ddTHH:mm:ss",
            @"\-yyyy-MM-ddTHH:mm:ssZ",
            @"\-yyyy-MM-ddTHH:mm:sszzz",

            "yyyy-MM-ddTHH:mm:ss.ff",
            "yyyy-MM-ddTHH:mm:ss.fZ",
            "yyyy-MM-ddTHH:mm:ss.fzzz",
            @"\-yyyy-MM-ddTHH:mm:ss.f",
            @"\-yyyy-MM-ddTHH:mm:ss.fZ",
            @"\-yyyy-MM-ddTHH:mm:ss.fzzz",

            "yyyy-MM-ddTHH:mm:ss.ff",
            "yyyy-MM-ddTHH:mm:ss.ffZ",
            "yyyy-MM-ddTHH:mm:ss.ffzzz",
            @"\-yyyy-MM-ddTHH:mm:ss.ff",
            @"\-yyyy-MM-ddTHH:mm:ss.ffZ",
            @"\-yyyy-MM-ddTHH:mm:ss.ffzzz",

            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "yyyy-MM-ddTHH:mm:ss.fffzzz",
            @"\-yyyy-MM-ddTHH:mm:ss.fff",
            @"\-yyyy-MM-ddTHH:mm:ss.fffZ",
            @"\-yyyy-MM-ddTHH:mm:ss.fffzzz",

            "yyyy-MM-ddTHH:mm:ss.ffff",
            "yyyy-MM-ddTHH:mm:ss.ffffZ",
            "yyyy-MM-ddTHH:mm:ss.ffffzzz",
            @"\-yyyy-MM-ddTHH:mm:ss.ffff",
            @"\-yyyy-MM-ddTHH:mm:ss.ffffZ",
            @"\-yyyy-MM-ddTHH:mm:ss.ffffzzz",

            "yyyy-MM-ddTHH:mm:ss.fffff",
            "yyyy-MM-ddTHH:mm:ss.fffffZ",
            "yyyy-MM-ddTHH:mm:ss.fffffzzz",
            @"\-yyyy-MM-ddTHH:mm:ss.fffff",
            @"\-yyyy-MM-ddTHH:mm:ss.fffffZ",
            @"\-yyyy-MM-ddTHH:mm:ss.fffffzzz",

            "yyyy-MM-ddTHH:mm:ss.ffffff",
            "yyyy-MM-ddTHH:mm:ss.ffffffZ",
            "yyyy-MM-ddTHH:mm:ss.ffffffzzz",
            @"\-yyyy-MM-ddTHH:mm:ss.ffffff",
            @"\-yyyy-MM-ddTHH:mm:ss.ffffffZ",
            @"\-yyyy-MM-ddTHH:mm:ss.ffffffzzz",

            "yyyy-MM-ddTHH:mm:ss.fffffff",
            "yyyy-MM-ddTHH:mm:ss.fffffffZ",
            "yyyy-MM-ddTHH:mm:ss.fffffffzzz",
            @"\-yyyy-MM-ddTHH:mm:ss.fffffff",
            @"\-yyyy-MM-ddTHH:mm:ss.fffffffZ",
            @"\-yyyy-MM-ddTHH:mm:ss.fffffffzzz",
        };

        #endregion Private Fields
    }
}

