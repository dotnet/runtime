// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class Root : AstNode
    {
        public Root() { }

        public override AstType Type { get { return AstType.Root; } }
        public override XPathResultType ReturnType { get { return XPathResultType.NodeSet; } }
    }
}
