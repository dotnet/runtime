// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace System.Security.Cryptography.Xml
{
    public class XmlDsigExcC14NTransform : Transform
    {
        private Type[] _inputTypes = { typeof(Stream), typeof(XmlDocument), typeof(XmlNodeList) };
        private Type[] _outputTypes = { typeof(Stream) };
        private bool _includeComments = false;
        private string _inclusiveNamespacesPrefixList;
        private ExcCanonicalXml _excCanonicalXml;

        public XmlDsigExcC14NTransform() : this(false, null) { }

        public XmlDsigExcC14NTransform(bool includeComments) : this(includeComments, null) { }

        public XmlDsigExcC14NTransform(string inclusiveNamespacesPrefixList) : this(false, inclusiveNamespacesPrefixList) { }

        public XmlDsigExcC14NTransform(bool includeComments, string inclusiveNamespacesPrefixList)
        {
            _includeComments = includeComments;
            _inclusiveNamespacesPrefixList = inclusiveNamespacesPrefixList;
            Algorithm = (includeComments ? SignedXml.XmlDsigExcC14NWithCommentsTransformUrl : SignedXml.XmlDsigExcC14NTransformUrl);
        }

        public string InclusiveNamespacesPrefixList
        {
            get { return _inclusiveNamespacesPrefixList; }
            set { _inclusiveNamespacesPrefixList = value; }
        }

        public override Type[] InputTypes
        {
            get { return _inputTypes; }
        }

        public override Type[] OutputTypes
        {
            get { return _outputTypes; }
        }

        public override void LoadInnerXml(XmlNodeList nodeList)
        {
            if (nodeList != null)
            {
                foreach (XmlNode n in nodeList)
                {
                    XmlElement e = n as XmlElement;
                    if (e != null)
                    {
                        if (e.LocalName.Equals("InclusiveNamespaces")
                        && e.NamespaceURI.Equals(SignedXml.XmlDsigExcC14NTransformUrl) &&
                        Utils.HasAttribute(e, "PrefixList", SignedXml.XmlDsigNamespaceUrl))
                        {
                            if (!Utils.VerifyAttributes(e, "PrefixList"))
                            {
                                throw new CryptographicException(SR.Cryptography_Xml_UnknownTransform);
                            }
                            this.InclusiveNamespacesPrefixList = Utils.GetAttribute(e, "PrefixList", SignedXml.XmlDsigNamespaceUrl);
                            return;
                        }
                        else
                        {
                            throw new CryptographicException(SR.Cryptography_Xml_UnknownTransform);
                        }
                    }
                }
            }
        }

        public override void LoadInput(object obj)
        {
            XmlResolver resolver = (ResolverSet ? _xmlResolver : new XmlSecureResolver(new XmlUrlResolver(), BaseURI));
            if (obj is Stream)
            {
                _excCanonicalXml = new ExcCanonicalXml((Stream)obj, _includeComments, _inclusiveNamespacesPrefixList, resolver, BaseURI);
            }
            else if (obj is XmlDocument)
            {
                _excCanonicalXml = new ExcCanonicalXml((XmlDocument)obj, _includeComments, _inclusiveNamespacesPrefixList, resolver);
            }
            else if (obj is XmlNodeList)
            {
                _excCanonicalXml = new ExcCanonicalXml((XmlNodeList)obj, _includeComments, _inclusiveNamespacesPrefixList, resolver);
            }
            else
                throw new ArgumentException(SR.Cryptography_Xml_IncorrectObjectType, nameof(obj));
        }

        protected override XmlNodeList GetInnerXml()
        {
            if (InclusiveNamespacesPrefixList == null)
                return null;
            XmlDocument document = new XmlDocument();
            XmlElement element = document.CreateElement("Transform", SignedXml.XmlDsigNamespaceUrl);
            if (!string.IsNullOrEmpty(Algorithm))
                element.SetAttribute("Algorithm", Algorithm);
            XmlElement prefixListElement = document.CreateElement("InclusiveNamespaces", SignedXml.XmlDsigExcC14NTransformUrl);
            prefixListElement.SetAttribute("PrefixList", InclusiveNamespacesPrefixList);
            element.AppendChild(prefixListElement);
            return element.ChildNodes;
        }

        public override object GetOutput()
        {
            return new MemoryStream(_excCanonicalXml.GetBytes());
        }

        public override object GetOutput(Type type)
        {
            if (type != typeof(Stream) && !type.IsSubclassOf(typeof(Stream)))
                throw new ArgumentException(SR.Cryptography_Xml_TransformIncorrectInputType, nameof(type));
            return new MemoryStream(_excCanonicalXml.GetBytes());
        }

        public override byte[] GetDigestedOutput(HashAlgorithm hash)
        {
            return _excCanonicalXml.GetDigestedBytes(hash);
        }
    }
}
