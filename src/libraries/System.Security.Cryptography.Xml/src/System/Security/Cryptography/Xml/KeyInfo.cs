// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    public class KeyInfo : IEnumerable
    {
        private string _id = null;
        private ArrayList _keyInfoClauses;

        //
        // public constructors
        //

        public KeyInfo()
        {
            _keyInfoClauses = new ArrayList();
        }

        //
        // public properties
        //

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public XmlElement GetXml()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.PreserveWhitespace = true;
            return GetXml(xmlDocument);
        }

        internal XmlElement GetXml(XmlDocument xmlDocument)
        {
            // Create the KeyInfo element itself
            XmlElement keyInfoElement = xmlDocument.CreateElement("KeyInfo", SignedXml.XmlDsigNamespaceUrl);
            if (!string.IsNullOrEmpty(_id))
            {
                keyInfoElement.SetAttribute("Id", _id);
            }

            // Add all the clauses that go underneath it
            for (int i = 0; i < _keyInfoClauses.Count; ++i)
            {
                XmlElement xmlElement = ((KeyInfoClause)_keyInfoClauses[i]).GetXml(xmlDocument);
                if (xmlElement != null)
                {
                    keyInfoElement.AppendChild(xmlElement);
                }
            }
            return keyInfoElement;
        }

        public void LoadXml(XmlElement value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            XmlElement keyInfoElement = value;
            _id = Utils.GetAttribute(keyInfoElement, "Id", SignedXml.XmlDsigNamespaceUrl);

            XmlNode child = keyInfoElement.FirstChild;
            while (child != null)
            {
                XmlElement elem = child as XmlElement;
                if (elem != null)
                {
                    // Create the right type of KeyInfoClause; we use a combination of the namespace and tag name (local name)
                    string kicString = elem.NamespaceURI + " " + elem.LocalName;
                    // Special-case handling for KeyValue -- we have to go one level deeper
                    if (kicString == "http://www.w3.org/2000/09/xmldsig# KeyValue")
                    {
                        XmlNodeList nodeList2 = elem.ChildNodes;
                        foreach (XmlNode node2 in nodeList2)
                        {
                            XmlElement elem2 = node2 as XmlElement;
                            if (elem2 != null)
                            {
                                kicString += "/" + elem2.LocalName;
                                break;
                            }
                        }
                    }
                    KeyInfoClause keyInfoClause = (KeyInfoClause)CryptoHelpers.CreateFromName(kicString);
                    // if we don't know what kind of KeyInfoClause we're looking at, use a generic KeyInfoNode:
                    if (keyInfoClause == null)
                        keyInfoClause = new KeyInfoNode();

                    // Ask the create clause to fill itself with the corresponding XML
                    keyInfoClause.LoadXml(elem);
                    // Add it to our list of KeyInfoClauses
                    AddClause(keyInfoClause);
                }
                child = child.NextSibling;
            }
        }

        public int Count
        {
            get { return _keyInfoClauses.Count; }
        }

        //
        // public constructors
        //

        public void AddClause(KeyInfoClause clause)
        {
            _keyInfoClauses.Add(clause);
        }

        public IEnumerator GetEnumerator()
        {
            return _keyInfoClauses.GetEnumerator();
        }

        public IEnumerator GetEnumerator(Type requestedObjectType)
        {
            ArrayList requestedList = new ArrayList();

            object tempObj;
            IEnumerator tempEnum = _keyInfoClauses.GetEnumerator();

            while (tempEnum.MoveNext())
            {
                tempObj = tempEnum.Current;
                if (requestedObjectType.Equals(tempObj.GetType()))
                    requestedList.Add(tempObj);
            }

            return requestedList.GetEnumerator();
        }
    }
}
