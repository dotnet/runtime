// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class Group : AstNode
    {
        private AstNode groupNode;

        public Group(AstNode groupNode)
        {
            this.groupNode = groupNode;
        }
        public override AstType Type { get { return AstType.Group; } }
        public override XPathResultType ReturnType { get { return XPathResultType.NodeSet; } }

        public AstNode GroupNode { get { return groupNode; } }
    }
}


