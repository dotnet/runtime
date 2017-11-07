// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Runtime.CompilerServices;

namespace System.ServiceModel.Syndication
{
    public abstract class CategoriesDocument : IExtensibleSyndicationObject
    {
        private Uri _baseUri;
        private ExtensibleSyndicationObject _extensions = new ExtensibleSyndicationObject();
        private string _language;

        internal CategoriesDocument()
        {
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

        public SyndicationElementExtensionCollection ElementExtensions
        {
            get
            {
                return _extensions.ElementExtensions;
            }
        }

        public string Language
        {
            get { return _language; }
            set { _language = value; }
        }

        internal abstract bool IsInline
        {
            get;
        }

        public static InlineCategoriesDocument Create(Collection<SyndicationCategory> categories)
        {
            return new InlineCategoriesDocument(categories);
        }

        public static InlineCategoriesDocument Create(Collection<SyndicationCategory> categories, bool isFixed, string scheme)
        {
            return new InlineCategoriesDocument(categories, isFixed, scheme);
        }

        public static ReferencedCategoriesDocument Create(Uri linkToCategoriesDocument)
        {
            return new ReferencedCategoriesDocument(linkToCategoriesDocument);
        }

        public static CategoriesDocument Load(XmlReader reader)
        {
            AtomPub10CategoriesDocumentFormatter formatter = new AtomPub10CategoriesDocumentFormatter();
            formatter.ReadFrom(reader);
            return formatter.Document;
        }

        public CategoriesDocumentFormatter GetFormatter()
        {
            return new AtomPub10CategoriesDocumentFormatter(this);
        }

        public void Save(XmlWriter writer)
        {
            this.GetFormatter().WriteTo(writer);
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
