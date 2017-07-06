// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.ServiceModel.Syndication
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Xml;

    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public class ServiceDocument : IExtensibleSyndicationObject
    {
        private Uri _baseUri;
        private ExtensibleSyndicationObject _extensions = new ExtensibleSyndicationObject();
        private string _language;
        private Collection<Workspace> _workspaces;

        public ServiceDocument() : this(null)
        {
        }

        public ServiceDocument(IEnumerable<Workspace> workspaces)
        {
            if (workspaces != null)
            {
                _workspaces = new NullNotAllowedCollection<Workspace>();
                foreach (Workspace workspace in workspaces)
                {
                    _workspaces.Add(workspace);
                }
            }
        }

        public Dictionary<XmlQualifiedName, string> AttributeExtensions
        {
            get { return _extensions.AttributeExtensions; }
        }

        public Uri BaseUri
        {
            get { return _baseUri; }
            set { _baseUri = value; }
        }

        public SyndicationElementExtensionCollection ElementExtensions
        {
            get { return _extensions.ElementExtensions; }
        }

        public string Language
        {
            get { return _language; }
            set { _language = value; }
        }

        public Collection<Workspace> Workspaces
        {
            get
            {
                if (_workspaces == null)
                {
                    _workspaces = new NullNotAllowedCollection<Workspace>();
                }
                return _workspaces;
            }
        }

        public static Task<ServiceDocument> Load(XmlReader reader)
        {
            return LoadAsync<ServiceDocument>(reader);
        }

        public static async Task<TServiceDocument> LoadAsync<TServiceDocument>(XmlReader reader)
            where TServiceDocument : ServiceDocument, new()
        {
            AtomPub10ServiceDocumentFormatter<TServiceDocument> formatter = new AtomPub10ServiceDocumentFormatter<TServiceDocument>();
            await formatter.ReadFromAsync(reader);
            return (TServiceDocument)(object)formatter.Document;
        }

        public ServiceDocumentFormatter GetFormatter()
        {
            return new AtomPub10ServiceDocumentFormatter(this);
        }

        public async Task Save(XmlWriter writer)
        {
            await new AtomPub10ServiceDocumentFormatter(this).WriteTo(writer);
        }

        protected internal virtual Workspace CreateWorkspace()
        {
            return new Workspace();
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
