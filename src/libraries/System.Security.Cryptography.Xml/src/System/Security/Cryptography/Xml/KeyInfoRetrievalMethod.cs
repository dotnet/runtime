// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;

namespace System.Security.Cryptography.Xml
{
    public class KeyInfoRetrievalMethod : KeyInfoClause
    {
        private string _uri;
        private string _type;

        //
        // public constructors
        //

        public KeyInfoRetrievalMethod() { }

        public KeyInfoRetrievalMethod(string strUri)
        {
            _uri = strUri;
        }

        public KeyInfoRetrievalMethod(string strUri, string typeName)
        {
            _uri = strUri;
            _type = typeName;
        }

        //
        // public properties
        //

        public string Uri
        {
            get { return _uri; }
            set { _uri = value; }
        }

        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public override XmlElement GetXml()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.PreserveWhitespace = true;
            return GetXml(xmlDocument);
        }

        internal override XmlElement GetXml(XmlDocument xmlDocument)
        {
            // Create the actual element
            XmlElement retrievalMethodElement = xmlDocument.CreateElement("RetrievalMethod", SignedXml.XmlDsigNamespaceUrl);

            if (!string.IsNullOrEmpty(_uri))
                retrievalMethodElement.SetAttribute("URI", _uri);
            if (!string.IsNullOrEmpty(_type))
                retrievalMethodElement.SetAttribute("Type", _type);

            return retrievalMethodElement;
        }

        public override void LoadXml(XmlElement value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _uri = Utils.GetAttribute(value, "URI", SignedXml.XmlDsigNamespaceUrl);
            _type = Utils.GetAttribute(value, "Type", SignedXml.XmlDsigNamespaceUrl);
        }
    }
}
