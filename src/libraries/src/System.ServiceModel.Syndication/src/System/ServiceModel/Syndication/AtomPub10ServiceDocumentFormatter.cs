// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;

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
                throw new ArgumentNullException(nameof(documentTypeToCreate));
            }
            if (!typeof(ServiceDocument).IsAssignableFrom(documentTypeToCreate))
            {
                throw new ArgumentException(SR.Format(SR.InvalidObjectTypePassed, nameof(documentTypeToCreate), nameof(ServiceDocument)));
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
                throw new ArgumentNullException(nameof(reader));
            }

            reader = XmlReaderWrapper.CreateFromReader(reader);
            return reader.IsStartElement(App10Constants.Service, App10Constants.Namespace);
        }

        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            ReadDocumentAsync(XmlReaderWrapper.CreateFromReader(reader)).GetAwaiter().GetResult();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            if (this.Document == null)
            {
                throw new InvalidOperationException(SR.DocumentFormatterDoesNotHaveDocument);
            }

            WriteDocumentAsync(XmlWriterWrapper.CreateFromWriter(writer)).GetAwaiter().GetResult();
        }

        public override void ReadFrom(XmlReader reader)
        {
            ReadFromAsync(reader).GetAwaiter().GetResult();
        }

        public override void WriteTo(XmlWriter writer)
        {
            WriteToAsync(writer).GetAwaiter().GetResult();
        }

        private Task WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (Document == null)
            {
                throw new InvalidOperationException(SR.DocumentFormatterDoesNotHaveDocument);
            }

            return WriteDocumentAsync(XmlWriterWrapper.CreateFromWriter(writer));
        }

        public override async Task ReadFromAsync(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            reader = XmlReaderWrapper.CreateFromReader(reader);
            await reader.MoveToContentAsync().ConfigureAwait(false);

            if (!CanRead(reader))
            {
                throw new XmlException(SR.Format(SR.UnknownDocumentXml, reader.LocalName, reader.NamespaceURI));
            }

            await ReadDocumentAsync(reader).ConfigureAwait(false);
        }

        public override async Task WriteToAsync(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (Document == null)
            {
                throw new InvalidOperationException(SR.DocumentFormatterDoesNotHaveDocument);
            }

            writer = XmlWriterWrapper.CreateFromWriter(writer);

            await writer.WriteStartElementAsync(App10Constants.Prefix, App10Constants.Service, App10Constants.Namespace).ConfigureAwait(false);
            await WriteDocumentAsync(writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        internal static async Task<CategoriesDocument> ReadCategories(XmlReader reader, Uri baseUri, CreateInlineCategoriesDelegate inlineCategoriesFactory, CreateReferencedCategoriesDelegate referencedCategoriesFactory, string version, bool preserveElementExtensions, bool preserveAttributeExtensions, int maxExtensionSize)
        {
            string link = reader.GetAttribute(App10Constants.Href, string.Empty);
            if (string.IsNullOrEmpty(link))
            {
                InlineCategoriesDocument inlineCategories = inlineCategoriesFactory();
                await ReadInlineCategoriesAsync(reader, inlineCategories, baseUri, version, preserveElementExtensions, preserveAttributeExtensions, maxExtensionSize).ConfigureAwait(false);
                return inlineCategories;
            }
            else
            {
                ReferencedCategoriesDocument referencedCategories = referencedCategoriesFactory();
                await ReadReferencedCategoriesAsync(reader, referencedCategories, baseUri, new Uri(link, UriKind.RelativeOrAbsolute), version, preserveElementExtensions, preserveAttributeExtensions, maxExtensionSize).ConfigureAwait(false);
                return referencedCategories;
            }
        }

        internal static async Task WriteCategoriesInnerXml(XmlWriter writer, CategoriesDocument categories, Uri baseUri, string version)
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
                await WriteInlineCategoriesContentAsync(XmlWriterWrapper.CreateFromWriter(writer), (InlineCategoriesDocument) categories, version).ConfigureAwait(false);
            }
            else
            {
                WriteReferencedCategoriesContent(writer, (ReferencedCategoriesDocument) categories, version);
            }
        }

        protected override ServiceDocument CreateDocumentInstance()
        {
            if (_documentType == typeof (ServiceDocument))
            {
                return new ServiceDocument();
            }
            else
            {
                return (ServiceDocument) Activator.CreateInstance(_documentType);
            }
        }

        private static async Task ReadInlineCategoriesAsync(XmlReader reader, InlineCategoriesDocument inlineCategories, Uri baseUri, string version, bool preserveElementExtensions, bool preserveAttributeExtensions, int _maxExtensionSize)
        {
            inlineCategories.BaseUri = baseUri;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.LocalName == "base" && reader.NamespaceURI == Atom10FeedFormatter.XmlNs)
                    {
                        inlineCategories.BaseUri = FeedUtils.CombineXmlBase(inlineCategories.BaseUri, await reader.GetValueAsync().ConfigureAwait(false));
                    }
                    else if (reader.LocalName == "lang" && reader.NamespaceURI == Atom10FeedFormatter.XmlNs)
                    {
                        inlineCategories.Language = await reader.GetValueAsync().ConfigureAwait(false);
                    }
                    else if (reader.LocalName == App10Constants.Fixed && reader.NamespaceURI == string.Empty)
                    {
                        inlineCategories.IsFixed = (reader.Value == "yes");
                    }
                    else if (reader.LocalName == Atom10Constants.SchemeTag && reader.NamespaceURI == string.Empty)
                    {
                        inlineCategories.Scheme = await reader.GetValueAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        string ns = reader.NamespaceURI;
                        string name = reader.LocalName;
                        if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
                        {
                            continue;
                        }
                        string val = await reader.GetValueAsync().ConfigureAwait(false);
                        if (!TryParseAttribute(name, ns, val, inlineCategories, version))
                        {
                            if (preserveAttributeExtensions)
                            {
                                inlineCategories.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), await reader.GetValueAsync().ConfigureAwait(false));
                            }
                        }
                    }
                }
            }

            await SyndicationFeedFormatter.MoveToStartElementAsync(reader).ConfigureAwait(false);
            bool isEmptyElement = reader.IsEmptyElement;
            await reader.ReadStartElementAsync().ConfigureAwait(false);
            if (!isEmptyElement)
            {
                XmlBuffer buffer = null;
                XmlDictionaryWriter extWriter = null;
                try
                {
                    while (await reader.IsStartElementAsync().ConfigureAwait(false))
                    {
                        if (await reader.IsStartElementAsync(Atom10Constants.CategoryTag, Atom10Constants.Atom10Namespace).ConfigureAwait(false))
                        {
                            SyndicationCategory category = CreateCategory(inlineCategories);
                            await Atom10FeedFormatter.ReadCategoryAsync(reader, category, version, preserveAttributeExtensions, preserveElementExtensions, _maxExtensionSize).ConfigureAwait(false);
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
                                var tuple = await SyndicationFeedFormatter.CreateBufferIfRequiredAndWriteNodeAsync(buffer, extWriter, reader, _maxExtensionSize).ConfigureAwait(false);
                                buffer = tuple.Item1;
                                extWriter = tuple.Item2;
                            }
                            else
                            {
                                await reader.SkipAsync().ConfigureAwait(false);
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

                await reader.ReadEndElementAsync().ConfigureAwait(false);
            }
        }

        private static async Task ReadReferencedCategoriesAsync(XmlReader reader, ReferencedCategoriesDocument referencedCategories, Uri baseUri, Uri link, string version, bool preserveElementExtensions, bool preserveAttributeExtensions, int maxExtensionSize)
        {
            referencedCategories.BaseUri = baseUri;
            referencedCategories.Link = link;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.LocalName == "base" && reader.NamespaceURI == Atom10FeedFormatter.XmlNs)
                    {
                        referencedCategories.BaseUri = FeedUtils.CombineXmlBase(referencedCategories.BaseUri, await reader.GetValueAsync().ConfigureAwait(false));
                    }
                    else if (reader.LocalName == "lang" && reader.NamespaceURI == Atom10FeedFormatter.XmlNs)
                    {
                        referencedCategories.Language = await reader.GetValueAsync().ConfigureAwait(false);
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

                        string val = await reader.GetValueAsync().ConfigureAwait(false);
                        if (!TryParseAttribute(name, ns, val, referencedCategories, version))
                        {
                            if (preserveAttributeExtensions)
                            {
                                referencedCategories.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), await reader.GetValueAsync().ConfigureAwait(false));
                            }
                        }
                    }
                }
            }

            reader.MoveToElement();
            bool isEmptyElement = reader.IsEmptyElement;
            await reader.ReadStartElementAsync().ConfigureAwait(false);
            if (!isEmptyElement)
            {
                XmlBuffer buffer = null;
                XmlDictionaryWriter extWriter = null;
                try
                {
                    while (await reader.IsStartElementAsync().ConfigureAwait(false))
                    {
                        if (!TryParseElement(reader, referencedCategories, version))
                        {
                            if (preserveElementExtensions)
                            {
                                var tuple = await SyndicationFeedFormatter.CreateBufferIfRequiredAndWriteNodeAsync(buffer, extWriter, reader, maxExtensionSize).ConfigureAwait(false);
                                buffer = tuple.Item1;
                                extWriter = tuple.Item2;
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

                await reader.ReadEndElementAsync().ConfigureAwait(false);
            }
        }

        private static async Task WriteCategoriesAsync(XmlWriter writer, CategoriesDocument categories, Uri baseUri, string version)
        {
            await writer.WriteStartElementAsync(App10Constants.Prefix, App10Constants.Categories, App10Constants.Namespace).ConfigureAwait(false);
            await WriteCategoriesInnerXml(writer, categories, baseUri, version).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        private static async Task WriteInlineCategoriesContentAsync(XmlWriter writer, InlineCategoriesDocument categories, string version)
        {
            writer = XmlWriterWrapper.CreateFromWriter(writer);
            if (!string.IsNullOrEmpty(categories.Scheme))
            {
                await writer.WriteAttributeStringAsync(Atom10Constants.SchemeTag, categories.Scheme).ConfigureAwait(false);
            }
            // by default, categories are not fixed
            if (categories.IsFixed)
            {
                await writer.WriteAttributeStringAsync(App10Constants.Fixed, "yes").ConfigureAwait(false);
            }

            await WriteAttributeExtensionsAsync(writer, categories, version).ConfigureAwait(false);

            for (int i = 0; i < categories.Categories.Count; ++i)
            {
                await Atom10FeedFormatter.WriteCategoryAsync(writer, categories.Categories[i], version).ConfigureAwait(false);
            }

            await WriteElementExtensionsAsync(writer, categories, version).ConfigureAwait(false);
        }

        private static void WriteReferencedCategoriesContent(XmlWriter writer, ReferencedCategoriesDocument categories, string version)
        {
            if (categories.Link != null)
            {
                writer.WriteAttributeString(App10Constants.Href, FeedUtils.GetUriString(categories.Link));
            }

            WriteAttributeExtensionsAsync(writer, categories, version);
            WriteElementExtensionsAsync(writer, categories, version);
        }

        private static void WriteXmlBase(XmlWriter writer, Uri baseUri)
        {
            writer.WriteAttributeString("xml", "base", Atom10FeedFormatter.XmlNs, FeedUtils.GetUriString(baseUri));
        }

        private static void WriteXmlLang(XmlWriter writer, string lang)
        {
            writer.WriteAttributeString("xml", "lang", Atom10FeedFormatter.XmlNs, lang);
        }

        private async Task<ResourceCollectionInfo> ReadCollectionAsync(XmlReader reader, Workspace workspace)
        {
            ResourceCollectionInfo result = CreateCollection(workspace);
            result.BaseUri = workspace.BaseUri;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.LocalName == "base" && reader.NamespaceURI == Atom10FeedFormatter.XmlNs)
                    {
                        result.BaseUri = FeedUtils.CombineXmlBase(result.BaseUri, await reader.GetValueAsync().ConfigureAwait(false));
                    }
                    else if (reader.LocalName == App10Constants.Href && reader.NamespaceURI == string.Empty)
                    {
                        result.Link = new Uri(await reader.GetValueAsync().ConfigureAwait(false), UriKind.RelativeOrAbsolute);
                    }
                    else
                    {
                        string ns = reader.NamespaceURI;
                        string name = reader.LocalName;
                        if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
                        {
                            continue;
                        }

                        string val = await reader.GetValueAsync().ConfigureAwait(false);
                        if (!TryParseAttribute(name, ns, val, result, Version))
                        {
                            if (_preserveAttributeExtensions)
                            {
                                result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), val);
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
                while (await reader.IsStartElementAsync().ConfigureAwait(false))
                {
                    if (await reader.IsStartElementAsync(Atom10Constants.TitleTag, Atom10Constants.Atom10Namespace).ConfigureAwait(false))
                    {
                        result.Title = await new Atom10FeedFormatter().ReadTextContentFromAsync(reader, "//app:service/app:workspace/app:collection/atom:title[@type]", _preserveAttributeExtensions).ConfigureAwait(false);
                    }
                    else if (await reader.IsStartElementAsync(App10Constants.Categories, App10Constants.Namespace).ConfigureAwait(false))
                    {
                        result.Categories.Add(await ReadCategories(reader, result.BaseUri, delegate() { return CreateInlineCategories(result); }, delegate() { return CreateReferencedCategories(result); }, Version, _preserveElementExtensions, _preserveAttributeExtensions, _maxExtensionSize).ConfigureAwait(false));
                    }
                    else if (await reader.IsStartElementAsync(App10Constants.Accept, App10Constants.Namespace).ConfigureAwait(false))
                    {
                        result.Accepts.Add(reader.ReadElementString());
                    }
                    else if (!TryParseElement(reader, result, Version))
                    {
                        if (_preserveElementExtensions)
                        {
                            var tuple = await SyndicationFeedFormatter.CreateBufferIfRequiredAndWriteNodeAsync(buffer, extWriter, reader, _maxExtensionSize).ConfigureAwait(false);
                            buffer = tuple.Item1;
                            extWriter = tuple.Item2;
                        }
                        else
                        {
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

        private async Task ReadDocumentAsync(XmlReader reader)
        {
            ServiceDocument result = CreateDocumentInstance();
            try
            {
                await SyndicationFeedFormatter.MoveToStartElementAsync(reader).ConfigureAwait(false);
                bool elementIsEmpty = reader.IsEmptyElement;
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        if (reader.LocalName == "lang" && reader.NamespaceURI == Atom10FeedFormatter.XmlNs)
                        {
                            result.Language = await reader.GetValueAsync().ConfigureAwait(false);
                        }
                        else if (reader.LocalName == "base" && reader.NamespaceURI == Atom10FeedFormatter.XmlNs)
                        {
                            result.BaseUri = new Uri(await reader.GetValueAsync().ConfigureAwait(false), UriKind.RelativeOrAbsolute);
                        }
                        else
                        {
                            string ns = reader.NamespaceURI;
                            string name = reader.LocalName;
                            if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
                            {
                                continue;
                            }

                            string val = await reader.GetValueAsync().ConfigureAwait(false);
                            if (!TryParseAttribute(name, ns, val, result, Version))
                            {
                                if (_preserveAttributeExtensions)
                                {
                                    result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), val);
                                }
                            }
                        }
                    }
                }
                XmlBuffer buffer = null;
                XmlDictionaryWriter extWriter = null;

                await reader.ReadStartElementAsync().ConfigureAwait(false);
                if (!elementIsEmpty)
                {
                    try
                    {
                        while (await reader.IsStartElementAsync().ConfigureAwait(false))
                        {
                            if (await reader.IsStartElementAsync(App10Constants.Workspace, App10Constants.Namespace).ConfigureAwait(false))
                            {
                                result.Workspaces.Add(await ReadWorkspaceAsync(reader, result).ConfigureAwait(false));
                            }
                            else if (!TryParseElement(reader, result, Version))
                            {
                                if (_preserveElementExtensions)
                                {
                                    var tuple = await SyndicationFeedFormatter.CreateBufferIfRequiredAndWriteNodeAsync(buffer, extWriter, reader, _maxExtensionSize).ConfigureAwait(false);
                                    buffer = tuple.Item1;
                                    extWriter = tuple.Item2;
                                }
                                else
                                {
                                    await reader.SkipAsync().ConfigureAwait(false);
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

                await reader.ReadEndElementAsync().ConfigureAwait(false);
            }
            catch (FormatException e)
            {
                throw new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingDocument), e);
            }
            catch (ArgumentException e)
            {
                new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingDocument), e);
            }

            SetDocument(result);
        }

        private async Task<Workspace> ReadWorkspaceAsync(XmlReader reader, ServiceDocument document)
        {
            Workspace result = CreateWorkspace(document);
            result.BaseUri = document.BaseUri;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.LocalName == "base" && reader.NamespaceURI == Atom10FeedFormatter.XmlNs)
                    {
                        result.BaseUri = FeedUtils.CombineXmlBase(result.BaseUri, await reader.GetValueAsync().ConfigureAwait(false));
                    }
                    else
                    {
                        string ns = reader.NamespaceURI;
                        string name = reader.LocalName;
                        if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
                        {
                            continue;
                        }

                        string val = await reader.GetValueAsync().ConfigureAwait(false);
                        if (!TryParseAttribute(name, ns, val, result, Version))
                        {
                            if (_preserveAttributeExtensions)
                            {
                                result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), val);
                            }
                        }
                    }
                }
            }

            XmlBuffer buffer = null;
            XmlDictionaryWriter extWriter = null;
            await reader.ReadStartElementAsync().ConfigureAwait(false);
            try
            {
                while (await reader.IsStartElementAsync().ConfigureAwait(false))
                {
                    if (await reader.IsStartElementAsync(Atom10Constants.TitleTag, Atom10Constants.Atom10Namespace).ConfigureAwait(false))
                    {
                        result.Title = await new Atom10FeedFormatter().ReadTextContentFromAsync(reader, "//app:service/app:workspace/atom:title[@type]", _preserveAttributeExtensions).ConfigureAwait(false);
                    }
                    else if (await reader.IsStartElementAsync(App10Constants.Collection, App10Constants.Namespace).ConfigureAwait(false))
                    {
                        result.Collections.Add(await ReadCollectionAsync(reader, result).ConfigureAwait(false));
                    }
                    else if (!TryParseElement(reader, result, Version))
                    {
                        if (_preserveElementExtensions)
                        {
                            var tuple = await SyndicationFeedFormatter.CreateBufferIfRequiredAndWriteNodeAsync(buffer, extWriter, reader, _maxExtensionSize).ConfigureAwait(false);
                            buffer = tuple.Item1;
                            extWriter = tuple.Item2;
                        }
                        else
                        {
                            await reader.SkipAsync().ConfigureAwait(false);
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

            await reader.ReadEndElementAsync().ConfigureAwait(false);
            return result;
        }

        private async Task WriteCollectionAsync(XmlWriter writer, ResourceCollectionInfo collection, Uri baseUri)
        {
            await writer.WriteStartElementAsync(App10Constants.Prefix, App10Constants.Collection, App10Constants.Namespace).ConfigureAwait(false);
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(baseUri, collection.BaseUri);
            if (baseUriToWrite != null)
            {
                baseUri = collection.BaseUri;
                WriteXmlBase(writer, baseUriToWrite);
            }

            if (collection.Link != null)
            {
                await writer.WriteAttributeStringAsync(App10Constants.Href, FeedUtils.GetUriString(collection.Link)).ConfigureAwait(false);
            }

            await WriteAttributeExtensionsAsync(writer, collection, Version).ConfigureAwait(false);
            if (collection.Title != null)
            {
                await collection.Title.WriteToAsync(writer, Atom10Constants.TitleTag, Atom10Constants.Atom10Namespace).ConfigureAwait(false);
            }

            for (int i = 0; i < collection.Accepts.Count; ++i)
            {
                await writer.WriteElementStringAsync(App10Constants.Prefix, App10Constants.Accept, App10Constants.Namespace, collection.Accepts[i]).ConfigureAwait(false);
            }

            for (int i = 0; i < collection.Categories.Count; ++i)
            {
                await WriteCategoriesAsync(writer, collection.Categories[i], baseUri, Version).ConfigureAwait(false);
            }

            await WriteElementExtensionsAsync(writer, collection, Version).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        private async Task WriteDocumentAsync(XmlWriter writer)
        {
            // declare the atom10 namespace upfront for compactness
            await writer.WriteAttributeStringAsync(Atom10Constants.Atom10Prefix, Atom10FeedFormatter.XmlNsNs, Atom10Constants.Atom10Namespace).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(Document.Language))
            {
                WriteXmlLang(writer, Document.Language);
            }

            Uri baseUri = Document.BaseUri;
            if (baseUri != null)
            {
                WriteXmlBase(writer, baseUri);
            }

            WriteAttributeExtensions(writer, Document, Version);

            for (int i = 0; i < Document.Workspaces.Count; ++i)
            {
                await WriteWorkspaceAsync(writer, Document.Workspaces[i], baseUri).ConfigureAwait(false);
            }

            await WriteElementExtensionsAsync(writer, Document, Version).ConfigureAwait(false);
        }

        private async Task WriteWorkspaceAsync(XmlWriter writer, Workspace workspace, Uri baseUri)
        {
            await writer.WriteStartElementAsync(App10Constants.Prefix, App10Constants.Workspace, App10Constants.Namespace).ConfigureAwait(false);
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(baseUri, workspace.BaseUri);
            if (baseUriToWrite != null)
            {
                baseUri = workspace.BaseUri;
                WriteXmlBase(writer, baseUriToWrite);
            }

            WriteAttributeExtensions(writer, workspace, Version);
            if (workspace.Title != null)
            {
                await workspace.Title.WriteToAsync(writer, Atom10Constants.TitleTag, Atom10Constants.Atom10Namespace).ConfigureAwait(false);
            }

            for (int i = 0; i < workspace.Collections.Count; ++i)
            {
                await WriteCollectionAsync(writer, workspace.Collections[i], baseUri).ConfigureAwait(false);
            }

            await WriteElementExtensionsAsync(writer, workspace, Version).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
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
