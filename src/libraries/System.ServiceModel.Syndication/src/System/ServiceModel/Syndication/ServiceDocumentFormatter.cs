// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml;

namespace System.ServiceModel.Syndication
{
    [DataContract]
    public abstract class ServiceDocumentFormatter
    {
        private ServiceDocument _document;

        protected ServiceDocumentFormatter()
        {
        }

        protected ServiceDocumentFormatter(ServiceDocument documentToWrite)
        {
            ArgumentNullException.ThrowIfNull(documentToWrite);

            _document = documentToWrite;
        }

        public ServiceDocument Document => _document;

        public abstract string Version { get; }

        public abstract bool CanRead(XmlReader reader);
        public abstract void ReadFrom(XmlReader reader);
        public abstract void WriteTo(XmlWriter writer);

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, CategoriesDocument categories)
        {
            Debug.Assert(categories != null);

            SyndicationFeedFormatter.CloseBuffer(buffer, writer);
            categories.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, ResourceCollectionInfo collection)
        {
            Debug.Assert(collection != null);

            SyndicationFeedFormatter.CloseBuffer(buffer, writer);
            collection.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, Workspace workspace)
        {
            Debug.Assert(workspace != null);

            SyndicationFeedFormatter.CloseBuffer(buffer, writer);
            workspace.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, ServiceDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);

            SyndicationFeedFormatter.CloseBuffer(buffer, writer);
            document.LoadElementExtensions(buffer);
        }

        protected static SyndicationCategory CreateCategory(InlineCategoriesDocument inlineCategories)
        {
            ArgumentNullException.ThrowIfNull(inlineCategories);

            return inlineCategories.CreateCategory();
        }

        protected static ResourceCollectionInfo CreateCollection(Workspace workspace)
        {
            ArgumentNullException.ThrowIfNull(workspace);

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
            ArgumentNullException.ThrowIfNull(document);

            return document.CreateWorkspace();
        }

        protected static void LoadElementExtensions(XmlReader reader, CategoriesDocument categories, int maxExtensionSize)
        {
            ArgumentNullException.ThrowIfNull(categories);

            categories.LoadElementExtensions(reader, maxExtensionSize);
        }

        protected static void LoadElementExtensions(XmlReader reader, ResourceCollectionInfo collection, int maxExtensionSize)
        {
            ArgumentNullException.ThrowIfNull(collection);

            collection.LoadElementExtensions(reader, maxExtensionSize);
        }

        protected static void LoadElementExtensions(XmlReader reader, Workspace workspace, int maxExtensionSize)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            workspace.LoadElementExtensions(reader, maxExtensionSize);
        }

        protected static void LoadElementExtensions(XmlReader reader, ServiceDocument document, int maxExtensionSize)
        {
            ArgumentNullException.ThrowIfNull(document);

            document.LoadElementExtensions(reader, maxExtensionSize);
        }

        protected static bool TryParseAttribute(string name, string ns, string value, ServiceDocument document, string version)
        {
            ArgumentNullException.ThrowIfNull(document);

            return document.TryParseAttribute(name, ns, value, version);
        }

        protected static bool TryParseAttribute(string name, string ns, string value, ResourceCollectionInfo collection, string version)
        {
            ArgumentNullException.ThrowIfNull(collection);

            return collection.TryParseAttribute(name, ns, value, version);
        }

        protected static bool TryParseAttribute(string name, string ns, string value, CategoriesDocument categories, string version)
        {
            ArgumentNullException.ThrowIfNull(categories);

            return categories.TryParseAttribute(name, ns, value, version);
        }

        protected static bool TryParseAttribute(string name, string ns, string value, Workspace workspace, string version)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            return workspace.TryParseAttribute(name, ns, value, version);
        }

        protected static bool TryParseElement(XmlReader reader, ResourceCollectionInfo collection, string version)
        {
            ArgumentNullException.ThrowIfNull(collection);

            return collection.TryParseElement(reader, version);
        }

        protected static bool TryParseElement(XmlReader reader, ServiceDocument document, string version)
        {
            ArgumentNullException.ThrowIfNull(document);

            return document.TryParseElement(reader, version);
        }

        protected static bool TryParseElement(XmlReader reader, Workspace workspace, string version)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            return workspace.TryParseElement(reader, version);
        }

        protected static bool TryParseElement(XmlReader reader, CategoriesDocument categories, string version)
        {
            ArgumentNullException.ThrowIfNull(categories);

            return categories.TryParseElement(reader, version);
        }

        protected static void WriteAttributeExtensions(XmlWriter writer, ServiceDocument document, string version)
        {
            ArgumentNullException.ThrowIfNull(document);

            document.WriteAttributeExtensions(writer, version);
        }

        protected static void WriteAttributeExtensions(XmlWriter writer, Workspace workspace, string version)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            workspace.WriteAttributeExtensions(writer, version);
        }

        protected static void WriteAttributeExtensions(XmlWriter writer, ResourceCollectionInfo collection, string version)
        {
            ArgumentNullException.ThrowIfNull(collection);

            collection.WriteAttributeExtensions(writer, version);
        }

        protected static void WriteAttributeExtensions(XmlWriter writer, CategoriesDocument categories, string version)
        {
            ArgumentNullException.ThrowIfNull(categories);

            categories.WriteAttributeExtensions(writer, version);
        }

        protected static void WriteElementExtensions(XmlWriter writer, ServiceDocument document, string version)
        {
            ArgumentNullException.ThrowIfNull(document);

            document.WriteElementExtensions(writer, version);
        }

        protected static void WriteElementExtensions(XmlWriter writer, Workspace workspace, string version)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            workspace.WriteElementExtensions(writer, version);
        }

        protected static void WriteElementExtensions(XmlWriter writer, ResourceCollectionInfo collection, string version)
        {
            ArgumentNullException.ThrowIfNull(collection);

            collection.WriteElementExtensions(writer, version);
        }

        protected static void WriteElementExtensions(XmlWriter writer, CategoriesDocument categories, string version)
        {
            ArgumentNullException.ThrowIfNull(categories);

            categories.WriteElementExtensions(writer, version);
        }

        protected virtual ServiceDocument CreateDocumentInstance() => new ServiceDocument();

        protected virtual void SetDocument(ServiceDocument document) => _document = document;
    }
}
