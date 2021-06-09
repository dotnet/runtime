// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Text;

namespace System.Security.Cryptography.Xml
{
    // the class that provides node subset state and canonicalization function to XmlProcessingInstruction
    internal sealed class CanonicalXmlProcessingInstruction : XmlProcessingInstruction, ICanonicalizableNode
    {
        private bool _isInNodeSet;

        public CanonicalXmlProcessingInstruction(string target, string data, XmlDocument doc, bool defaultNodeSetInclusionState)
            : base(target, data, doc)
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
            if (!IsInNodeSet)
                return;

            if (docPos == DocPosition.AfterRootElement)
                strBuilder.Append((char)10);
            strBuilder.Append("<?");
            strBuilder.Append(Name);
            if ((Value != null) && (Value.Length > 0))
                strBuilder.Append(" " + Value);
            strBuilder.Append("?>");
            if (docPos == DocPosition.BeforeRootElement)
                strBuilder.Append((char)10);
        }

        public void WriteHash(HashAlgorithm hash, DocPosition docPos, AncestralNamespaceContextManager anc)
        {
            if (!IsInNodeSet)
                return;

            UTF8Encoding utf8 = new UTF8Encoding(false);
            byte[] rgbData;
            if (docPos == DocPosition.AfterRootElement)
            {
                rgbData = utf8.GetBytes("(char) 10");
                hash.TransformBlock(rgbData, 0, rgbData.Length, rgbData, 0);
            }
            rgbData = utf8.GetBytes("<?");
            hash.TransformBlock(rgbData, 0, rgbData.Length, rgbData, 0);
            rgbData = utf8.GetBytes((Name));
            hash.TransformBlock(rgbData, 0, rgbData.Length, rgbData, 0);
            if ((Value != null) && (Value.Length > 0))
            {
                rgbData = utf8.GetBytes(" " + Value);
                hash.TransformBlock(rgbData, 0, rgbData.Length, rgbData, 0);
            }
            rgbData = utf8.GetBytes("?>");
            hash.TransformBlock(rgbData, 0, rgbData.Length, rgbData, 0);
            if (docPos == DocPosition.BeforeRootElement)
            {
                rgbData = utf8.GetBytes("(char) 10");
                hash.TransformBlock(rgbData, 0, rgbData.Length, rgbData, 0);
            }
        }
    }
}
