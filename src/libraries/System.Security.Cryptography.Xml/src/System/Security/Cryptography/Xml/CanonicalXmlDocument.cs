// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    // all input types eventually lead to the creation of an XmlDocument document
    // of this type. it maintains the node subset state and performs output rendering during canonicalization
    internal sealed class CanonicalXmlDocument : XmlDocument, ICanonicalizableNode
    {
        private readonly bool _defaultNodeSetInclusionState;
        private readonly bool _includeComments;
        private bool _isInNodeSet;

        public CanonicalXmlDocument(bool defaultNodeSetInclusionState, bool includeComments) : base()
        {
            PreserveWhitespace = true;
            _includeComments = includeComments;
            _isInNodeSet = _defaultNodeSetInclusionState = defaultNodeSetInclusionState;
        }

        public bool IsInNodeSet
        {
            get { return _isInNodeSet; }
            set { _isInNodeSet = value; }
        }

        public void Write(StringBuilder strBuilder, DocPosition docPos, AncestralNamespaceContextManager anc)
        {
            docPos = DocPosition.BeforeRootElement;
            foreach (XmlNode childNode in ChildNodes)
            {
                if (childNode.NodeType == XmlNodeType.Element)
                {
                    CanonicalizationDispatcher.Write(childNode, strBuilder, DocPosition.InRootElement, anc);
                    docPos = DocPosition.AfterRootElement;
                }
                else
                {
                    CanonicalizationDispatcher.Write(childNode, strBuilder, docPos, anc);
                }
            }
        }

        public void WriteHash(HashAlgorithm hash, DocPosition docPos, AncestralNamespaceContextManager anc)
        {
            docPos = DocPosition.BeforeRootElement;
            foreach (XmlNode childNode in ChildNodes)
            {
                if (childNode.NodeType == XmlNodeType.Element)
                {
                    CanonicalizationDispatcher.WriteHash(childNode, hash, DocPosition.InRootElement, anc);
                    docPos = DocPosition.AfterRootElement;
                }
                else
                {
                    CanonicalizationDispatcher.WriteHash(childNode, hash, docPos, anc);
                }
            }
        }

        public override XmlElement CreateElement(string? prefix, string localName, string? namespaceURI)
        {
            return new CanonicalXmlElement(prefix, localName, namespaceURI, this, _defaultNodeSetInclusionState);
        }

        public override XmlAttribute CreateAttribute(string? prefix, string localName, string? namespaceURI)
        {
            return new CanonicalXmlAttribute(prefix, localName, namespaceURI, this, _defaultNodeSetInclusionState);
        }

        protected override XmlAttribute CreateDefaultAttribute(string? prefix, string localName, string? namespaceURI)
        {
            return new CanonicalXmlAttribute(prefix, localName, namespaceURI, this, _defaultNodeSetInclusionState);
        }

        public override XmlText CreateTextNode(string? text)
        {
            return new CanonicalXmlText(text, this, _defaultNodeSetInclusionState);
        }

        public override XmlWhitespace CreateWhitespace(string? prefix)
        {
            return new CanonicalXmlWhitespace(prefix, this, _defaultNodeSetInclusionState);
        }

        public override XmlSignificantWhitespace CreateSignificantWhitespace(string? text)
        {
            return new CanonicalXmlSignificantWhitespace(text, this, _defaultNodeSetInclusionState);
        }

        public override XmlProcessingInstruction CreateProcessingInstruction(string target, string? data)
        {
            return new CanonicalXmlProcessingInstruction(target, data!, this, _defaultNodeSetInclusionState);
        }

        public override XmlComment CreateComment(string? data)
        {
            return new CanonicalXmlComment(data, this, _defaultNodeSetInclusionState, _includeComments);
        }

        public override XmlEntityReference CreateEntityReference(string name)
        {
            return new CanonicalXmlEntityReference(name, this, _defaultNodeSetInclusionState);
        }

        public override XmlCDataSection CreateCDataSection(string? data)
        {
            return new CanonicalXmlCDataSection(data, this, _defaultNodeSetInclusionState);
        }
    }
}
