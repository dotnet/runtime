// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class Filter : AstNode
    {
        private AstNode _input;
        private AstNode _condition;

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
