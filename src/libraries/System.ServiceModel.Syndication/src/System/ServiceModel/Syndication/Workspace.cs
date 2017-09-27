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

    public class Workspace : IExtensibleSyndicationObject
    {
        private Uri _baseUri;
        private Collection<ResourceCollectionInfo> _collections;
        private ExtensibleSyndicationObject _extensions = new ExtensibleSyndicationObject();
        private TextSyndicationContent _title;

        public Workspace()
        {
        }

        public Workspace(string title, IEnumerable<ResourceCollectionInfo> collections)
            : this((title != null) ? new TextSyndicationContent(title) : null, collections)
        {
        }

        public Workspace(TextSyndicationContent title, IEnumerable<ResourceCollectionInfo> collections)
        {
            _title = title;
            if (collections != null)
            {
                _collections = new NullNotAllowedCollection<ResourceCollectionInfo>();
                foreach (ResourceCollectionInfo collection in collections)
                {
                    _collections.Add(collection);
                }
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

        public Collection<ResourceCollectionInfo> Collections
        {
            get
            {
                if (_collections == null)
                {
                    _collections = new NullNotAllowedCollection<ResourceCollectionInfo>();
                }
                return _collections;
            }
        }

        public SyndicationElementExtensionCollection ElementExtensions
        {
            get
            {
                return _extensions.ElementExtensions;
            }
        }

        public TextSyndicationContent Title
        {
            get { return _title; }
            set { _title = value; }
        }

        protected internal virtual ResourceCollectionInfo CreateResourceCollection()
        {
            return new ResourceCollectionInfo();
        }

        protected internal virtual bool TryParseAttribute(string name, string ns, string value, string version)
        {
            return false;
        }

        protected internal virtual bool TryParseElement(XmlReader reader, string version)
        {
            return false;
        }

        protected internal virtual Task WriteAttributeExtensions(XmlWriter writer, string version)
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
    }
}
