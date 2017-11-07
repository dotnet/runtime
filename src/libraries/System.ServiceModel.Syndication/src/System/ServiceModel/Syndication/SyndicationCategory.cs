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
    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    public class SyndicationCategory : IExtensibleSyndicationObject
    {
        private ExtensibleSyndicationObject _extensions = new ExtensibleSyndicationObject();
        private string _label;
        private string _name;
        private string _scheme;

        public SyndicationCategory()
            : this((string)null)
        {
        }

        public SyndicationCategory(string name)
            : this(name, null, null)
        {
        }

        public SyndicationCategory(string name, string scheme, string label)
        {
            _name = name;
            _scheme = scheme;
            _label = label;
        }

        protected SyndicationCategory(SyndicationCategory source)
        {
            if (source == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("source");
            }
            _label = source._label;
            _name = source._name;
            _scheme = source._scheme;
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

        public string Label
        {
            get { return _label; }
            set { _label = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Scheme
        {
            get { return _scheme; }
            set { _scheme = value; }
        }

        public virtual SyndicationCategory Clone()
        {
            return new SyndicationCategory(this);
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
