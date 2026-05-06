// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    // the central dispatcher for canonicalization writes. not all node classes
    // implement ICanonicalizableNode; so a manual dispatch is sometimes necessary.
    internal static class CanonicalizationDispatcher
    {
        [ThreadStatic]
        private static int t_depth;

        public static void Write(XmlNode node, StringBuilder strBuilder, DocPosition docPos, AncestralNamespaceContextManager anc)
        {
            int maxDepth = LocalAppContextSwitches.DangerousMaxRecursionDepth;
            if (maxDepth > 0 && t_depth > maxDepth)
            {
                throw new CryptographicException(SR.Cryptography_Xml_MaxDepthExceeded);
            }

            t_depth++;
            try
            {
                if (node is ICanonicalizableNode canonicalizableNode)
                {
                    canonicalizableNode.Write(strBuilder, docPos, anc);
                }
                else
                {
                    WriteGenericNode(node, strBuilder, docPos, anc);
                }
            }
            finally
            {
                t_depth--;
            }
        }

        public static void WriteGenericNode(XmlNode node, StringBuilder strBuilder, DocPosition docPos, AncestralNamespaceContextManager anc)
        {
            ArgumentNullException.ThrowIfNull(node);

            XmlNodeList childNodes = node.ChildNodes;
            foreach (XmlNode childNode in childNodes)
            {
                Write(childNode, strBuilder, docPos, anc);
            }
        }

        public static void WriteHash(XmlNode node, HashAlgorithm hash, DocPosition docPos, AncestralNamespaceContextManager anc)
        {
            int maxDepth = LocalAppContextSwitches.DangerousMaxRecursionDepth;
            if (maxDepth > 0 && t_depth > maxDepth)
            {
                throw new CryptographicException(SR.Cryptography_Xml_MaxDepthExceeded);
            }

            t_depth++;
            try
            {
                if (node is ICanonicalizableNode canonicalizableNode)
                {
                    canonicalizableNode.WriteHash(hash, docPos, anc);
                }
                else
                {
                    WriteHashGenericNode(node, hash, docPos, anc);
                }
            }
            finally
            {
                t_depth--;
            }
        }

        public static void WriteHashGenericNode(XmlNode node, HashAlgorithm hash, DocPosition docPos, AncestralNamespaceContextManager anc)
        {
            ArgumentNullException.ThrowIfNull(node);

            XmlNodeList childNodes = node.ChildNodes;
            foreach (XmlNode childNode in childNodes)
            {
                WriteHash(childNode, hash, docPos, anc);
            }
        }
    }
}
