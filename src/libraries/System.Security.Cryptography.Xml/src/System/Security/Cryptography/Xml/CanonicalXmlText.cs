// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    // the class that provides node subset state and canonicalization function to XmlText
    internal sealed class CanonicalXmlText : XmlText, ICanonicalizableNode
    {
        private bool _isInNodeSet;

        public CanonicalXmlText(string? strData, XmlDocument doc, bool defaultNodeSetInclusionState)
            : base(strData, doc)
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
            if (IsInNodeSet)
                strBuilder.Append(Utils.EscapeTextData(Value));
        }

        public void WriteHash(HashAlgorithm hash, DocPosition docPos, AncestralNamespaceContextManager anc)
        {
            if (IsInNodeSet)
            {
                byte[] rgbData = Encoding.UTF8.GetBytes(Utils.EscapeTextData(Value));
                hash.TransformBlock(rgbData, 0, rgbData.Length, rgbData, 0);
            }
        }
    }
}
