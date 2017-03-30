// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file contains the classes necessary to represent the Transform processing model used in 
// XMLDSIG. The basic idea is as follows. A Reference object contains within it a TransformChain, which
// is an ordered set of XMLDSIG transforms (represented by <Transform>...</Transform> clauses in the XML).
// A transform in XMLDSIG operates on an input of either an octet stream or a node set and produces
// either an octet stream or a node set. Conversion between the two types is performed by parsing (octet stream->
// node set) or C14N (node set->octet stream). We generalize this slightly to allow a transform to define an array of
// input and output types (because I believe in the future there will be perf gains by being smarter about what goes in & comes out)
// Each XMLDSIG transform is represented by a subclass of the abstract Transform class. We need to use CryptoConfig to
// associate Transform classes with URLs for transform extensibility, but that's a future concern for this code.
// Once the Transform chain is constructed, call TransformToOctetStream to convert some sort of input type to an octet
// stream. (We only bother implementing that much now since every use of transform chains in XmlDsig ultimately yields something to hash).

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
    public abstract class Transform
    {
        private string _algorithm;
        private string _baseUri = null;
        internal XmlResolver _xmlResolver = null;
        private bool _bResolverSet = false;
        private SignedXml _signedXml = null;
        private Reference _reference = null;
        private Hashtable _propagatedNamespaces = null;
        private XmlElement _context = null;

        internal string BaseURI
        {
            get { return _baseUri; }
            set { _baseUri = value; }
        }

        internal SignedXml SignedXml
        {
            get { return _signedXml; }
            set { _signedXml = value; }
        }

        internal Reference Reference
        {
            get { return _reference; }
            set { _reference = value; }
        }

        //
        // protected constructors
        //

        protected Transform() { }

        //
        // public properties
        //

        public string Algorithm
        {
            get { return _algorithm; }
            set { _algorithm = value; }
        }

        public XmlResolver Resolver
        {
            // This property only has a setter. The rationale for this is that we don't have a good value
            // to return when it has not been explicitely set, as we are using XmlSecureResolver by default
            set
            {
                _xmlResolver = value;
                _bResolverSet = true;
            }

            internal get
            {
                return _xmlResolver;
            }
        }

        internal bool ResolverSet
        {
            get { return _bResolverSet; }
        }

        public abstract Type[] InputTypes
        {
            get;
        }

        public abstract Type[] OutputTypes
        {
            get;
        }

        internal bool AcceptsType(Type inputType)
        {
            if (InputTypes != null)
            {
                for (int i = 0; i < InputTypes.Length; i++)
                {
                    if (inputType == InputTypes[i] || inputType.IsSubclassOf(InputTypes[i]))
                        return true;
                }
            }
            return false;
        }

        //
        // public methods
        //

        public XmlElement GetXml()
        {
            XmlDocument document = new XmlDocument();
            document.PreserveWhitespace = true;
            return GetXml(document);
        }

        internal XmlElement GetXml(XmlDocument document)
        {
            return GetXml(document, "Transform");
        }

        internal XmlElement GetXml(XmlDocument document, string name)
        {
            XmlElement transformElement = document.CreateElement(name, SignedXml.XmlDsigNamespaceUrl);
            if (!string.IsNullOrEmpty(Algorithm))
                transformElement.SetAttribute("Algorithm", Algorithm);
            XmlNodeList children = GetInnerXml();
            if (children != null)
            {
                foreach (XmlNode node in children)
                {
                    transformElement.AppendChild(document.ImportNode(node, true));
                }
            }
            return transformElement;
        }

        public abstract void LoadInnerXml(XmlNodeList nodeList);

        protected abstract XmlNodeList GetInnerXml();

        public abstract void LoadInput(object obj);

        public abstract object GetOutput();

        public abstract object GetOutput(Type type);

        public virtual byte[] GetDigestedOutput(HashAlgorithm hash)
        {
            return hash.ComputeHash((Stream)GetOutput(typeof(Stream)));
        }

        public XmlElement Context
        {
            get
            {
                if (_context != null)
                    return _context;

                Reference reference = Reference;
                SignedXml signedXml = (reference == null ? SignedXml : reference.SignedXml);
                if (signedXml == null)
                    return null;

                return signedXml._context;
            }
            set
            {
                _context = value;
            }
        }

        public Hashtable PropagatedNamespaces
        {
            get
            {
                if (_propagatedNamespaces != null)
                    return _propagatedNamespaces;

                Reference reference = Reference;
                SignedXml signedXml = (reference == null ? SignedXml : reference.SignedXml);

                // If the reference is not a Uri reference with a DataObject target, return an empty hashtable.
                if (reference != null &&
                    ((reference.ReferenceTargetType != ReferenceTargetType.UriReference) ||
                     (string.IsNullOrEmpty(reference.Uri) || reference.Uri[0] != '#')))
                {
                    _propagatedNamespaces = new Hashtable(0);
                    return _propagatedNamespaces;
                }

                CanonicalXmlNodeList namespaces = null;
                if (reference != null)
                    namespaces = reference._namespaces;
                else if (signedXml?._context != null)
                    namespaces = Utils.GetPropagatedAttributes(signedXml._context);

                // if no namespaces have been propagated, return an empty hashtable.
                if (namespaces == null)
                {
                    _propagatedNamespaces = new Hashtable(0);
                    return _propagatedNamespaces;
                }

                _propagatedNamespaces = new Hashtable(namespaces.Count);
                foreach (XmlNode attrib in namespaces)
                {
                    string key = ((attrib.Prefix.Length > 0) ? attrib.Prefix + ":" + attrib.LocalName : attrib.LocalName);
                    if (!_propagatedNamespaces.Contains(key))
                        _propagatedNamespaces.Add(key, attrib.Value);
                }
                return _propagatedNamespaces;
            }
        }
    }
}
