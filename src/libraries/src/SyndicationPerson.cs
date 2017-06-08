//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Generic;
    using System.Xml;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;

    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public class SyndicationPerson : IExtensibleSyndicationObject
    {
        string email;
        ExtensibleSyndicationObject extensions = new ExtensibleSyndicationObject();
        string name;
        string uri;

        public SyndicationPerson()
            : this((string)null)
        {
        }

        public SyndicationPerson(string email)
            : this(email, null, null)
        {
        }

        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "2#", Justification = "The Uri represents a unique category and not a network location")]
        public SyndicationPerson(string email, string name, string uri)
        {
            this.name = name;
            this.email = email;
            this.uri = uri;
        }

        protected SyndicationPerson(SyndicationPerson source)
        {
            if (source == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("source");
            }
            this.email = source.email;
            this.name = source.name;
            this.uri = source.uri;
            this.extensions = source.extensions.Clone();
        }

        public Dictionary<XmlQualifiedName, string> AttributeExtensions
        {
            get { return this.extensions.AttributeExtensions; }
        }

        public SyndicationElementExtensionCollection ElementExtensions
        {
            get { return this.extensions.ElementExtensions; }
        }

        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Scope = "property", Justification = "The Uri represents a unique category and not a network location")]
        public string Uri
        {
            get { return uri; }
            set { uri = value; }
        }

        public virtual SyndicationPerson Clone()
        {
            return new SyndicationPerson(this);
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
