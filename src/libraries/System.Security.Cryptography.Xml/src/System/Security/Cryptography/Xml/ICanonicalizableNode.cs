// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Security.Cryptography.Xml
{
    // the interface to be implemented by all subclasses of XmlNode
    // that have to provide node subsetting and canonicalization features.
    internal interface ICanonicalizableNode
    {
        bool IsInNodeSet
        {
            get;
            set;
        }

        void Write(StringBuilder strBuilder, DocPosition docPos, AncestralNamespaceContextManager anc);
        void WriteHash(HashAlgorithm hash, DocPosition docPos, AncestralNamespaceContextManager anc);
    }
}
