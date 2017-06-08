//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace System.ServiceModel.Syndication
{
    using System.Collections.ObjectModel;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using System.Collections.Generic;
    using System.Xml;
    using System.Runtime.CompilerServices;

    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    [DataContract]
    public abstract class ServiceDocumentFormatter
    {
        ServiceDocument document;

        protected ServiceDocumentFormatter()
        {
        }
        protected ServiceDocumentFormatter(ServiceDocument documentToWrite)
        {
            if (documentToWrite == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("documentToWrite");
            }
            this.document = documentToWrite;
        }

        public ServiceDocument Document
        {
            get { return this.document; }
        }

        public abstract string Version
        { get; }

        public abstract bool CanRead(XmlReader reader);
        public abstract void ReadFrom(XmlReader reader);
        public abstract void WriteTo(XmlWriter writer);

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, CategoriesDocument categories)
        {
            if (categories == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("categories");
            }
            Atom10FeedFormatter.CloseBuffer(buffer, writer);
            categories.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, ResourceCollectionInfo collection)
        {
            if (collection == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("collection");
            }
            Atom10FeedFormatter.CloseBuffer(buffer, writer);
            collection.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, Workspace workspace)
        {
            if (workspace == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("workspace");
            }
            Atom10FeedFormatter.CloseBuffer(buffer, writer);
            workspace.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, ServiceDocument document)
        {
            if (document == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("document");
            }
            Atom10FeedFormatter.CloseBuffer(buffer, writer);
            document.LoadElementExtensions(buffer);
        }

        protected static SyndicationCategory CreateCategory(InlineCategoriesDocument inlineCategories)
        {
            if (inlineCategories == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("inlineCategories");
            }
            return inlineCategories.CreateCategory();
        }

        protected static ResourceCollectionInfo CreateCollection(Workspace workspace)
        {
            if (workspace == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("workspace");
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
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("document");
            }
            return document.CreateWorkspace();
        }

        protected static void LoadElementExtensions(XmlReader reader, CategoriesDocument categories, int maxExtensionSize)
        {
            if (categories == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("categories");
            }
            categories.LoadElementExtensions(reader, maxExtensionSize);
        }

        protected static void LoadElementExtensions(XmlReader reader, ResourceCollectionInfo collection, int maxExtensionSize)
        {
            if (collection == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("collection");
            }
            collection.LoadElementExtensions(reader, maxExtensionSize);
        }

        protected static void LoadElementExtensions(XmlReader reader, Workspace workspace, int maxExtensionSize)
        {
            if (workspace == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("workspace");
            }
            workspace.LoadElementExtensions(reader, maxExtensionSize);
        }

        protected static void LoadElementExtensions(XmlReader reader, ServiceDocument document, int maxExtensionSize)
        {
            if (document == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("document");
            }
            document.LoadElementExtensions(reader, maxExtensionSize);
        }

        protected static bool TryParseAttribute(string name, string ns, string value, ServiceDocument document, string version)
        {
            if (document == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("document");
            }
            return document.TryParseAttribute(name, ns, value, version);
        }

        protected static bool TryParseAttribute(string name, string ns, string value, ResourceCollectionInfo collection, string version)
        {
            if (collection == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("collection");
            }
            return collection.TryParseAttribute(name, ns, value, version);
        }

        protected static bool TryParseAttribute(string name, string ns, string value, CategoriesDocument categories, string version)
        {
            if (categories == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("categories");
            }
            return categories.TryParseAttribute(name, ns, value, version);
        }

        protected static bool TryParseAttribute(string name, string ns, string value, Workspace workspace, string version)
        {
            if (workspace == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("workspace");
            }
            return workspace.TryParseAttribute(name, ns, value, version);
        }

        protected static bool TryParseElement(XmlReader reader, ResourceCollectionInfo collection, string version)
        {
            if (collection == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("collection");
            }
            return collection.TryParseElement(reader, version);
        }

        protected static bool TryParseElement(XmlReader reader, ServiceDocument document, string version)
        {
            if (document == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("document");
            }
            return document.TryParseElement(reader, version);
        }

        protected static bool TryParseElement(XmlReader reader, Workspace workspace, string version)
        {
            if (workspace == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("workspace");
            }
            return workspace.TryParseElement(reader, version);
        }

        protected static bool TryParseElement(XmlReader reader, CategoriesDocument categories, string version)
        {
            if (categories == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("categories");
            }
            return categories.TryParseElement(reader, version);
        }

        protected static void WriteAttributeExtensions(XmlWriter writer, ServiceDocument document, string version)
        {
            if (document == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("document");
            }
            document.WriteAttributeExtensions(writer, version);
        }

        protected static void WriteAttributeExtensions(XmlWriter writer, Workspace workspace, string version)
        {
            if (workspace == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("workspace");
            }
            workspace.WriteAttributeExtensions(writer, version);
        }

        protected static void WriteAttributeExtensions(XmlWriter writer, ResourceCollectionInfo collection, string version)
        {
            if (collection == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("collection");
            }
            collection.WriteAttributeExtensions(writer, version);
        }

        protected static void WriteAttributeExtensions(XmlWriter writer, CategoriesDocument categories, string version)
        {
            if (categories == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("categories");
            }
            categories.WriteAttributeExtensions(writer, version);
        }

        protected static void WriteElementExtensions(XmlWriter writer, ServiceDocument document, string version)
        {
            if (document == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("document");
            }
            document.WriteElementExtensions(writer, version);
        }

        protected static void WriteElementExtensions(XmlWriter writer, Workspace workspace, string version)
        {
            if (workspace == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("workspace");
            }
            workspace.WriteElementExtensions(writer, version);
        }

        protected static void WriteElementExtensions(XmlWriter writer, ResourceCollectionInfo collection, string version)
        {
            if (collection == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("collection");
            }
            collection.WriteElementExtensions(writer, version);
        }

        protected static void WriteElementExtensions(XmlWriter writer, CategoriesDocument categories, string version)
        {
            if (categories == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("categories");
            }
            categories.WriteElementExtensions(writer, version);
        }

        protected virtual ServiceDocument CreateDocumentInstance()
        {
            return new ServiceDocument();
        }

        protected virtual void SetDocument(ServiceDocument document)
        {
            this.document = document;
        }
    }
}
