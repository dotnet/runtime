// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal sealed class Group : AstNode
    {
        private readonly AstNode _groupNode;

        public Group(AstNode groupNode)
        {
            _groupNode = groupNode;
        }
        public override AstType Type { get { return AstType.Group; } }
        public override XPathResultType ReturnType { get { return XPathResultType.NodeSet; } }

        public AstNode GroupNode { get { return _groupNode; } }
    }
}
