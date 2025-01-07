// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    // the class that provides node subset state and canonicalization function to XmlAttribute
    internal sealed class CanonicalXmlAttribute : XmlAttribute, ICanonicalizableNode
    {
        private bool _isInNodeSet;

        public CanonicalXmlAttribute(string? prefix, string localName, string? namespaceURI, XmlDocument doc, bool defaultNodeSetInclusionState)
            : base(prefix, localName, namespaceURI, doc)
        {
            IsInNodeSet = defaultNodeSetInclusionState;
        }

        public bool IsInNodeSet
        {
            get { return _isInNodeSet; }
            set { _isInNodeSet = value; }
        }

        public void Write(StringBuilder strBuilder, DocPosition docPos, AncestralNamespaceContextManager anc)
        {
            strBuilder.Append($" {Name}=\"");
            strBuilder.Append(Utils.EscapeAttributeValue(Value));
            strBuilder.Append('"');
        }

        public void WriteHash(HashAlgorithm hash, DocPosition docPos, AncestralNamespaceContextManager anc)
        {
            byte[] rgbData = Encoding.UTF8.GetBytes(" " + Name + "=\"");
            hash.TransformBlock(rgbData, 0, rgbData.Length, rgbData, 0);
            rgbData = Encoding.UTF8.GetBytes(Utils.EscapeAttributeValue(Value));
            hash.TransformBlock(rgbData, 0, rgbData.Length, rgbData, 0);

            rgbData = "\""u8.ToArray();
            hash.TransformBlock(rgbData, 0, rgbData.Length, rgbData, 0);
        }
    }
}
