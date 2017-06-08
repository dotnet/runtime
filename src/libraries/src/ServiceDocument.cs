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
    public class ServiceDocument : IExtensibleSyndicationObject
    {
        Uri baseUri;
        ExtensibleSyndicationObject extensions = new ExtensibleSyndicationObject();
        string language;
        Collection<Workspace> workspaces;

        public ServiceDocument() : this(null)
        {
        }

        public ServiceDocument(IEnumerable<Workspace> workspaces)
        {
            if (workspaces != null)
            {
                this.workspaces = new NullNotAllowedCollection<Workspace>();
                foreach (Workspace workspace in workspaces)
                {
                    this.workspaces.Add(workspace);
                }
            }
        }

        public Dictionary<XmlQualifiedName, string> AttributeExtensions
        {
            get { return this.extensions.AttributeExtensions; }
        }

        public Uri BaseUri
        {
            get { return this.baseUri; }
            set { this.baseUri = value; }
        }

        public SyndicationElementExtensionCollection ElementExtensions
        {
            get { return this.extensions.ElementExtensions; }
        }

        public string Language
        {
            get { return this.language; }
            set { this.language = value; }
        }

        public Collection<Workspace> Workspaces
        {
            get
            {
                if (this.workspaces == null)
                {
                    this.workspaces = new NullNotAllowedCollection<Workspace>();
                }
                return this.workspaces;
            }
        }

        public static ServiceDocument Load(XmlReader reader)
        {
            return Load<ServiceDocument>(reader);
        }

        public static TServiceDocument Load<TServiceDocument>(XmlReader reader)
            where TServiceDocument : ServiceDocument, new ()
        {
            AtomPub10ServiceDocumentFormatter<TServiceDocument> formatter = new AtomPub10ServiceDocumentFormatter<TServiceDocument>();
            formatter.ReadFrom(reader);
            return (TServiceDocument)(object) formatter.Document;
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
    }
}
