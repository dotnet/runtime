// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;

namespace System.ServiceModel.Syndication
{
    public class ResourceCollectionInfo : IExtensibleSyndicationObject
    {
        private static IEnumerable<string> s_singleEmptyAccept;
        private Collection<string> _accepts;
        private Collection<CategoriesDocument> _categories;
        private ExtensibleSyndicationObject _extensions;

        public ResourceCollectionInfo()
        {
        }

        public ResourceCollectionInfo(string title, Uri link) : this((title == null) ? null : new TextSyndicationContent(title), link)
        {
        }

        public ResourceCollectionInfo(TextSyndicationContent title, Uri link) : this(title, link, null, null)
        {
        }

        public ResourceCollectionInfo(TextSyndicationContent title, Uri link, IEnumerable<CategoriesDocument> categories, bool allowsNewEntries)
            : this(title, link, categories, (allowsNewEntries) ? null : CreateSingleEmptyAccept())
        {
        }

        public ResourceCollectionInfo(TextSyndicationContent title, Uri link, IEnumerable<CategoriesDocument> categories, IEnumerable<string> accepts)
        {
            if (title is null)
            {
                throw new ArgumentNullException(nameof(title));
            }
            if (link is null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            Title = title;
            Link = link;

            if (categories != null)
            {
                _categories = new NullNotAllowedCollection<CategoriesDocument>();
                foreach (CategoriesDocument category in categories)
                {
                    _categories.Add(category);
                }
            }

            if (accepts != null)
            {
                _accepts = new NullNotAllowedCollection<string>();
                foreach (string accept in accepts)
                {
                    _accepts.Add(accept);
                }
            }
        }

        public Collection<string> Accepts
        {
            get => _accepts ??= new NullNotAllowedCollection<string>();
        }

        public Dictionary<XmlQualifiedName, string> AttributeExtensions => _extensions.AttributeExtensions;

        public Uri BaseUri { get; set; }

        public Collection<CategoriesDocument> Categories
        {
            get => _categories ??= new NullNotAllowedCollection<CategoriesDocument>();
        }

        public SyndicationElementExtensionCollection ElementExtensions => _extensions.ElementExtensions;

        public Uri Link { get; set; }

        public TextSyndicationContent Title { get; set; }

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
            _extensions.WriteAttributeExtensions(writer);
        }

        protected internal virtual void WriteElementExtensions(XmlWriter writer, string version)
        {
            _extensions.WriteElementExtensions(writer);
        }

        internal void LoadElementExtensions(XmlReader readerOverUnparsedExtensions, int maxExtensionSize)
        {
            _extensions.LoadElementExtensions(readerOverUnparsedExtensions, maxExtensionSize);
        }

        internal void LoadElementExtensions(XmlBuffer buffer)
        {
            _extensions.LoadElementExtensions(buffer);
        }

        private static IEnumerable<string> CreateSingleEmptyAccept() =>
            s_singleEmptyAccept ??= new List<string>(1) { string.Empty }.AsReadOnly();
    }
}
