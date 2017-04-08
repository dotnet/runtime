// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal sealed class BooleanExpr : ValueQuery
    {
        private Query _opnd1;
        private Query _opnd2;
        private bool _isOr;

        public BooleanExpr(Operator.Op op, Query opnd1, Query opnd2)
        {
            Debug.Assert(op == Operator.Op.AND || op == Operator.Op.OR);
            Debug.Assert(opnd1 != null && opnd2 != null);
            if (opnd1.StaticType != XPathResultType.Boolean)
            {
                opnd1 = new BooleanFunctions(Function.FunctionType.FuncBoolean, opnd1);
            }
            if (opnd2.StaticType != XPathResultType.Boolean)
            {
                opnd2 = new BooleanFunctions(Function.FunctionType.FuncBoolean, opnd2);
            }
            _opnd1 = opnd1;
            _opnd2 = opnd2;
            _isOr = (op == Operator.Op.OR);
        }
        private BooleanExpr(BooleanExpr other) : base(other)
        {
            _opnd1 = Clone(other._opnd1);
            _opnd2 = Clone(other._opnd2);
            _isOr = other._isOr;
        }

        public override void SetXsltContext(XsltContext context)
        {
            _opnd1.SetXsltContext(context);
            _opnd2.SetXsltContext(context);
        }

        public override object Evaluate(XPathNodeIterator nodeIterator)
        {
            object n1 = _opnd1.Evaluate(nodeIterator);
            if (((bool)n1) == _isOr)
            {
                return n1;
            }
            return _opnd2.Evaluate(nodeIterator);
        }

        public override XPathNodeIterator Clone() { return new BooleanExpr(this); }
        public override XPathResultType StaticType { get { return XPathResultType.Boolean; } }
    }
}
