// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Xml;

namespace System.ServiceModel.Syndication
{
    public class Workspace : IExtensibleSyndicationObject
    {
        private Collection<ResourceCollectionInfo> _collections;
        private ExtensibleSyndicationObject _extensions = new ExtensibleSyndicationObject();

        public Workspace()
        {
        }

        public Workspace(string title, IEnumerable<ResourceCollectionInfo> collections) : this((title != null) ? new TextSyndicationContent(title) : null, collections)
        {
        }

        public Workspace(TextSyndicationContent title, IEnumerable<ResourceCollectionInfo> collections)
        {
            Title = title;
            if (collections != null)
            {
                _collections = new NullNotAllowedCollection<ResourceCollectionInfo>();
                foreach (ResourceCollectionInfo collection in collections)
                {
                    _collections.Add(collection);
                }
            }
        }

        public Dictionary<XmlQualifiedName, string> AttributeExtensions => _extensions.AttributeExtensions;

        public Uri BaseUri { get; set; }

        public Collection<ResourceCollectionInfo> Collections
        {
            get => _collections ?? (_collections = new NullNotAllowedCollection<ResourceCollectionInfo>());
        }

        public SyndicationElementExtensionCollection ElementExtensions => _extensions.ElementExtensions;

        public TextSyndicationContent Title { get; set; }

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
    }
}
