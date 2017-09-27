// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Xml;

    public class ResourceCollectionInfo : IExtensibleSyndicationObject
    {
        private static IEnumerable<string> s_singleEmptyAccept;
        private Collection<string> _accepts;
        private Uri _baseUri;
        private Collection<CategoriesDocument> _categories;
        private ExtensibleSyndicationObject _extensions = new ExtensibleSyndicationObject();
        private Uri _link;
        private TextSyndicationContent _title;

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
                throw new ArgumentNullException(nameof(title));
            }
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }
            _title = title;
            _link = link;
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
            get
            {
                if (_accepts == null)
                {
                    _accepts = new NullNotAllowedCollection<string>();
                }
                return _accepts;
            }
        }

        public Dictionary<XmlQualifiedName, string> AttributeExtensions
        {
            get
            {
                return _extensions.AttributeExtensions;
            }
        }

        public Uri BaseUri
        {
            get { return _baseUri; }
            set { _baseUri = value; }
        }

        public Collection<CategoriesDocument> Categories
        {
            get
            {
                if (_categories == null)
                {
                    _categories = new NullNotAllowedCollection<CategoriesDocument>();
                }
                return _categories;
            }
        }

        public SyndicationElementExtensionCollection ElementExtensions
        {
            get
            {
                return _extensions.ElementExtensions;
            }
        }

        public Uri Link
        {
            get { return _link; }
            set { _link = value; }
        }

        public TextSyndicationContent Title
        {
            get { return _title; }
            set { _title = value; }
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

        protected internal virtual Task WriteAttributeExtensionsAsync(XmlWriter writer, string version)
        {
            return _extensions.WriteAttributeExtensionsAsync(writer);
        }

        protected internal virtual Task WriteElementExtensionsAsync(XmlWriter writer, string version)
        {
            return _extensions.WriteElementExtensionsAsync(writer);
        }

        internal void LoadElementExtensions(XmlReaderWrapper readerOverUnparsedExtensions, int maxExtensionSize)
        {
            _extensions.LoadElementExtensions(readerOverUnparsedExtensions, maxExtensionSize);
        }

        internal void LoadElementExtensions(XmlBuffer buffer)
        {
            _extensions.LoadElementExtensions(buffer);
        }

        private static IEnumerable<string> CreateSingleEmptyAccept()
        {
            if (s_singleEmptyAccept == null)
            {
                List<string> tmp = new List<string>(1);
                tmp.Add(string.Empty);
                s_singleEmptyAccept = tmp.AsReadOnly();
            }
            return s_singleEmptyAccept;
        }
    }
}
