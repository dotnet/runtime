// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal sealed class BooleanExpr : ValueQuery
    {
        private Query opnd1;
        private Query opnd2;
        private bool isOr;

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
            this.opnd1 = opnd1;
            this.opnd2 = opnd2;
            isOr = (op == Operator.Op.OR);
        }
        private BooleanExpr(BooleanExpr other) : base(other)
        {
            this.opnd1 = Clone(other.opnd1);
            this.opnd2 = Clone(other.opnd2);
            this.isOr = other.isOr;
        }

        public override void SetXsltContext(XsltContext context)
        {
            opnd1.SetXsltContext(context);
            opnd2.SetXsltContext(context);
        }

        public override object Evaluate(XPathNodeIterator nodeIterator)
        {
            object n1 = opnd1.Evaluate(nodeIterator);
            if (((bool)n1) == isOr)
            {
                return n1;
            }
            return opnd2.Evaluate(nodeIterator);
        }

        public override XPathNodeIterator Clone() { return new BooleanExpr(this); }
        public override XPathResultType StaticType { get { return XPathResultType.Boolean; } }

        public override void PrintQuery(XmlWriter w)
        {
            w.WriteStartElement(this.GetType().Name);
            w.WriteAttributeString("op", (isOr ? Operator.Op.OR : Operator.Op.AND).ToString());
            opnd1.PrintQuery(w);
            opnd2.PrintQuery(w);
            w.WriteEndElement();
        }
    }
}
