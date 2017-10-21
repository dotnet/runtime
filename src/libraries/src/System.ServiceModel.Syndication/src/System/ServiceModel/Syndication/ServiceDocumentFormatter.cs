// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using System.Xml;

    [DataContract]
    public abstract class ServiceDocumentFormatter
    {
        private ServiceDocument _document;

        protected ServiceDocumentFormatter()
        {
        }
        protected ServiceDocumentFormatter(ServiceDocument documentToWrite)
        {
            if (documentToWrite == null)
            {
                throw new ArgumentNullException(nameof(documentToWrite));
            }
            _document = documentToWrite;
        }

        public ServiceDocument Document
        {
            get { return _document; }
        }

        public abstract string Version
        { get; }

        public abstract bool CanRead(XmlReader reader);
        public abstract void ReadFrom(System.Xml.XmlReader reader);
        public abstract void WriteTo(System.Xml.XmlWriter writer);
        public abstract Task ReadFromAsync(XmlReader reader);
        public abstract Task WriteToAsync(XmlWriter writer);

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, CategoriesDocument categories)
        {
            if (categories == null)
            {
                throw new ArgumentNullException(nameof(categories));
            }
            Atom10FeedFormatter.CloseBuffer(buffer, writer);
            categories.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, ResourceCollectionInfo collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }
            Atom10FeedFormatter.CloseBuffer(buffer, writer);
            collection.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, Workspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }
            Atom10FeedFormatter.CloseBuffer(buffer, writer);
            workspace.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, ServiceDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }
            Atom10FeedFormatter.CloseBuffer(buffer, writer);
            document.LoadElementExtensions(buffer);
        }

        protected static SyndicationCategory CreateCategory(InlineCategoriesDocument inlineCategories)
        {
            if (inlineCategories == null)
            {
                throw new ArgumentNullException(nameof(inlineCategories));
            }
            return inlineCategories.CreateCategory();
        }

        protected static ResourceCollectionInfo CreateCollection(Workspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }
            return workspace.CreateResourceCollection();
        }

        protected static InlineCategoriesDocument CreateInlineCategories(ResourceCollectionInfo collection)
        {
            return collection.CreateInlineCategoriesDocument();
        }

        protected static ReferencedCategoriesDocument CreateReferencedCategories(ResourceCollectionInfo collection)
        {
            return collection.CreateReferencedCategoriesDocument();
        }

        protected static Workspace CreateWorkspace(ServiceDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }
            return document.CreateWorkspace();
        }

        protected static void LoadElementExtensions(XmlReader reader, CategoriesDocument categories, int maxExtensionSize)
        {
            if (categories == null)
            {
                throw new ArgumentNullException(nameof(categories));
            }

            categories.LoadElementExtensions(XmlReaderWrapper.CreateFromReader(reader), maxExtensionSize);
        }

        protected static void LoadElementExtensions(XmlReader reader, ResourceCollectionInfo collection, int maxExtensionSize)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            collection.LoadElementExtensions(XmlReaderWrapper.CreateFromReader(reader), maxExtensionSize);
        }

        protected static void LoadElementExtensions(XmlReader reader, Workspace workspace, int maxExtensionSize)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            workspace.LoadElementExtensions(XmlReaderWrapper.CreateFromReader(reader), maxExtensionSize);
        }

        protected static void LoadElementExtensions(XmlReader reader, ServiceDocument document, int maxExtensionSize)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            document.LoadElementExtensions(XmlReaderWrapper.CreateFromReader(reader), maxExtensionSize);
        }

        protected static bool TryParseAttribute(string name, string ns, string value, ServiceDocument document, string version)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return document.TryParseAttribute(name, ns, value, version);
        }

        protected static bool TryParseAttribute(string name, string ns, string value, ResourceCollectionInfo collection, string version)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            return collection.TryParseAttribute(name, ns, value, version);
        }

        protected static bool TryParseAttribute(string name, string ns, string value, CategoriesDocument categories, string version)
        {
            if (categories == null)
            {
                throw new ArgumentNullException(nameof(categories));
            }

            return categories.TryParseAttribute(name, ns, value, version);
        }

        protected static bool TryParseAttribute(string name, string ns, string value, Workspace workspace, string version)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            return workspace.TryParseAttribute(name, ns, value, version);
        }

        protected static bool TryParseElement(XmlReader reader, ResourceCollectionInfo collection, string version)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            return collection.TryParseElement(XmlReaderWrapper.CreateFromReader(reader), version);
        }

        protected static bool TryParseElement(XmlReader reader, ServiceDocument document, string version)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return document.TryParseElement(XmlReaderWrapper.CreateFromReader(reader), version);
        }

        protected static bool TryParseElement(XmlReader reader, Workspace workspace, string version)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            return workspace.TryParseElement(XmlReaderWrapper.CreateFromReader(reader), version);
        }

        protected static bool TryParseElement(XmlReader reader, CategoriesDocument categories, string version)
        {
            if (categories == null)
            {
                throw new ArgumentNullException(nameof(categories));
            }

            return categories.TryParseElement(XmlReaderWrapper.CreateFromReader(reader), version);
        }

        protected static void WriteAttributeExtensions(XmlWriter writer, ServiceDocument document, string version)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }
            document.WriteAttributeExtensions(writer, version);
        }

        protected static void WriteAttributeExtensions(XmlWriter writer, Workspace workspace, string version)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }
            workspace.WriteAttributeExtensions(writer, version);
        }

        protected static void WriteAttributeExtensions(XmlWriter writer, ResourceCollectionInfo collection, string version)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }
            collection.WriteAttributeExtensions(writer, version);
        }

        protected static void WriteAttributeExtensions(XmlWriter writer, CategoriesDocument categories, string version)
        {
            if (categories == null)
            {
                throw new ArgumentNullException(nameof(categories));
            }
            categories.WriteAttributeExtensions(writer, version);
        }

        protected static void WriteElementExtensions(XmlWriter writer, ServiceDocument document, string version)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }
            document.WriteElementExtensions(writer, version);
        }

        protected static void WriteElementExtensions(XmlWriter writer, Workspace workspace, string version)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }
            workspace.WriteElementExtensions(writer, version);
        }

        protected static void WriteElementExtensions(XmlWriter writer, ResourceCollectionInfo collection, string version)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }
            collection.WriteElementExtensions(writer, version);
        }

        protected static void WriteElementExtensions(XmlWriter writer, CategoriesDocument categories, string version)
        {
            if (categories == null)
            {
                throw new ArgumentNullException(nameof(categories));
            }
            categories.WriteElementExtensions(writer, version);
        }

        protected static Task WriteAttributeExtensionsAsync(XmlWriter writer, ServiceDocument document, string version)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return document.WriteAttributeExtensionsAsync(writer, version);
        }

        protected static Task WriteAttributeExtensionsAsync(XmlWriter writer, Workspace workspace, string version)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            return workspace.WriteAttributeExtensionsAsync(writer, version);
        }

        protected static Task WriteAttributeExtensionsAsync(XmlWriter writer, ResourceCollectionInfo collection, string version)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            return collection.WriteAttributeExtensionsAsync(writer, version);
        }

        protected static Task WriteAttributeExtensionsAsync(XmlWriter writer, CategoriesDocument categories, string version)
        {
            if (categories == null)
            {
                throw new ArgumentNullException(nameof(categories));
            }

            return categories.WriteAttributeExtensionsAsync(writer, version);
        }

        protected static Task WriteElementExtensionsAsync(XmlWriter writer, ServiceDocument document, string version)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return document.WriteElementExtensionsAsync(writer, version);
        }

        protected static Task WriteElementExtensionsAsync(XmlWriter writer, Workspace workspace, string version)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            return workspace.WriteElementExtensionsAsync(writer, version);
        }

        protected static Task WriteElementExtensionsAsync(XmlWriter writer, ResourceCollectionInfo collection, string version)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            return collection.WriteElementExtensionsAsync(writer, version);
        }

        protected static Task WriteElementExtensionsAsync(XmlWriter writer, CategoriesDocument categories, string version)
        {
            if (categories == null)
            {
                throw new ArgumentNullException(nameof(categories));
            }

            return categories.WriteElementExtensionsAsync(writer, version);
        }

        protected virtual ServiceDocument CreateDocumentInstance()
        {
            return new ServiceDocument();
        }

        protected virtual void SetDocument(ServiceDocument document)
        {
            _document = document;
        }
    }
}
