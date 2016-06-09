// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal.Xml.XPath
{
    using System;
    using System.Xml;
    using System.Xml.XPath;
    using System.Diagnostics;
    using System.Globalization;
    internal class Group : AstNode
    {
        private AstNode _groupNode;

        public Group(AstNode groupNode)
        {
            _groupNode = groupNode;
        }
        public override AstType Type { get { return AstType.Group; } }
        public override XPathResultType ReturnType { get { return XPathResultType.NodeSet; } }

        public AstNode GroupNode { get { return _groupNode; } }
    }
}


