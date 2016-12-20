// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Collections;
using System.Security.Cryptography;

namespace System.Security.Cryptography.Xml
{
    // the class that provides node subset state and canonicalization function to XmlElement
    internal class CanonicalXmlElement : XmlElement, ICanonicalizableNode
    {
        private bool _isInNodeSet;

        public CanonicalXmlElement(string prefix, string localName, string namespaceURI, XmlDocument doc, bool defaultNodeSetInclusionState)
            : base(prefix, localName, namespaceURI, doc)
        {
            _isInNodeSet = defaultNodeSetInclusionState;
        }

        public bool IsInNodeSet
        {
            get { return _isInNodeSet; }
            set { _isInNodeSet = value; }
        }

        public void Write(StringBuilder strBuilder, DocPosition docPos, AncestralNamespaceContextManager anc)
        {
            Hashtable nsLocallyDeclared = new Hashtable();
            SortedList nsListToRender = new SortedList(new NamespaceSortOrder());
            SortedList attrListToRender = new SortedList(new AttributeSortOrder());

            XmlAttributeCollection attrList = Attributes;
            if (attrList != null)
            {
                foreach (XmlAttribute attr in attrList)
                {
                    if (((CanonicalXmlAttribute)attr).IsInNodeSet || Utils.IsNamespaceNode(attr) || Utils.IsXmlNamespaceNode(attr))
                    {
                        if (Utils.IsNamespaceNode(attr))
                        {
                            anc.TrackNamespaceNode(attr, nsListToRender, nsLocallyDeclared);
                        }
                        else if (Utils.IsXmlNamespaceNode(attr))
                        {
                            anc.TrackXmlNamespaceNode(attr, nsListToRender, attrListToRender, nsLocallyDeclared);
                        }
                        else if (IsInNodeSet)
                        {
                            attrListToRender.Add(attr, null);
                        }
                    }
                }
            }

            if (!Utils.IsCommittedNamespace(this, Prefix, NamespaceURI))
            {
                string name = ((Prefix.Length > 0) ? "xmlns" + ":" + Prefix : "xmlns");
                XmlAttribute nsattrib = (XmlAttribute)OwnerDocument.CreateAttribute(name);
                nsattrib.Value = NamespaceURI;
                anc.TrackNamespaceNode(nsattrib, nsListToRender, nsLocallyDeclared);
            }

            if (IsInNodeSet)
            {
                anc.GetNamespacesToRender(this, attrListToRender, nsListToRender, nsLocallyDeclared);

                strBuilder.Append("<" + Name);
                foreach (object attr in nsListToRender.GetKeyList())
                {
                    (attr as CanonicalXmlAttribute).Write(strBuilder, docPos, anc);
                }
                foreach (object attr in attrListToRender.GetKeyList())
                {
                    (attr as CanonicalXmlAttribute).Write(strBuilder, docPos, anc);
                }
                strBuilder.Append(">");
            }

            anc.EnterElementContext();
            anc.LoadUnrenderedNamespaces(nsLocallyDeclared);
            anc.LoadRenderedNamespaces(nsListToRender);

            XmlNodeList childNodes = ChildNodes;
            foreach (XmlNode childNode in childNodes)
            {
                CanonicalizationDispatcher.Write(childNode, strBuilder, docPos, anc);
            }

            anc.ExitElementContext();

            if (IsInNodeSet)
            {
                strBuilder.Append("</" + Name + ">");
            }
        }

        public void WriteHash(HashAlgorithm hash, DocPosition docPos, AncestralNamespaceContextManager anc)
        {
            Hashtable nsLocallyDeclared = new Hashtable();
            SortedList nsListToRender = new SortedList(new NamespaceSortOrder());
            SortedList attrListToRender = new SortedList(new AttributeSortOrder());
            UTF8Encoding utf8 = new UTF8Encoding(false);
            byte[] rgbData;

            XmlAttributeCollection attrList = Attributes;
            if (attrList != null)
            {
                foreach (XmlAttribute attr in attrList)
                {
                    if (((CanonicalXmlAttribute)attr).IsInNodeSet || Utils.IsNamespaceNode(attr) || Utils.IsXmlNamespaceNode(attr))
                    {
                        if (Utils.IsNamespaceNode(attr))
                        {
                            anc.TrackNamespaceNode(attr, nsListToRender, nsLocallyDeclared);
                        }
                        else if (Utils.IsXmlNamespaceNode(attr))
                        {
                            anc.TrackXmlNamespaceNode(attr, nsListToRender, attrListToRender, nsLocallyDeclared);
                        }
                        else if (IsInNodeSet)
                        {
                            attrListToRender.Add(attr, null);
                        }
                    }
                }
            }

            if (!Utils.IsCommittedNamespace(this, Prefix, NamespaceURI))
            {
                string name = ((Prefix.Length > 0) ? "xmlns" + ":" + Prefix : "xmlns");
                XmlAttribute nsattrib = (XmlAttribute)OwnerDocument.CreateAttribute(name);
                nsattrib.Value = NamespaceURI;
                anc.TrackNamespaceNode(nsattrib, nsListToRender, nsLocallyDeclared);
            }

            if (IsInNodeSet)
            {
                anc.GetNamespacesToRender(this, attrListToRender, nsListToRender, nsLocallyDeclared);
                rgbData = utf8.GetBytes("<" + Name);
                hash.TransformBlock(rgbData, 0, rgbData.Length, rgbData, 0);
                foreach (object attr in nsListToRender.GetKeyList())
                {
                    (attr as CanonicalXmlAttribute).WriteHash(hash, docPos, anc);
                }
                foreach (object attr in attrListToRender.GetKeyList())
                {
                    (attr as CanonicalXmlAttribute).WriteHash(hash, docPos, anc);
                }
                rgbData = utf8.GetBytes(">");
                hash.TransformBlock(rgbData, 0, rgbData.Length, rgbData, 0);
            }

            anc.EnterElementContext();
            anc.LoadUnrenderedNamespaces(nsLocallyDeclared);
            anc.LoadRenderedNamespaces(nsListToRender);

            XmlNodeList childNodes = ChildNodes;
            foreach (XmlNode childNode in childNodes)
            {
                CanonicalizationDispatcher.WriteHash(childNode, hash, docPos, anc);
            }

            anc.ExitElementContext();

            if (IsInNodeSet)
            {
                rgbData = utf8.GetBytes("</" + Name + ">");
                hash.TransformBlock(rgbData, 0, rgbData.Length, rgbData, 0);
            }
        }
    }
}
