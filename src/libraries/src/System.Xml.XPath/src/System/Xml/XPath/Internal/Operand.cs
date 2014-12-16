// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class Operand : AstNode
    {
        private XPathResultType _type;
        private object _val;

        public Operand(string val)
        {
            _type = XPathResultType.String;
            _val = val;
        }

        public Operand(double val)
        {
            _type = XPathResultType.Number;
            _val = val;
        }

        public override AstType Type { get { return AstType.ConstantOperand; } }
        public override XPathResultType ReturnType { get { return _type; } }

        public object OperandValue { get { return _val; } }
    }
}
