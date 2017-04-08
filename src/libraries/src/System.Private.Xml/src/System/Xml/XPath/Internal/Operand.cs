// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
