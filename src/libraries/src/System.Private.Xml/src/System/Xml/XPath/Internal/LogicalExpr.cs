// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal sealed class LogicalExpr : ValueQuery
    {
        private Operator.Op _op;
        private Query _opnd1;
        private Query _opnd2;

        public LogicalExpr(Operator.Op op, Query opnd1, Query opnd2)
        {
            Debug.Assert(
                Operator.Op.LT == op || Operator.Op.GT == op ||
                Operator.Op.LE == op || Operator.Op.GE == op ||
                Operator.Op.EQ == op || Operator.Op.NE == op
            );
            _op = op;
            _opnd1 = opnd1;
            _opnd2 = opnd2;
        }
        private LogicalExpr(LogicalExpr other) : base(other)
        {
            _op = other._op;
            _opnd1 = Clone(other._opnd1);
            _opnd2 = Clone(other._opnd2);
        }

        public override void SetXsltContext(XsltContext context)
        {
            _opnd1.SetXsltContext(context);
            _opnd2.SetXsltContext(context);
        }

        public override object Evaluate(XPathNodeIterator nodeIterator)
        {
            Operator.Op op = _op;
            object val1 = _opnd1.Evaluate(nodeIterator);
            object val2 = _opnd2.Evaluate(nodeIterator);
            int type1 = (int)GetXPathType(val1);
            int type2 = (int)GetXPathType(val2);
            if (type1 < type2)
            {
                op = Operator.InvertOperator(op);
                object valTemp = val1;
                val1 = val2;
                val2 = valTemp;
                int typeTmp = type1;
                type1 = type2;
                type2 = typeTmp;
            }

            if (op == Operator.Op.EQ || op == Operator.Op.NE)
            {
                return s_CompXsltE[type1][type2](op, val1, val2);
            }
            else
            {
                return s_CompXsltO[type1][type2](op, val1, val2);
            }
        }

        private delegate bool cmpXslt(Operator.Op op, object val1, object val2);

        //                              Number,                       String,                        Boolean,                     NodeSet,                      Navigator
        private static readonly cmpXslt[][] s_CompXsltE = {
            new cmpXslt[] { new cmpXslt(cmpNumberNumber), null                         , null                       , null                        , null                    },
            new cmpXslt[] { new cmpXslt(cmpStringNumber), new cmpXslt(cmpStringStringE), null                       , null                        , null                    },
            new cmpXslt[] { new cmpXslt(cmpBoolNumberE ), new cmpXslt(cmpBoolStringE  ), new cmpXslt(cmpBoolBoolE  ), null                        , null                    },
            new cmpXslt[] { new cmpXslt(cmpQueryNumber ), new cmpXslt(cmpQueryStringE ), new cmpXslt(cmpQueryBoolE ), new cmpXslt(cmpQueryQueryE ), null                    },
            new cmpXslt[] { new cmpXslt(cmpRtfNumber   ), new cmpXslt(cmpRtfStringE   ), new cmpXslt(cmpRtfBoolE   ), new cmpXslt(cmpRtfQueryE   ), new cmpXslt(cmpRtfRtfE) },
        };
        private static readonly cmpXslt[][] s_CompXsltO = {
            new cmpXslt[] { new cmpXslt(cmpNumberNumber), null                         , null                       , null                        , null                    },
            new cmpXslt[] { new cmpXslt(cmpStringNumber), new cmpXslt(cmpStringStringO), null                       , null                        , null                    },
            new cmpXslt[] { new cmpXslt(cmpBoolNumberO ), new cmpXslt(cmpBoolStringO  ), new cmpXslt(cmpBoolBoolO  ), null                        , null                    },
            new cmpXslt[] { new cmpXslt(cmpQueryNumber ), new cmpXslt(cmpQueryStringO ), new cmpXslt(cmpQueryBoolO ), new cmpXslt(cmpQueryQueryO ), null                    },
            new cmpXslt[] { new cmpXslt(cmpRtfNumber   ), new cmpXslt(cmpRtfStringO   ), new cmpXslt(cmpRtfBoolO   ), new cmpXslt(cmpRtfQueryO   ), new cmpXslt(cmpRtfRtfO) },
        };

        /*cmpXslt:*/
        private static bool cmpQueryQueryE(Operator.Op op, object val1, object val2)
        {
            Debug.Assert(op == Operator.Op.EQ || op == Operator.Op.NE);
            bool isEQ = (op == Operator.Op.EQ);

            NodeSet n1 = new NodeSet(val1);
            NodeSet n2 = new NodeSet(val2);

            while (true)
            {
                if (!n1.MoveNext())
                {
                    return false;
                }
                if (!n2.MoveNext())
                {
                    return false;
                }

                string str1 = n1.Value;

                do
                {
                    if ((str1 == n2.Value) == isEQ)
                    {
                        return true;
                    }
                } while (n2.MoveNext());
                n2.Reset();
            }
        }

        /*cmpXslt:*/
        private static bool cmpQueryQueryO(Operator.Op op, object val1, object val2)
        {
            Debug.Assert(
                op == Operator.Op.LT || op == Operator.Op.GT ||
                op == Operator.Op.LE || op == Operator.Op.GE
            );

            NodeSet n1 = new NodeSet(val1);
            NodeSet n2 = new NodeSet(val2);

            while (true)
            {
                if (!n1.MoveNext())
                {
                    return false;
                }
                if (!n2.MoveNext())
                {
                    return false;
                }

                double num1 = NumberFunctions.Number(n1.Value);

                do
                {
                    if (cmpNumberNumber(op, num1, NumberFunctions.Number(n2.Value)))
                    {
                        return true;
                    }
                } while (n2.MoveNext());
                n2.Reset();
            }
        }
        private static bool cmpQueryNumber(Operator.Op op, object val1, object val2)
        {
            NodeSet n1 = new NodeSet(val1);
            double n2 = (double)val2;

            while (n1.MoveNext())
            {
                if (cmpNumberNumber(op, NumberFunctions.Number(n1.Value), n2))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool cmpQueryStringE(Operator.Op op, object val1, object val2)
        {
            NodeSet n1 = new NodeSet(val1);
            string n2 = (string)val2;

            while (n1.MoveNext())
            {
                if (cmpStringStringE(op, n1.Value, n2))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool cmpQueryStringO(Operator.Op op, object val1, object val2)
        {
            NodeSet n1 = new NodeSet(val1);
            double n2 = NumberFunctions.Number((string)val2);

            while (n1.MoveNext())
            {
                if (cmpNumberNumberO(op, NumberFunctions.Number(n1.Value), n2))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool cmpRtfQueryE(Operator.Op op, object val1, object val2)
        {
            string n1 = Rtf(val1);
            NodeSet n2 = new NodeSet(val2);

            while (n2.MoveNext())
            {
                if (cmpStringStringE(op, n1, n2.Value))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool cmpRtfQueryO(Operator.Op op, object val1, object val2)
        {
            double n1 = NumberFunctions.Number(Rtf(val1));
            NodeSet n2 = new NodeSet(val2);

            while (n2.MoveNext())
            {
                if (cmpNumberNumberO(op, n1, NumberFunctions.Number(n2.Value)))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool cmpQueryBoolE(Operator.Op op, object val1, object val2)
        {
            NodeSet n1 = new NodeSet(val1);
            bool b1 = n1.MoveNext();
            bool b2 = (bool)val2;
            return cmpBoolBoolE(op, b1, b2);
        }

        private static bool cmpQueryBoolO(Operator.Op op, object val1, object val2)
        {
            NodeSet n1 = new NodeSet(val1);
            double d1 = n1.MoveNext() ? 1.0 : 0;
            double d2 = NumberFunctions.Number((bool)val2);
            return cmpNumberNumberO(op, d1, d2);
        }

        private static bool cmpBoolBoolE(Operator.Op op, bool n1, bool n2)
        {
            Debug.Assert(op == Operator.Op.EQ || op == Operator.Op.NE,
                "Unexpected Operator.op code in cmpBoolBoolE()"
            );
            return (op == Operator.Op.EQ) == (n1 == n2);
        }
        private static bool cmpBoolBoolE(Operator.Op op, object val1, object val2)
        {
            bool n1 = (bool)val1;
            bool n2 = (bool)val2;
            return cmpBoolBoolE(op, n1, n2);
        }

        private static bool cmpBoolBoolO(Operator.Op op, object val1, object val2)
        {
            double n1 = NumberFunctions.Number((bool)val1);
            double n2 = NumberFunctions.Number((bool)val2);
            return cmpNumberNumberO(op, n1, n2);
        }

        private static bool cmpBoolNumberE(Operator.Op op, object val1, object val2)
        {
            bool n1 = (bool)val1;
            bool n2 = BooleanFunctions.toBoolean((double)val2);
            return cmpBoolBoolE(op, n1, n2);
        }

        private static bool cmpBoolNumberO(Operator.Op op, object val1, object val2)
        {
            double n1 = NumberFunctions.Number((bool)val1);
            double n2 = (double)val2;
            return cmpNumberNumberO(op, n1, n2);
        }

        private static bool cmpBoolStringE(Operator.Op op, object val1, object val2)
        {
            bool n1 = (bool)val1;
            bool n2 = BooleanFunctions.toBoolean((string)val2);
            return cmpBoolBoolE(op, n1, n2);
        }

        private static bool cmpRtfBoolE(Operator.Op op, object val1, object val2)
        {
            bool n1 = BooleanFunctions.toBoolean(Rtf(val1));
            bool n2 = (bool)val2;
            return cmpBoolBoolE(op, n1, n2);
        }

        private static bool cmpBoolStringO(Operator.Op op, object val1, object val2)
        {
            return cmpNumberNumberO(op,
                NumberFunctions.Number((bool)val1),
                NumberFunctions.Number((string)val2)
            );
        }

        private static bool cmpRtfBoolO(Operator.Op op, object val1, object val2)
        {
            return cmpNumberNumberO(op,
                NumberFunctions.Number(Rtf(val1)),
                NumberFunctions.Number((bool)val2)
            );
        }

        private static bool cmpNumberNumber(Operator.Op op, double n1, double n2)
        {
            switch (op)
            {
                case Operator.Op.LT: return (n1 < n2);
                case Operator.Op.GT: return (n1 > n2);
                case Operator.Op.LE: return (n1 <= n2);
                case Operator.Op.GE: return (n1 >= n2);
                case Operator.Op.EQ: return (n1 == n2);
                case Operator.Op.NE: return (n1 != n2);
            }
            Debug.Fail("Unexpected Operator.op code in cmpNumberNumber()");
            return false;
        }
        private static bool cmpNumberNumberO(Operator.Op op, double n1, double n2)
        {
            switch (op)
            {
                case Operator.Op.LT: return (n1 < n2);
                case Operator.Op.GT: return (n1 > n2);
                case Operator.Op.LE: return (n1 <= n2);
                case Operator.Op.GE: return (n1 >= n2);
            }
            Debug.Fail("Unexpected Operator.op code in cmpNumberNumber()");
            return false;
        }
        private static bool cmpNumberNumber(Operator.Op op, object val1, object val2)
        {
            double n1 = (double)val1;
            double n2 = (double)val2;
            return cmpNumberNumber(op, n1, n2);
        }

        private static bool cmpStringNumber(Operator.Op op, object val1, object val2)
        {
            double n2 = (double)val2;
            double n1 = NumberFunctions.Number((string)val1);
            return cmpNumberNumber(op, n1, n2);
        }

        private static bool cmpRtfNumber(Operator.Op op, object val1, object val2)
        {
            double n2 = (double)val2;
            double n1 = NumberFunctions.Number(Rtf(val1));
            return cmpNumberNumber(op, n1, n2);
        }

        private static bool cmpStringStringE(Operator.Op op, string n1, string n2)
        {
            Debug.Assert(op == Operator.Op.EQ || op == Operator.Op.NE,
                "Unexpected Operator.op code in cmpStringStringE()"
            );
            return (op == Operator.Op.EQ) == (n1 == n2);
        }
        private static bool cmpStringStringE(Operator.Op op, object val1, object val2)
        {
            string n1 = (string)val1;
            string n2 = (string)val2;
            return cmpStringStringE(op, n1, n2);
        }
        private static bool cmpRtfStringE(Operator.Op op, object val1, object val2)
        {
            string n1 = Rtf(val1);
            string n2 = (string)val2;
            return cmpStringStringE(op, n1, n2);
        }
        private static bool cmpRtfRtfE(Operator.Op op, object val1, object val2)
        {
            string n1 = Rtf(val1);
            string n2 = Rtf(val2);
            return cmpStringStringE(op, n1, n2);
        }

        private static bool cmpStringStringO(Operator.Op op, object val1, object val2)
        {
            double n1 = NumberFunctions.Number((string)val1);
            double n2 = NumberFunctions.Number((string)val2);
            return cmpNumberNumberO(op, n1, n2);
        }

        private static bool cmpRtfStringO(Operator.Op op, object val1, object val2)
        {
            double n1 = NumberFunctions.Number(Rtf(val1));
            double n2 = NumberFunctions.Number((string)val2);
            return cmpNumberNumberO(op, n1, n2);
        }

        private static bool cmpRtfRtfO(Operator.Op op, object val1, object val2)
        {
            double n1 = NumberFunctions.Number(Rtf(val1));
            double n2 = NumberFunctions.Number(Rtf(val2));
            return cmpNumberNumberO(op, n1, n2);
        }

        public override XPathNodeIterator Clone() { return new LogicalExpr(this); }

        private struct NodeSet
        {
            private Query _opnd;
            private XPathNavigator _current;

            public NodeSet(object opnd)
            {
                _opnd = (Query)opnd;
                _current = null;
            }
            public bool MoveNext()
            {
                _current = _opnd.Advance();
                return _current != null;
            }

            public void Reset()
            {
                _opnd.Reset();
            }

            public string Value { get { return _current.Value; } }
        }

        private static string Rtf(object o) { return ((XPathNavigator)o).Value; }

        public override XPathResultType StaticType { get { return XPathResultType.Boolean; } }
    }
}
