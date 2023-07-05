// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Collections.Generic;

namespace System.ServiceModel.Syndication
{
    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    public class SyndicationCategory : IExtensibleSyndicationObject
    {
        private ExtensibleSyndicationObject _extensions;

        public SyndicationCategory() : this((string)null)
        {
        }

        public SyndicationCategory(string name) : this(name, null, null)
        {
        }

        public SyndicationCategory(string name, string scheme, string label)
        {
            Name = name;
            Scheme = scheme;
            Label = label;
        }

        protected SyndicationCategory(SyndicationCategory source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Label = source.Label;
            Name = source.Name;
            Scheme = source.Scheme;
            _extensions = source._extensions.Clone();
        }

        public Dictionary<XmlQualifiedName, string> AttributeExtensions => _extensions.AttributeExtensions;

        public SyndicationElementExtensionCollection ElementExtensions => _extensions.ElementExtensions;

        public string Label { get; set; }

        public string Name { get; set; }

        public string Scheme { get; set; }

        public virtual SyndicationCategory Clone() => new SyndicationCategory(this);

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
