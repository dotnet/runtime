// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;

namespace System.Security.Cryptography.Xml
{
    public class KeyInfoName : KeyInfoClause
    {
        private string? _keyName;

        //
        // public constructors
        //

        public KeyInfoName() : this(null) { }

        public KeyInfoName(string? keyName)
        {
            Value = keyName;
        }

        //
        // public properties
        //

        public string? Value
        {
            get { return _keyName; }
            set { _keyName = value; }
        }

        //
        // public methods
        //

        public override XmlElement GetXml()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.PreserveWhitespace = true;
            return GetXml(xmlDocument);
        }

        internal override XmlElement GetXml(XmlDocument xmlDocument)
        {
            XmlElement nameElement = xmlDocument.CreateElement("KeyName", SignedXml.XmlDsigNamespaceUrl);
            nameElement.AppendChild(xmlDocument.CreateTextNode(_keyName));
            return nameElement;
        }

        public override void LoadXml(XmlElement value)
        {
            ArgumentNullException.ThrowIfNull(value);

            XmlElement nameElement = value;
            _keyName = nameElement.InnerText.Trim();
        }
    }
}
