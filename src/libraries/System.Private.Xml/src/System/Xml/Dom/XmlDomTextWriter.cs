// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.IO;
using System.Text;

namespace System.Xml
{
    // Represents a writer that will make it possible to work with prefixes even
    // if the namespace is not specified.
    // This is not possible with XmlTextWriter. But this class inherits XmlTextWriter.
    internal class XmlDOMTextWriter : XmlTextWriter
    {
        public XmlDOMTextWriter(Stream w, Encoding? encoding) : base(w, encoding)
        {
        }

        public XmlDOMTextWriter(string filename, Encoding? encoding) : base(filename, encoding)
        {
        }

        public XmlDOMTextWriter(TextWriter w) : base(w)
        {
        }

        // Overrides the baseclass implementation so that emptystring prefixes do
        // do not fail if namespace is not specified.
        public override void WriteStartElement(string? prefix, string localName, string? ns)
        {
            if (string.IsNullOrEmpty(ns) && !string.IsNullOrEmpty(prefix))
                prefix = string.Empty;

            base.WriteStartElement(prefix, localName, ns);
        }

        // Overrides the baseclass implementation so that emptystring prefixes do
        // do not fail if namespace is not specified.
        public override void WriteStartAttribute(string? prefix, string localName, string? ns)
        {
            if (string.IsNullOrEmpty(ns) && !string.IsNullOrEmpty(prefix))
                prefix = string.Empty;

            base.WriteStartAttribute(prefix, localName, ns);
        }
    }
}
