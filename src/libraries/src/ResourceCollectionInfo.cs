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
    public class ResourceCollectionInfo : IExtensibleSyndicationObject
    {
        static IEnumerable<string> singleEmptyAccept;
        Collection<string> accepts;
        Uri baseUri;
        Collection<CategoriesDocument> categories;
        ExtensibleSyndicationObject extensions = new ExtensibleSyndicationObject();
        Uri link;
        TextSyndicationContent title;

        public ResourceCollectionInfo()
        {
        }

        public ResourceCollectionInfo(string title, Uri link)
            : this((title == null) ? null : new TextSyndicationContent(title), link)
        {
        }

        public ResourceCollectionInfo(TextSyndicationContent title, Uri link)
            : this(title, link, null, null)
        {
        }

        public ResourceCollectionInfo(TextSyndicationContent title, Uri link, IEnumerable<CategoriesDocument> categories, bool allowsNewEntries)
            : this(title, link, categories, (allowsNewEntries) ? null : CreateSingleEmptyAccept())
        {
        }

        public ResourceCollectionInfo(TextSyndicationContent title, Uri link, IEnumerable<CategoriesDocument> categories, IEnumerable<string> accepts)
        {
            if (title == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("title");
            }
            if (link == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("link");
            }
            this.title = title;
            this.link = link;
            if (categories != null)
            {
                this.categories = new NullNotAllowedCollection<CategoriesDocument>();
                foreach (CategoriesDocument category in categories)
                {
                    this.categories.Add(category);
                }
            }
            if (accepts != null)
            {
                this.accepts = new NullNotAllowedCollection<string>();
                foreach (string accept in accepts)
                {
                    this.accepts.Add(accept);
                }
            }
        }

        public Collection<string> Accepts
        {
            get
            {
                if (this.accepts == null)
                {
                    this.accepts = new NullNotAllowedCollection<string>();
                }
                return this.accepts;
            }
        }

        public Dictionary<XmlQualifiedName, string> AttributeExtensions
        {
            get
            {
                return this.extensions.AttributeExtensions;
            }
        }

        public Uri BaseUri
        {
            get { return this.baseUri; }
            set { this.baseUri = value; }
        }

        public Collection<CategoriesDocument> Categories
        {
            get
            {
                if (this.categories == null)
                {
                    this.categories = new NullNotAllowedCollection<CategoriesDocument>();
                }
                return this.categories;
            }
        }

        public SyndicationElementExtensionCollection ElementExtensions
        {
            get
            {
                return this.extensions.ElementExtensions;
            }
        }

        public Uri Link
        {
            get { return this.link; }
            set { this.link = value; }
        }

        public TextSyndicationContent Title
        {
            get { return this.title; }
            set { this.title = value; }
        }

        protected internal virtual InlineCategoriesDocument CreateInlineCategoriesDocument()
        {
            return new InlineCategoriesDocument();
        }

        protected internal virtual ReferencedCategoriesDocument CreateReferencedCategoriesDocument()
        {
            return new ReferencedCategoriesDocument();
        }

        protected internal virtual bool TryParseAttribute(string name, string ns, string value, string version)
        {
            return false;
        }

        protected internal virtual bool TryParseElement(XmlReader reader, string version)
        {
            return false;
        }

        protected internal virtual void WriteAttributeExtensions(XmlWriter writer, string version)
        {
            this.extensions.WriteAttributeExtensions(writer);
        }

        protected internal virtual void WriteElementExtensions(XmlWriter writer, string version)
        {
            this.extensions.WriteElementExtensions(writer);
        }

        internal void LoadElementExtensions(XmlReader readerOverUnparsedExtensions, int maxExtensionSize)
        {
            this.extensions.LoadElementExtensions(readerOverUnparsedExtensions, maxExtensionSize);
        }

        internal void LoadElementExtensions(XmlBuffer buffer)
        {
            this.extensions.LoadElementExtensions(buffer);
        }

        static IEnumerable<string> CreateSingleEmptyAccept()
        {
            if (singleEmptyAccept == null)
            {
                List<string> tmp = new List<string>(1);
                tmp.Add(string.Empty);
                singleEmptyAccept = tmp.AsReadOnly();
            }
            return singleEmptyAccept;
        }
    }
}
