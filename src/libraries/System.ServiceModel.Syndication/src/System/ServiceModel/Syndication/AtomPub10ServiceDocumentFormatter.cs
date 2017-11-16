// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.ServiceModel.Channels;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.ServiceModel.Syndication
{
    internal delegate InlineCategoriesDocument CreateInlineCategoriesDelegate();
    internal delegate ReferencedCategoriesDocument CreateReferencedCategoriesDelegate();

    [XmlRoot(ElementName = App10Constants.Service, Namespace = App10Constants.Namespace)]
    public class AtomPub10ServiceDocumentFormatter : ServiceDocumentFormatter, IXmlSerializable
    {
        private Type _documentType;
        private int _maxExtensionSize;
        private bool _preserveAttributeExtensions;
        private bool _preserveElementExtensions;

        public AtomPub10ServiceDocumentFormatter()
            : this(typeof(ServiceDocument))
        {
        }

        public AtomPub10ServiceDocumentFormatter(Type documentTypeToCreate)
            : base()
        {
            if (documentTypeToCreate == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("documentTypeToCreate");
            }
            if (!typeof(ServiceDocument).IsAssignableFrom(documentTypeToCreate))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("documentTypeToCreate",
                    SR.Format(SR.InvalidObjectTypePassed, "documentTypeToCreate", "ServiceDocument"));
            }
            _maxExtensionSize = int.MaxValue;
            _preserveAttributeExtensions = true;
            _preserveElementExtensions = true;
            _documentType = documentTypeToCreate;
        }

        public AtomPub10ServiceDocumentFormatter(ServiceDocument documentToWrite)
            : base(documentToWrite)
        {
            // No need to check that the parameter passed is valid - it is checked by the c'tor of the base class
            _maxExtensionSize = int.MaxValue;
            _preserveAttributeExtensions = true;
            _preserveElementExtensions = true;
            _documentType = documentToWrite.GetType();
        }

        public override string Version
        {
            get { return App10Constants.Namespace; }
        }

        public override bool CanRead(XmlReader reader)
        {
            if (reader == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("reader");
            }
            return reader.IsStartElement(App10Constants.Service, App10Constants.Namespace);
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "The IXmlSerializable implementation is only for exposing under WCF DataContractSerializer. The funcionality is exposed to derived class through the ReadFrom\\WriteTo methods")]
        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "The IXmlSerializable implementation is only for exposing under WCF DataContractSerializer. The funcionality is exposed to derived class through the ReadFrom\\WriteTo methods")]
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (reader == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("reader");
            }
            TraceServiceDocumentReadBegin();
            ReadDocument(reader);
            TraceServiceDocumentReadEnd();
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "The IXmlSerializable implementation is only for exposing under WCF DataContractSerializer. The funcionality is exposed to derived class through the ReadFrom\\WriteTo methods")]
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("writer");
            }
            if (this.Document == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.DocumentFormatterDoesNotHaveDocument)));
            }
            TraceServiceDocumentWriteBegin();
            WriteDocument(writer);
            TraceServiceDocumentWriteEnd();
        }

        public override void ReadFrom(XmlReader reader)
        {
            if (reader == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("reader");
            }
            reader.MoveToContent();
            if (!CanRead(reader))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(SR.Format(SR.UnknownDocumentXml, reader.LocalName, reader.NamespaceURI)));
            }
            TraceServiceDocumentReadBegin();
            ReadDocument(reader);
            TraceServiceDocumentReadEnd();
        }

        public override void WriteTo(XmlWriter writer)
        {
            if (writer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("writer");
            }
            if (this.Document == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.DocumentFormatterDoesNotHaveDocument)));
            }
            TraceServiceDocumentWriteBegin();
            writer.WriteStartElement(App10Constants.Prefix, App10Constants.Service, App10Constants.Namespace);
            WriteDocument(writer);
            writer.WriteEndElement();
            TraceServiceDocumentWriteEnd();
        }

        internal static CategoriesDocument ReadCategories(XmlReader reader, Uri baseUri, CreateInlineCategoriesDelegate inlineCategoriesFactory, CreateReferencedCategoriesDelegate referencedCategoriesFactory, string version, bool preserveElementExtensions, bool preserveAttributeExtensions, int maxExtensionSize)
        {
            string link = reader.GetAttribute(App10Constants.Href, string.Empty);
            if (string.IsNullOrEmpty(link))
            {
                InlineCategoriesDocument inlineCategories = inlineCategoriesFactory();
                ReadInlineCategories(reader, inlineCategories, baseUri, version, preserveElementExtensions, preserveAttributeExtensions, maxExtensionSize);
                return inlineCategories;
            }
            else
            {
                ReferencedCategoriesDocument referencedCategories = referencedCategoriesFactory();
                ReadReferencedCategories(reader, referencedCategories, baseUri, new Uri(link, UriKind.RelativeOrAbsolute), version, preserveElementExtensions, preserveAttributeExtensions, maxExtensionSize);
                return referencedCategories;
            }
        }

        internal static void TraceServiceDocumentReadBegin()
        {
        }

        internal static void TraceServiceDocumentReadEnd()
        {
        }

        internal static void TraceServiceDocumentWriteBegin()
        {
        }

        internal static void TraceServiceDocumentWriteEnd()
        {
        }

        internal static void WriteCategoriesInnerXml(XmlWriter writer, CategoriesDocument categories, Uri baseUri, string version)
        {
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(baseUri, categories.BaseUri);
            if (baseUriToWrite != null)
            {
                WriteXmlBase(writer, baseUriToWrite);
            }
            if (!string.IsNullOrEmpty(categories.Language))
            {
                WriteXmlLang(writer, categories.Language);
            }
            if (categories.IsInline)
            {
                WriteInlineCategoriesContent(writer, (InlineCategoriesDocument)categories, version);
            }
            else
            {
                WriteReferencedCategoriesContent(writer, (ReferencedCategoriesDocument)categories, version);
            }
        }

        protected override ServiceDocument CreateDocumentInstance()
        {
            if (_documentType == typeof(ServiceDocument))
            {
                return new ServiceDocument();
            }
            else
            {
                return (ServiceDocument)Activator.CreateInstance(_documentType);
            }
        }

        private static void ReadInlineCategories(XmlReader reader, InlineCategoriesDocument inlineCategories, Uri baseUri, string version, bool preserveElementExtensions, bool preserveAttributeExtensions, int maxExtensionSize)
        {
            inlineCategories.BaseUri = baseUri;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.LocalName == "base" && reader.NamespaceURI == Atom10FeedFormatter.XmlNs)
                    {
                        inlineCategories.BaseUri = FeedUtils.CombineXmlBase(inlineCategories.BaseUri, reader.Value);
                    }
                    else if (reader.LocalName == "lang" && reader.NamespaceURI == Atom10FeedFormatter.XmlNs)
                    {
                        inlineCategories.Language = reader.Value;
                    }
                    else if (reader.LocalName == App10Constants.Fixed && reader.NamespaceURI == string.Empty)
                    {
                        inlineCategories.IsFixed = (reader.Value == "yes");
                    }
                    else if (reader.LocalName == Atom10Constants.SchemeTag && reader.NamespaceURI == string.Empty)
                    {
                        inlineCategories.Scheme = reader.Value;
                    }
                    else
                    {
                        string ns = reader.NamespaceURI;
                        string name = reader.LocalName;
                        if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
                        {
                            continue;
                        }
                        string val = reader.Value;
                        if (!TryParseAttribute(name, ns, val, inlineCategories, version))
                        {
                            if (preserveAttributeExtensions)
                            {
                                inlineCategories.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), reader.Value);
                            }
                            else
                            {
                                SyndicationFeedFormatter.TraceSyndicationElementIgnoredOnRead(reader);
                            }
                        }
                    }
                }
            }
            SyndicationFeedFormatter.MoveToStartElement(reader);
            bool isEmptyElement = reader.IsEmptyElement;
            reader.ReadStartElement();
            if (!isEmptyElement)
            {
                XmlBuffer buffer = null;
                XmlDictionaryWriter extWriter = null;
                try
                {
                    while (reader.IsStartElement())
                    {
                        if (reader.IsStartElement(Atom10Constants.CategoryTag, Atom10Constants.Atom10Namespace))
                        {
                            SyndicationCategory category = CreateCategory(inlineCategories);
                            Atom10FeedFormatter.ReadCategory(reader, category, version, preserveAttributeExtensions, preserveElementExtensions, maxExtensionSize);
                            if (category.Scheme == null)
                            {
                                category.Scheme = inlineCategories.Scheme;
                            }
                            inlineCategories.Categories.Add(category);
                        }
                        else if (!TryParseElement(reader, inlineCategories, version))
                        {
                            if (preserveElementExtensions)
                            {
                                SyndicationFeedFormatter.CreateBufferIfRequiredAndWriteNode(ref buffer, ref extWriter, reader, maxExtensionSize);
                            }
                            else
                            {
                                SyndicationFeedFormatter.TraceSyndicationElementIgnoredOnRead(reader);
                                reader.Skip();
                            }
                        }
                    }
                    LoadElementExtensions(buffer, extWriter, inlineCategories);
                }
                finally
                {
                    if (extWriter != null)
                    {
                        extWriter.Close();
                    }
                }
                reader.ReadEndElement();
            }
        }

        private static void ReadReferencedCategories(XmlReader reader, ReferencedCategoriesDocument referencedCategories, Uri baseUri, Uri link, string version, bool preserveElementExtensions, bool preserveAttributeExtensions, int maxExtensionSize)
        {
            referencedCategories.BaseUri = baseUri;
            referencedCategories.Link = link;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.LocalName == "base" && reader.NamespaceURI == Atom10FeedFormatter.XmlNs)
                    {
                        referencedCategories.BaseUri = FeedUtils.CombineXmlBase(referencedCategories.BaseUri, reader.Value);
                    }
                    else if (reader.LocalName == "lang" && reader.NamespaceURI == Atom10FeedFormatter.XmlNs)
                    {
                        referencedCategories.Language = reader.Value;
                    }
                    else if (reader.LocalName == App10Constants.Href && reader.NamespaceURI == string.Empty)
                    {
                        continue;
                    }
                    else
                    {
                        string ns = reader.NamespaceURI;
                        string name = reader.LocalName;
                        if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
                        {
                            continue;
                        }
                        string val = reader.Value;
                        if (!TryParseAttribute(name, ns, val, referencedCategories, version))
                        {
                            if (preserveAttributeExtensions)
                            {
                                referencedCategories.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), reader.Value);
                            }
                            else
                            {
                                SyndicationFeedFormatter.TraceSyndicationElementIgnoredOnRead(reader);
                            }
                        }
                    }
                }
            }
            reader.MoveToElement();
            bool isEmptyElement = reader.IsEmptyElement;
            reader.ReadStartElement();
            if (!isEmptyElement)
            {
                XmlBuffer buffer = null;
                XmlDictionaryWriter extWriter = null;
                try
                {
                    while (reader.IsStartElement())
                    {
                        if (!TryParseElement(reader, referencedCategories, version))
                        {
                            if (preserveElementExtensions)
                            {
                                SyndicationFeedFormatter.CreateBufferIfRequiredAndWriteNode(ref buffer, ref extWriter, reader, maxExtensionSize);
                            }
                            else
                            {
                                SyndicationFeedFormatter.TraceSyndicationElementIgnoredOnRead(reader);
                                reader.Skip();
                            }
                        }
                    }
                    LoadElementExtensions(buffer, extWriter, referencedCategories);
                }
                finally
                {
                    if (extWriter != null)
                    {
                        extWriter.Close();
                    }
                }
                reader.ReadEndElement();
            }
        }

        private static void WriteCategories(XmlWriter writer, CategoriesDocument categories, Uri baseUri, string version)
        {
            writer.WriteStartElement(App10Constants.Prefix, App10Constants.Categories, App10Constants.Namespace);
            WriteCategoriesInnerXml(writer, categories, baseUri, version);
            writer.WriteEndElement();
        }

        private static void WriteInlineCategoriesContent(XmlWriter writer, InlineCategoriesDocument categories, string version)
        {
            if (!string.IsNullOrEmpty(categories.Scheme))
            {
                writer.WriteAttributeString(Atom10Constants.SchemeTag, categories.Scheme);
            }
            // by default, categories are not fixed
            if (categories.IsFixed)
            {
                writer.WriteAttributeString(App10Constants.Fixed, "yes");
            }
            WriteAttributeExtensions(writer, categories, version);
            for (int i = 0; i < categories.Categories.Count; ++i)
            {
                Atom10FeedFormatter.WriteCategory(writer, categories.Categories[i], version);
            }
            WriteElementExtensions(writer, categories, version);
        }

        private static void WriteReferencedCategoriesContent(XmlWriter writer, ReferencedCategoriesDocument categories, string version)
        {
            if (categories.Link != null)
            {
                writer.WriteAttributeString(App10Constants.Href, FeedUtils.GetUriString(categories.Link));
            }
            WriteAttributeExtensions(writer, categories, version);
            WriteElementExtensions(writer, categories, version);
        }

        private static void WriteXmlBase(XmlWriter writer, Uri baseUri)
        {
            writer.WriteAttributeString("xml", "base", Atom10FeedFormatter.XmlNs, FeedUtils.GetUriString(baseUri));
        }

        private static void WriteXmlLang(XmlWriter writer, string lang)
        {
            writer.WriteAttributeString("xml", "lang", Atom10FeedFormatter.XmlNs, lang);
        }

        private ResourceCollectionInfo ReadCollection(XmlReader reader, Workspace workspace)
        {
            ResourceCollectionInfo result = CreateCollection(workspace);
            result.BaseUri = workspace.BaseUri;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.LocalName == "base" && reader.NamespaceURI == Atom10FeedFormatter.XmlNs)
                    {
                        result.BaseUri = FeedUtils.CombineXmlBase(result.BaseUri, reader.Value);
                    }
                    else if (reader.LocalName == App10Constants.Href && reader.NamespaceURI == string.Empty)
                    {
                        result.Link = new Uri(reader.Value, UriKind.RelativeOrAbsolute);
                    }
                    else
                    {
                        string ns = reader.NamespaceURI;
                        string name = reader.LocalName;
                        if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
                        {
                            continue;
                        }
                        string val = reader.Value;
                        if (!TryParseAttribute(name, ns, val, result, this.Version))
                        {
                            if (_preserveAttributeExtensions)
                            {
                                result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), reader.Value);
                            }
                            else
                            {
                                SyndicationFeedFormatter.TraceSyndicationElementIgnoredOnRead(reader);
                            }
                        }
                    }
                }
            }

            XmlBuffer buffer = null;
            XmlDictionaryWriter extWriter = null;

            reader.ReadStartElement();
            try
            {
                while (reader.IsStartElement())
                {
                    if (reader.IsStartElement(Atom10Constants.TitleTag, Atom10Constants.Atom10Namespace))
                    {
                        result.Title = Atom10FeedFormatter.ReadTextContentFrom(reader, "//app:service/app:workspace/app:collection/atom:title[@type]", _preserveAttributeExtensions);
                    }
                    else if (reader.IsStartElement(App10Constants.Categories, App10Constants.Namespace))
                    {
                        result.Categories.Add(ReadCategories(reader,
                            result.BaseUri,
                            delegate ()
                            {
                                return CreateInlineCategories(result);
                            },

                            delegate ()
                            {
                                return CreateReferencedCategories(result);
                            },
                            this.Version,
                            _preserveElementExtensions,
                            _preserveAttributeExtensions,
                            _maxExtensionSize));
                    }
                    else if (reader.IsStartElement(App10Constants.Accept, App10Constants.Namespace))
                    {
                        result.Accepts.Add(reader.ReadElementString());
                    }
                    else if (!TryParseElement(reader, result, this.Version))
                    {
                        if (_preserveElementExtensions)
                        {
                            SyndicationFeedFormatter.CreateBufferIfRequiredAndWriteNode(ref buffer, ref extWriter, reader, _maxExtensionSize);
                        }
                        else
                        {
                            SyndicationFeedFormatter.TraceSyndicationElementIgnoredOnRead(reader);
                            reader.Skip();
                        }
                    }
                }
                LoadElementExtensions(buffer, extWriter, result);
            }
            finally
            {
                if (extWriter != null)
                {
                    extWriter.Close();
                }
            }
            reader.ReadEndElement();
            return result;
        }

        private void ReadDocument(XmlReader reader)
        {
            ServiceDocument result = CreateDocumentInstance();
            try
            {
                SyndicationFeedFormatter.MoveToStartElement(reader);
                bool elementIsEmpty = reader.IsEmptyElement;
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        if (reader.LocalName == "lang" && reader.NamespaceURI == Atom10FeedFormatter.XmlNs)
                        {
                            result.Language = reader.Value;
                        }
                        else if (reader.LocalName == "base" && reader.NamespaceURI == Atom10FeedFormatter.XmlNs)
                        {
                            result.BaseUri = new Uri(reader.Value, UriKind.RelativeOrAbsolute);
                        }
                        else
                        {
                            string ns = reader.NamespaceURI;
                            string name = reader.LocalName;
                            if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
                            {
                                continue;
                            }
                            string val = reader.Value;
                            if (!TryParseAttribute(name, ns, val, result, this.Version))
                            {
                                if (_preserveAttributeExtensions)
                                {
                                    result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), reader.Value);
                                }
                                else
                                {
                                    SyndicationFeedFormatter.TraceSyndicationElementIgnoredOnRead(reader);
                                }
                            }
                        }
                    }
                }
                XmlBuffer buffer = null;
                XmlDictionaryWriter extWriter = null;

                reader.ReadStartElement();
                if (!elementIsEmpty)
                {
                    try
                    {
                        while (reader.IsStartElement())
                        {
                            if (reader.IsStartElement(App10Constants.Workspace, App10Constants.Namespace))
                            {
                                result.Workspaces.Add(ReadWorkspace(reader, result));
                            }
                            else if (!TryParseElement(reader, result, this.Version))
                            {
                                if (_preserveElementExtensions)
                                {
                                    SyndicationFeedFormatter.CreateBufferIfRequiredAndWriteNode(ref buffer, ref extWriter, reader, _maxExtensionSize);
                                }
                                else
                                {
                                    SyndicationFeedFormatter.TraceSyndicationElementIgnoredOnRead(reader);
                                    reader.Skip();
                                }
                            }
                        }
                        LoadElementExtensions(buffer, extWriter, result);
                    }
                    finally
                    {
                        if (extWriter != null)
                        {
                            extWriter.Close();
                        }
                    }
                }
                reader.ReadEndElement();
            }
            catch (FormatException e)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingDocument), e));
            }
            catch (ArgumentException e)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingDocument), e));
            }
            SetDocument(result);
        }

        private Workspace ReadWorkspace(XmlReader reader, ServiceDocument document)
        {
            Workspace result = CreateWorkspace(document);
            result.BaseUri = document.BaseUri;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.LocalName == "base" && reader.NamespaceURI == Atom10FeedFormatter.XmlNs)
                    {
                        result.BaseUri = FeedUtils.CombineXmlBase(result.BaseUri, reader.Value);
                    }
                    else
                    {
                        string ns = reader.NamespaceURI;
                        string name = reader.LocalName;
                        if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
                        {
                            continue;
                        }
                        string val = reader.Value;
                        if (!TryParseAttribute(name, ns, val, result, this.Version))
                        {
                            if (_preserveAttributeExtensions)
                            {
                                result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), reader.Value);
                            }
                            else
                            {
                                SyndicationFeedFormatter.TraceSyndicationElementIgnoredOnRead(reader);
                            }
                        }
                    }
                }
            }

            XmlBuffer buffer = null;
            XmlDictionaryWriter extWriter = null;

            reader.ReadStartElement();
            try
            {
                while (reader.IsStartElement())
                {
                    if (reader.IsStartElement(Atom10Constants.TitleTag, Atom10Constants.Atom10Namespace))
                    {
                        result.Title = Atom10FeedFormatter.ReadTextContentFrom(reader, "//app:service/app:workspace/atom:title[@type]", _preserveAttributeExtensions);
                    }
                    else if (reader.IsStartElement(App10Constants.Collection, App10Constants.Namespace))
                    {
                        result.Collections.Add(ReadCollection(reader, result));
                    }
                    else if (!TryParseElement(reader, result, this.Version))
                    {
                        if (_preserveElementExtensions)
                        {
                            SyndicationFeedFormatter.CreateBufferIfRequiredAndWriteNode(ref buffer, ref extWriter, reader, _maxExtensionSize);
                        }
                        else
                        {
                            SyndicationFeedFormatter.TraceSyndicationElementIgnoredOnRead(reader);
                            reader.Skip();
                        }
                    }
                }
                LoadElementExtensions(buffer, extWriter, result);
            }
            finally
            {
                if (extWriter != null)
                {
                    extWriter.Close();
                }
            }
            reader.ReadEndElement();
            return result;
        }

        private void WriteCollection(XmlWriter writer, ResourceCollectionInfo collection, Uri baseUri)
        {
            writer.WriteStartElement(App10Constants.Prefix, App10Constants.Collection, App10Constants.Namespace);
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(baseUri, collection.BaseUri);
            if (baseUriToWrite != null)
            {
                baseUri = collection.BaseUri;
                WriteXmlBase(writer, baseUriToWrite);
            }
            if (collection.Link != null)
            {
                writer.WriteAttributeString(App10Constants.Href, FeedUtils.GetUriString(collection.Link));
            }
            WriteAttributeExtensions(writer, collection, this.Version);
            if (collection.Title != null)
            {
                collection.Title.WriteTo(writer, Atom10Constants.TitleTag, Atom10Constants.Atom10Namespace);
            }
            for (int i = 0; i < collection.Accepts.Count; ++i)
            {
                writer.WriteElementString(App10Constants.Prefix, App10Constants.Accept, App10Constants.Namespace, collection.Accepts[i]);
            }
            for (int i = 0; i < collection.Categories.Count; ++i)
            {
                WriteCategories(writer, collection.Categories[i], baseUri, this.Version);
            }
            WriteElementExtensions(writer, collection, this.Version);
            writer.WriteEndElement();
        }

        private void WriteDocument(XmlWriter writer)
        {
            // declare the atom10 namespace upfront for compactness
            writer.WriteAttributeString(Atom10Constants.Atom10Prefix, Atom10FeedFormatter.XmlNsNs, Atom10Constants.Atom10Namespace);
            if (!string.IsNullOrEmpty(this.Document.Language))
            {
                WriteXmlLang(writer, this.Document.Language);
            }
            Uri baseUri = this.Document.BaseUri;
            if (baseUri != null)
            {
                WriteXmlBase(writer, baseUri);
            }
            WriteAttributeExtensions(writer, this.Document, this.Version);

            for (int i = 0; i < this.Document.Workspaces.Count; ++i)
            {
                WriteWorkspace(writer, this.Document.Workspaces[i], baseUri);
            }
            WriteElementExtensions(writer, this.Document, this.Version);
        }

        private void WriteWorkspace(XmlWriter writer, Workspace workspace, Uri baseUri)
        {
            writer.WriteStartElement(App10Constants.Prefix, App10Constants.Workspace, App10Constants.Namespace);
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(baseUri, workspace.BaseUri);
            if (baseUriToWrite != null)
            {
                baseUri = workspace.BaseUri;
                WriteXmlBase(writer, baseUriToWrite);
            }
            WriteAttributeExtensions(writer, workspace, this.Version);
            if (workspace.Title != null)
            {
                workspace.Title.WriteTo(writer, Atom10Constants.TitleTag, Atom10Constants.Atom10Namespace);
            }
            for (int i = 0; i < workspace.Collections.Count; ++i)
            {
                WriteCollection(writer, workspace.Collections[i], baseUri);
            }
            WriteElementExtensions(writer, workspace, this.Version);
            writer.WriteEndElement();
        }
    }

    [XmlRoot(ElementName = App10Constants.Service, Namespace = App10Constants.Namespace)]
    public class AtomPub10ServiceDocumentFormatter<TServiceDocument> : AtomPub10ServiceDocumentFormatter
        where TServiceDocument : ServiceDocument, new()
    {
        public AtomPub10ServiceDocumentFormatter() :
            base(typeof(TServiceDocument))
        {
        }

        public AtomPub10ServiceDocumentFormatter(TServiceDocument documentToWrite)
            : base(documentToWrite)
        {
        }

        protected override ServiceDocument CreateDocumentInstance()
        {
            return new TServiceDocument();
        }
    }
}
