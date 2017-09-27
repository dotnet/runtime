// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Xml;

    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    public class SyndicationPerson : IExtensibleSyndicationObject
    {
        private string _email;
        private ExtensibleSyndicationObject _extensions = new ExtensibleSyndicationObject();
        private string _name;
        private string _uri;

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
            _name = name;
            _email = email;
            _uri = uri;
        }

        protected SyndicationPerson(SyndicationPerson source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            _email = source._email;
            _name = source._name;
            _uri = source._uri;
            _extensions = source._extensions.Clone();
        }

        public Dictionary<XmlQualifiedName, string> AttributeExtensions
        {
            get { return _extensions.AttributeExtensions; }
        }

        public SyndicationElementExtensionCollection ElementExtensions
        {
            get { return _extensions.ElementExtensions; }
        }

        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Scope = "property", Justification = "The Uri represents a unique category and not a network location")]
        public string Uri
        {
            get { return _uri; }
            set { _uri = value; }
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

        protected internal virtual Task WriteAttributeExtensionsAsync(XmlWriter writer, string version)
        {
            return _extensions.WriteAttributeExtensionsAsync(writer);
        }

        protected internal virtual Task WriteElementExtensionsAsync(XmlWriter writer, string version)
        {
            return _extensions.WriteElementExtensionsAsync(writer);
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
