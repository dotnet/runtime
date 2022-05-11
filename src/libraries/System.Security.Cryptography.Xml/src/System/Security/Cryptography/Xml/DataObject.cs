// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;

namespace System.Security.Cryptography.Xml
{
    public class DataObject
    {
        private string _id;
        private string _mimeType;
        private string _encoding;
        private CanonicalXmlNodeList _elData;
        private XmlElement _cachedXml;

        //
        // public constructors
        //

        public DataObject()
        {
            _cachedXml = null;
            _elData = new CanonicalXmlNodeList();
        }

        public DataObject(string id, string mimeType, string encoding, XmlElement data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _id = id;
            _mimeType = mimeType;
            _encoding = encoding;
            _elData = new CanonicalXmlNodeList();
            _elData.Add(data);
            _cachedXml = null;
        }

        //
        // public properties
        //

        public string Id
        {
            get { return _id; }
            set
            {
                _id = value;
                _cachedXml = null;
            }
        }

        public string MimeType
        {
            get { return _mimeType; }
            set
            {
                _mimeType = value;
                _cachedXml = null;
            }
        }

        public string Encoding
        {
            get { return _encoding; }
            set
            {
                _encoding = value;
                _cachedXml = null;
            }
        }

        public XmlNodeList Data
        {
            get { return _elData; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                // Reset the node list
                _elData = new CanonicalXmlNodeList();
                foreach (XmlNode node in value)
                {
                    _elData.Add(node);
                }
                _cachedXml = null;
            }
        }

        private bool CacheValid
        {
            get
            {
                return (_cachedXml != null);
            }
        }

        //
        // public methods
        //

        public XmlElement GetXml()
        {
            if (CacheValid) return (_cachedXml);

            XmlDocument document = new XmlDocument();
            document.PreserveWhitespace = true;
            return GetXml(document);
        }

        internal XmlElement GetXml(XmlDocument document)
        {
            XmlElement objectElement = document.CreateElement("Object", SignedXml.XmlDsigNamespaceUrl);

            if (!string.IsNullOrEmpty(_id))
                objectElement.SetAttribute("Id", _id);
            if (!string.IsNullOrEmpty(_mimeType))
                objectElement.SetAttribute("MimeType", _mimeType);
            if (!string.IsNullOrEmpty(_encoding))
                objectElement.SetAttribute("Encoding", _encoding);

            if (_elData != null)
            {
                foreach (XmlNode node in _elData)
                {
                    objectElement.AppendChild(document.ImportNode(node, true));
                }
            }

            return objectElement;
        }

        public void LoadXml(XmlElement value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _id = Utils.GetAttribute(value, "Id", SignedXml.XmlDsigNamespaceUrl);
            _mimeType = Utils.GetAttribute(value, "MimeType", SignedXml.XmlDsigNamespaceUrl);
            _encoding = Utils.GetAttribute(value, "Encoding", SignedXml.XmlDsigNamespaceUrl);

            foreach (XmlNode node in value.ChildNodes)
            {
                _elData.Add(node);
            }

            // Save away the cached value
            _cachedXml = value;
        }
    }
}
