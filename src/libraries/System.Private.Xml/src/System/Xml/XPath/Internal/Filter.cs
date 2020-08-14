// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class Filter : AstNode
    {
        private readonly AstNode _input;
        private readonly AstNode _condition;

        public Filter(AstNode input, AstNode condition)
        {
            _input = input;
            _condition = condition;
        }

        public override AstType Type { get { return AstType.Filter; } }
        public override XPathResultType ReturnType { get { return XPathResultType.NodeSet; } }

        public AstNode Input { get { return _input; } }
        public AstNode Condition { get { return _condition; } }
    }
}
