// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;

namespace System.ServiceModel.Syndication
{
    public class ServiceDocument : IExtensibleSyndicationObject
    {
        private ExtensibleSyndicationObject _extensions;
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

        public Dictionary<XmlQualifiedName, string> AttributeExtensions => _extensions.AttributeExtensions;

        public Uri BaseUri { get; set; }

        public SyndicationElementExtensionCollection ElementExtensions => _extensions.ElementExtensions;

        public string Language { get; set; }

        public Collection<Workspace> Workspaces
        {
            get => _workspaces ??= new NullNotAllowedCollection<Workspace>();
        }

        public static ServiceDocument Load(XmlReader reader)
        {
            return Load<ServiceDocument>(reader);
        }

        public static TServiceDocument Load<TServiceDocument>(XmlReader reader) where TServiceDocument : ServiceDocument, new()
        {
            AtomPub10ServiceDocumentFormatter<TServiceDocument> formatter = new AtomPub10ServiceDocumentFormatter<TServiceDocument>();
            formatter.ReadFrom(reader);
            return (TServiceDocument)formatter.Document;
        }

        public ServiceDocumentFormatter GetFormatter()
        {
            return new AtomPub10ServiceDocumentFormatter(this);
        }

        public void Save(XmlWriter writer)
        {
            new AtomPub10ServiceDocumentFormatter(this).WriteTo(writer);
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
