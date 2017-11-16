// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Globalization;

namespace System.IO.Packaging
{
    /// <summary>
    ///  The package properties are a subset of the standard OLE property sets
    ///  SummaryInformation and DocumentSummaryInformation, and include such properties
    ///  as Title and Subject.
    /// </summary>
    /// <remarks>
    /// <para>Setting a property to null deletes this property. 'null' is never strictly speaking
    /// a property value, but an absence indicator.</para>
    /// </remarks>
    internal class PartBasedPackageProperties : PackageProperties
    {
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
        
        #region Internal Methods

        // Invoked from Package.Flush.
        // The expectation is that whatever is currently dirty will get flushed.
        internal void Flush()
        {
            if (!_dirty)
                return;

            // Make sure there is a part to write to and that it contains
            // the expected start markup.
            EnsureXmlWriter();

            // Write the property elements and clear _dirty.
            SerializeDirtyProperties();

            // add closing markup and close the writer.
            CloseXmlWriter();
        }

        // Invoked from Package.Close.
        internal void Close()
        {
            Flush();
        }

        #endregion Internal Methods
        
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
        private void RecordNewBinding(PackageXmlEnum propertyenum, object value, bool initializing, XmlReader reader)
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
                    throw new XmlException(SR.Format(SR.DuplicateCorePropertyName, reader.Name),
                        null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
                }

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
            // Case of an initial value being set for a property. If value is null, no need to do anything
            else if (value != null)
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
            Debug.Assert(_propertyPart == null); // This gets called exclusively from constructor.

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
                throw new FileFormatException(SR.NoExternalTargetForMetadataRelationship);

            PackagePart propertiesPart = null;
            Uri propertiesPartUri = PackUriHelper.ResolvePartUri(
                PackUriHelper.PackageRootUri,
                corePropertiesRelationship.TargetUri);

            if (!_package.PartExists(propertiesPartUri))
                throw new FileFormatException(SR.DanglingMetadataRelationship);

            propertiesPart = _package.GetPart(propertiesPartUri);
            if (!propertiesPart.ValidatedContentType.AreTypeAndSubTypeEqual(s_coreDocumentPropertiesContentType))
            {
                throw new FileFormatException(SR.WrongContentTypeForPropertyPart);
            }

            return propertiesPart;
        }

        // Find a package-wide relationship of type CoreDocumentPropertiesRelationshipType.
        private PackageRelationship GetCorePropertiesRelationship()
        {
            PackageRelationship propertiesPartRelationship = null;
            foreach (PackageRelationship rel
                in _package.GetRelationshipsByType(CoreDocumentPropertiesRelationshipType))
            {
                if (propertiesPartRelationship != null)
                {
                    throw new FileFormatException(SR.MoreThanOneMetadataRelationships);
                }
                propertiesPartRelationship = rel;
            }
            return propertiesPartRelationship;
        }

        // Deserialize properties part.
        private void ParseCorePropertyPart(PackagePart part)
        {
            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.NameTable = _nameTable;
            using (Stream stream = part.GetStream(FileMode.Open, FileAccess.Read))

            // Create a reader that uses _nameTable so as to use the set of tag literals
            // in effect as a set of atomic identifiers.
            using (XmlReader reader = XmlReader.Create(stream, xrs))
            {
                //This method expects the reader to be in ReadState.Initial.
                //It will make the first read call.
                PackagingUtilities.PerformInitialReadAndVerifyEncoding(reader);

                //Note: After the previous method call the reader should be at the first tag in the markup.
                //MoveToContent - Skips over the following - ProcessingInstruction, DocumentType, Comment, Whitespace, or SignificantWhitespace
                //If the reader is currently at a content node then this function call is a no-op
                if (reader.MoveToContent() != XmlNodeType.Element
                    || (object)reader.NamespaceURI != PackageXmlStringTable.GetXmlStringAsObject(PackageXmlEnum.PackageCorePropertiesNamespace)
                    || (object)reader.LocalName != PackageXmlStringTable.GetXmlStringAsObject(PackageXmlEnum.CoreProperties))
                {
                    throw new XmlException(SR.CorePropertiesElementExpected,
                        null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
                }

                // The schema is closed and defines no attributes on the root element.
                if (PackagingUtilities.GetNonXmlnsAttributeCount(reader) != 0)
                {
                    throw new XmlException(SR.Format(SR.PropertyWrongNumbOfAttribsDefinedOn, reader.Name),
                        null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
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
                    {
                        throw new XmlException(SR.PropertyStartTagExpected,
                            null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
                    }

                    // Any element below the root should open at level 1 exclusively.
                    if (reader.Depth != 1)
                    {
                        throw new XmlException(SR.NoStructuredContentInsideProperties,
                            null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
                    }

                    attributesCount = PackagingUtilities.GetNonXmlnsAttributeCount(reader);

                    // Property elements can occur in any order (xsd:all).
                    object localName = reader.LocalName;
                    PackageXmlEnum xmlStringIndex = PackageXmlStringTable.GetEnumOf(localName);
                    String valueType = PackageXmlStringTable.GetValueType(xmlStringIndex);

                    if (Array.IndexOf(s_validProperties, xmlStringIndex) == -1)  // An unexpected element is an error.
                    {
                        throw new XmlException(
                            SR.Format(SR.InvalidPropertyNameInCorePropertiesPart, reader.LocalName),
                            null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
                    }

                    // Any element not in the valid core properties namespace is unexpected.
                    // The following is an object comparison, not a string comparison.
                    if ((object)reader.NamespaceURI != PackageXmlStringTable.GetXmlStringAsObject(PackageXmlStringTable.GetXmlNamespace(xmlStringIndex)))
                    {
                        throw new XmlException(SR.UnknownNamespaceInCorePropertiesPart,
                            null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
                    }

                    if (String.CompareOrdinal(valueType, "String") == 0)
                    {
                        // The schema is closed and defines no attributes on this type of element.
                        if (attributesCount != 0)
                        {
                            throw new XmlException(SR.Format(SR.PropertyWrongNumbOfAttribsDefinedOn, reader.Name),
                                null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
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
                            throw new XmlException(SR.Format(SR.PropertyWrongNumbOfAttribsDefinedOn, reader.Name),
                                null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
                        }

                        if (allowedAttributeCount != 0)
                        {
                            ValidateXsiType(reader,
                                PackageXmlStringTable.GetXmlStringAsObject(PackageXmlEnum.DublinCoreTermsNamespace),
                                W3cdtf);
                        }

                        RecordNewBinding(xmlStringIndex, GetDateData(reader), true /*initializing*/, reader);
                    }
                    else  // An unexpected element is an error.
                    {
                        Debug.Assert(false, "Unknown value type for properties");
                    }
                }
            }
        }

        // This method validates xsi:type="dcterms:W3CDTF"
        // The value of xsi:type is a qualified name. It should have a prefix that matches
        //  the xml namespace (ns) within the scope and the name that matches name
        // The comparisons should be case-sensitive comparisons
        internal static void ValidateXsiType(XmlReader reader, Object ns, string name)
        {
            // Get the value of xsi;type
            String typeValue = reader.GetAttribute(PackageXmlStringTable.GetXmlString(PackageXmlEnum.Type),
                                PackageXmlStringTable.GetXmlString(PackageXmlEnum.XmlSchemaInstanceNamespace));

            // Missing xsi:type
            if (typeValue == null)
            {
                throw new XmlException(SR.Format(SR.UnknownDCDateTimeXsiType, reader.Name),
                    null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
            }

            int index = typeValue.IndexOf(':');

            // The value of xsi:type is not a qualified name
            if (index == -1)
            {
                throw new XmlException(SR.Format(SR.UnknownDCDateTimeXsiType, reader.Name),
                    null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
            }

            // Check the following conditions
            //  The namespace of the prefix (string before ":") matches "ns"
            //  The name (string after ":") matches "name"
            if (!Object.ReferenceEquals(ns, reader.LookupNamespace(typeValue.Substring(0, index)))
                    || String.CompareOrdinal(name, typeValue.Substring(index + 1, typeValue.Length - index - 1)) != 0)
            {
                throw new XmlException(SR.Format(SR.UnknownDCDateTimeXsiType, reader.Name),
                    null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
            }
        }

        // Expect to find text data and return its value.
        private string GetStringData(XmlReader reader)
        {
            if (reader.IsEmptyElement)
                return string.Empty;

            reader.Read();
            if (reader.MoveToContent() == XmlNodeType.EndElement)
                return string.Empty;

            // If there is any content in the element, it should be text content and nothing else.
            if (reader.NodeType != XmlNodeType.Text)
            {
                throw new XmlException(SR.NoStructuredContentInsideProperties,
                    null, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
            }

            return reader.Value;
        }

        // Expect to find text data and return its value as DateTime.
        private Nullable<DateTime> GetDateData(XmlReader reader)
        {
            string data = GetStringData(reader);
            DateTime dateTime;

            try
            {
                // Note: No more than 7 second decimals are accepted by the
                // list of formats given. There currently is no method that
                // would perform XSD-compliant parsing.
                dateTime = DateTime.ParseExact(data, s_dateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None);
            }
            catch (FormatException exc)
            {
                throw new XmlException(SR.XsdDateTimeExpected,
                    exc, ((IXmlLineInfo)reader).LineNumber, ((IXmlLineInfo)reader).LinePosition);
            }
            return dateTime;
        }

        // Make sure there is a part to write to and that it contains
        // the expected start markup.
        private void EnsureXmlWriter()
        {
            if (_xmlWriter != null)
                return;

            EnsurePropertyPart(); // Should succeed or throw an exception.

            Stream writerStream = new IgnoreFlushAndCloseStream(_propertyPart.GetStream(FileMode.Create, FileAccess.Write));
            _xmlWriter = XmlWriter.Create(writerStream, new XmlWriterSettings { Encoding = System.Text.Encoding.UTF8 });
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
                CoreDocumentPropertiesRelationshipType);
        }

        private Uri GeneratePropertyPartUri()
        {
            string propertyPartName = DefaultPropertyPartNamePrefix
                + Guid.NewGuid().ToString(GuidStorageFormatString)
                + DefaultPropertyPartNameExtension;

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
            // Create a property element for each non-null entry.
            foreach (KeyValuePair<PackageXmlEnum, Object> entry in _propertyDictionary)
            {
                Debug.Assert(entry.Value != null);

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
                        _xmlWriter.WriteQualifiedName(W3cdtf,
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
            }

            // Mark properties as saved.
            _dirty = false;
        }

        // Add end markup and close the writer.
        private void CloseXmlWriter()
        {
            // Close the root element.
            _xmlWriter.WriteEndElement();

            // Close the writer itself.
            _xmlWriter.Dispose();

            // Make sure we know it's closed.
            _xmlWriter = null;
        }

        #endregion Private Methods
        
        #region Private Fields

        private Package _package;
        private PackagePart _propertyPart;
        private XmlWriter _xmlWriter;

        // Table of objects from the closed set of literals defined below.
        // (Uses object comparison rather than string comparison.)
        private const int NumCoreProperties = 16;
        private Dictionary<PackageXmlEnum, Object> _propertyDictionary = new Dictionary<PackageXmlEnum, Object>(NumCoreProperties);
        private bool _dirty = false;

        // This System.Xml.NameTable makes sure that we use the same references to strings
        // throughout (including when parsing Xml) and so can perform reference comparisons
        // rather than value comparisons.
        private NameTable _nameTable;

        // Literals.
        private static readonly ContentType s_coreDocumentPropertiesContentType
            = new ContentType("application/vnd.openxmlformats-package.core-properties+xml");
        private const string CoreDocumentPropertiesRelationshipType
            = "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties";

        private const string DefaultPropertyPartNamePrefix =
            "/package/services/metadata/core-properties/";
        private const string W3cdtf = "W3CDTF";
        private const string DefaultPropertyPartNameExtension = ".psmdcp";
        private const string GuidStorageFormatString = @"N";     // N - simple format without adornments

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
