// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal sealed class NumericExpr : ValueQuery
    {
        private Operator.Op op;
        private Query opnd1;
        private Query opnd2;

        public NumericExpr(Operator.Op op, Query opnd1, Query opnd2)
        {
            Debug.Assert(
                op == Operator.Op.PLUS || op == Operator.Op.MINUS ||
                op == Operator.Op.MUL || op == Operator.Op.DIV ||
                op == Operator.Op.MOD
            );
            Debug.Assert(opnd1 != null && opnd2 != null);
            if (opnd1.StaticType != XPathResultType.Number)
            {
                opnd1 = new NumberFunctions(Function.FunctionType.FuncNumber, opnd1);
            }
            if (opnd2.StaticType != XPathResultType.Number)
            {
                opnd2 = new NumberFunctions(Function.FunctionType.FuncNumber, opnd2);
            }
            this.op = op;
            this.opnd1 = opnd1;
            this.opnd2 = opnd2;
        }
        private NumericExpr(NumericExpr other) : base(other)
        {
            this.op = other.op;
            this.opnd1 = Clone(other.opnd1);
            this.opnd2 = Clone(other.opnd2);
        }

        public override void SetXsltContext(XsltContext context)
        {
            opnd1.SetXsltContext(context);
            opnd2.SetXsltContext(context);
        }

        public override object Evaluate(XPathNodeIterator nodeIterator)
        {
            return GetValue(this.op,
                XmlConvertEx.ToXPathDouble(opnd1.Evaluate(nodeIterator)),
                XmlConvertEx.ToXPathDouble(opnd2.Evaluate(nodeIterator))
            );
        }
        private static double GetValue(Operator.Op op, double n1, double n2)
        {
            Debug.Assert(op == Operator.Op.PLUS || op == Operator.Op.MINUS || op == Operator.Op.MOD || op == Operator.Op.DIV || op == Operator.Op.MUL);
            switch (op)
            {
                case Operator.Op.PLUS: return n1 + n2;
                case Operator.Op.MINUS: return n1 - n2;
                case Operator.Op.MOD: return n1 % n2;
                case Operator.Op.DIV: return n1 / n2;
                case Operator.Op.MUL: return n1 * n2;
            }
            return 0;
        }

        public override XPathResultType StaticType { get { return XPathResultType.Number; } }

        public override XPathNodeIterator Clone() { return new NumericExpr(this); }

        public override void PrintQuery(XmlWriter w)
        {
            w.WriteStartElement(this.GetType().Name);
            w.WriteAttributeString("op", op.ToString());
            opnd1.PrintQuery(w);
            opnd2.PrintQuery(w);
            w.WriteEndElement();
        }
    }
}
