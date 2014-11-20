// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class Operand : AstNode
    {
        private XPathResultType type;
        private object val;

        public Operand(string val)
        {
            this.type = XPathResultType.String;
            this.val = val;
        }

        public Operand(double val)
        {
            this.type = XPathResultType.Number;
            this.val = val;
        }

        public override AstType Type { get { return AstType.ConstantOperand; } }
        public override XPathResultType ReturnType { get { return type; } }

        public object OperandValue { get { return val; } }
    }
}
