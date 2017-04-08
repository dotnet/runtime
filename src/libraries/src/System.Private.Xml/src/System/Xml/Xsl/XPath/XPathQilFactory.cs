// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Xml.Schema;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.XPath
{
    using T = XmlQueryTypeFactory;

    internal class XPathQilFactory : QilPatternFactory
    {
        public XPathQilFactory(QilFactory f, bool debug) : base(f, debug)
        {
        }

        // Helper methods used in addition to QilPatternFactory's ones

        public QilNode Error(string res, QilNode args)
        {
            return Error(InvokeFormatMessage(String(res), args));
        }

        public QilNode Error(ISourceLineInfo lineInfo, string res, params string[] args)
        {
            return Error(String(XslLoadException.CreateMessage(lineInfo, res, args)));
        }

        public QilIterator FirstNode(QilNode n)
        {
            CheckNodeSet(n);
            QilIterator i = For(DocOrderDistinct(n));
            return For(Filter(i, Eq(PositionOf(i), Int32(1))));
        }

        public bool IsAnyType(QilNode n)
        {
            XmlQueryType xt = n.XmlType;
            bool result = !(xt.IsStrict || xt.IsNode);
            Debug.Assert(result == (xt.TypeCode == XmlTypeCode.Item || xt.TypeCode == XmlTypeCode.AnyAtomicType), "What else can it be?");
            return result;
        }

        [Conditional("DEBUG")]
        public void CheckNode(QilNode n)
        {
            Debug.Assert(n != null && n.XmlType.IsSingleton && n.XmlType.IsNode, "Must be a singleton node");
        }

        [Conditional("DEBUG")]
        public void CheckNodeSet(QilNode n)
        {
            Debug.Assert(n != null && n.XmlType.IsNode, "Must be a node-set");
        }

        [Conditional("DEBUG")]
        public void CheckNodeNotRtf(QilNode n)
        {
            Debug.Assert(n != null && n.XmlType.IsSingleton && n.XmlType.IsNode && n.XmlType.IsNotRtf, "Must be a singleton node and not an Rtf");
        }

        [Conditional("DEBUG")]
        public void CheckString(QilNode n)
        {
            Debug.Assert(n != null && n.XmlType.IsSubtypeOf(T.StringX), "Must be a singleton string");
        }

        [Conditional("DEBUG")]
        public void CheckStringS(QilNode n)
        {
            Debug.Assert(n != null && n.XmlType.IsSubtypeOf(T.StringXS), "Must be a sequence of strings");
        }

        [Conditional("DEBUG")]
        public void CheckDouble(QilNode n)
        {
            Debug.Assert(n != null && n.XmlType.IsSubtypeOf(T.DoubleX), "Must be a singleton Double");
        }

        [Conditional("DEBUG")]
        public void CheckBool(QilNode n)
        {
            Debug.Assert(n != null && n.XmlType.IsSubtypeOf(T.BooleanX), "Must be a singleton Bool");
        }

        // Return true if inferred type of the given expression is never a subtype of T.NodeS
        public bool CannotBeNodeSet(QilNode n)
        {
            XmlQueryType xt = n.XmlType;
            // Do not report compile error if n is a VarPar, whose inferred type forbids nodes (SQLBUDT 339398)
            return xt.IsAtomicValue && !xt.IsEmpty && !(n is QilIterator);
        }

        public QilNode SafeDocOrderDistinct(QilNode n)
        {
            XmlQueryType xt = n.XmlType;

            if (xt.MaybeMany)
            {
                if (xt.IsNode && xt.IsNotRtf)
                {
                    // node-set
                    return DocOrderDistinct(n);
                }
                else if (!xt.IsAtomicValue)
                {
                    QilIterator i;
                    return Loop(i = Let(n),
                        Conditional(Gt(Length(i), Int32(1)),
                            DocOrderDistinct(TypeAssert(i, T.NodeNotRtfS)),
                            i
                        )
                    );
                }
            }

            return n;
        }

        public QilNode InvokeFormatMessage(QilNode res, QilNode args)
        {
            CheckString(res);
            CheckStringS(args);
            return XsltInvokeEarlyBound(QName("format-message"),
                XsltMethods.FormatMessage, T.StringX, new QilNode[] { res, args }
            );
        }

        #region Comparisons
        public QilNode InvokeEqualityOperator(QilNodeType op, QilNode left, QilNode right)
        {
            Debug.Assert(op == QilNodeType.Eq || op == QilNodeType.Ne);
            double opCode;
            left = TypeAssert(left, T.ItemS);
            right = TypeAssert(right, T.ItemS);

            switch (op)
            {
                case QilNodeType.Eq: opCode = (double)XsltLibrary.ComparisonOperator.Eq; break;
                default: opCode = (double)XsltLibrary.ComparisonOperator.Ne; break;
            }
            return XsltInvokeEarlyBound(QName("EqualityOperator"),
                XsltMethods.EqualityOperator, T.BooleanX, new QilNode[] { Double(opCode), left, right }
            );
        }

        public QilNode InvokeRelationalOperator(QilNodeType op, QilNode left, QilNode right)
        {
            Debug.Assert(op == QilNodeType.Lt || op == QilNodeType.Le || op == QilNodeType.Gt || op == QilNodeType.Ge);
            double opCode;
            left = TypeAssert(left, T.ItemS);
            right = TypeAssert(right, T.ItemS);

            switch (op)
            {
                case QilNodeType.Lt: opCode = (double)XsltLibrary.ComparisonOperator.Lt; break;
                case QilNodeType.Le: opCode = (double)XsltLibrary.ComparisonOperator.Le; break;
                case QilNodeType.Gt: opCode = (double)XsltLibrary.ComparisonOperator.Gt; break;
                default: opCode = (double)XsltLibrary.ComparisonOperator.Ge; break;
            }
            return XsltInvokeEarlyBound(QName("RelationalOperator"),
                XsltMethods.RelationalOperator, T.BooleanX, new QilNode[] { Double(opCode), left, right }
            );
        }
        #endregion

        #region Type Conversions
        [Conditional("DEBUG")]
        private void ExpectAny(QilNode n)
        {
            Debug.Assert(IsAnyType(n), "Unexpected expression type: " + n.XmlType.ToString());
        }

        public QilNode ConvertToType(XmlTypeCode requiredType, QilNode n)
        {
            switch (requiredType)
            {
                case XmlTypeCode.String: return ConvertToString(n);
                case XmlTypeCode.Double: return ConvertToNumber(n);
                case XmlTypeCode.Boolean: return ConvertToBoolean(n);
                case XmlTypeCode.Node: return EnsureNodeSet(n);
                case XmlTypeCode.Item: return n;
                default: Debug.Fail("Unexpected XmlTypeCode: " + requiredType); return null;
            }
        }

        // XPath spec $4.2, string()
        public QilNode ConvertToString(QilNode n)
        {
            switch (n.XmlType.TypeCode)
            {
                case XmlTypeCode.Boolean:
                    return (
                        n.NodeType == QilNodeType.True ? (QilNode)String("true") :
                        n.NodeType == QilNodeType.False ? (QilNode)String("false") :
                        /*default: */                     (QilNode)Conditional(n, String("true"), String("false"))
                    );
                case XmlTypeCode.Double:
                    return (n.NodeType == QilNodeType.LiteralDouble
                        ? (QilNode)String(XPathConvert.DoubleToString((double)(QilLiteral)n))
                        : (QilNode)XsltConvert(n, T.StringX)
                    );
                case XmlTypeCode.String:
                    return n;
                default:
                    if (n.XmlType.IsNode)
                    {
                        return XPathNodeValue(SafeDocOrderDistinct(n));
                    }

                    ExpectAny(n);
                    return XsltConvert(n, T.StringX);
            }
        }

        // XPath spec $4.3, boolean()
        public QilNode ConvertToBoolean(QilNode n)
        {
            switch (n.XmlType.TypeCode)
            {
                case XmlTypeCode.Boolean:
                    return n;
                case XmlTypeCode.Double:
                    // (x < 0 || 0 < x)  ==  (x != 0) && !Double.IsNaN(x)
                    QilIterator i;
                    return (n.NodeType == QilNodeType.LiteralDouble
                        ? Boolean((double)(QilLiteral)n < 0 || 0 < (double)(QilLiteral)n)
                        : Loop(i = Let(n), Or(Lt(i, Double(0)), Lt(Double(0), i)))
                    );
                case XmlTypeCode.String:
                    return (n.NodeType == QilNodeType.LiteralString
                        ? Boolean(((string)(QilLiteral)n).Length != 0)
                        : Ne(StrLength(n), Int32(0))
                    );
                default:
                    if (n.XmlType.IsNode)
                    {
                        return Not(IsEmpty(n));
                    }

                    ExpectAny(n);
                    return XsltConvert(n, T.BooleanX);
            }
        }

        // XPath spec $4.4, number()
        public QilNode ConvertToNumber(QilNode n)
        {
            switch (n.XmlType.TypeCode)
            {
                case XmlTypeCode.Boolean:
                    return (
                        n.NodeType == QilNodeType.True ? (QilNode)Double(1) :
                        n.NodeType == QilNodeType.False ? (QilNode)Double(0) :
                        /*default: */                 (QilNode)Conditional(n, Double(1), Double(0))
                    );
                case XmlTypeCode.Double:
                    return n;
                case XmlTypeCode.String:
                    return XsltConvert(n, T.DoubleX);
                default:
                    if (n.XmlType.IsNode)
                    {
                        return XsltConvert(XPathNodeValue(SafeDocOrderDistinct(n)), T.DoubleX);
                    }

                    ExpectAny(n);
                    return XsltConvert(n, T.DoubleX);
            }
        }

        public QilNode ConvertToNode(QilNode n)
        {
            if (n.XmlType.IsNode && n.XmlType.IsNotRtf && n.XmlType.IsSingleton)
            {
                return n;
            }
            return XsltConvert(n, T.NodeNotRtf);
        }

        public QilNode ConvertToNodeSet(QilNode n)
        {
            if (n.XmlType.IsNode && n.XmlType.IsNotRtf)
            {
                return n;
            }

            return XsltConvert(n, T.NodeNotRtfS);
        }

        // Returns null if the given expression is never a node-set
        public QilNode TryEnsureNodeSet(QilNode n)
        {
            if (n.XmlType.IsNode && n.XmlType.IsNotRtf)
            {
                return n;
            }
            if (CannotBeNodeSet(n))
            {
                return null;
            }

            // Ensure it is not an Rtf at runtime
            return InvokeEnsureNodeSet(n);
        }

        // Throws an exception if the given expression is never a node-set
        public QilNode EnsureNodeSet(QilNode n)
        {
            QilNode result = TryEnsureNodeSet(n);
            if (result == null)
            {
                throw new XPathCompileException(SR.XPath_NodeSetExpected);
            }
            return result;
        }

        public QilNode InvokeEnsureNodeSet(QilNode n)
        {
            return XsltInvokeEarlyBound(QName("ensure-node-set"),
                XsltMethods.EnsureNodeSet, T.NodeSDod, new QilNode[] { n }
            );
        }
        #endregion

        #region Other XPath Functions
        public QilNode Id(QilNode context, QilNode id)
        {
            CheckNodeNotRtf(context);

            if (id.XmlType.IsSingleton)
            {
                return Deref(context, ConvertToString(id));
            }

            QilIterator i;
            return Loop(i = For(id), Deref(context, ConvertToString(i)));
        }

        public QilNode InvokeStartsWith(QilNode str1, QilNode str2)
        {
            CheckString(str1);
            CheckString(str2);
            return XsltInvokeEarlyBound(QName("starts-with"),
                XsltMethods.StartsWith, T.BooleanX, new QilNode[] { str1, str2 }
            );
        }

        public QilNode InvokeContains(QilNode str1, QilNode str2)
        {
            CheckString(str1);
            CheckString(str2);
            return XsltInvokeEarlyBound(QName("contains"),
                XsltMethods.Contains, T.BooleanX, new QilNode[] { str1, str2 }
            );
        }

        public QilNode InvokeSubstringBefore(QilNode str1, QilNode str2)
        {
            CheckString(str1);
            CheckString(str2);
            return XsltInvokeEarlyBound(QName("substring-before"),
                XsltMethods.SubstringBefore, T.StringX, new QilNode[] { str1, str2 }
            );
        }

        public QilNode InvokeSubstringAfter(QilNode str1, QilNode str2)
        {
            CheckString(str1);
            CheckString(str2);
            return XsltInvokeEarlyBound(QName("substring-after"),
                XsltMethods.SubstringAfter, T.StringX, new QilNode[] { str1, str2 }
            );
        }

        public QilNode InvokeSubstring(QilNode str, QilNode start)
        {
            CheckString(str);
            CheckDouble(start);
            return XsltInvokeEarlyBound(QName("substring"),
                XsltMethods.Substring2, T.StringX, new QilNode[] { str, start }
            );
        }

        public QilNode InvokeSubstring(QilNode str, QilNode start, QilNode length)
        {
            CheckString(str);
            CheckDouble(start);
            CheckDouble(length);
            return XsltInvokeEarlyBound(QName("substring"),
                XsltMethods.Substring3, T.StringX, new QilNode[] { str, start, length }
            );
        }

        public QilNode InvokeNormalizeSpace(QilNode str)
        {
            CheckString(str);
            return XsltInvokeEarlyBound(QName("normalize-space"),
                XsltMethods.NormalizeSpace, T.StringX, new QilNode[] { str }
            );
        }

        public QilNode InvokeTranslate(QilNode str1, QilNode str2, QilNode str3)
        {
            CheckString(str1);
            CheckString(str2);
            CheckString(str3);
            return XsltInvokeEarlyBound(QName("translate"),
                XsltMethods.Translate, T.StringX, new QilNode[] { str1, str2, str3 }
            );
        }

        public QilNode InvokeLang(QilNode lang, QilNode context)
        {
            CheckString(lang);
            CheckNodeNotRtf(context);
            return XsltInvokeEarlyBound(QName(nameof(lang)),
                XsltMethods.Lang, T.BooleanX, new QilNode[] { lang, context }
            );
        }

        public QilNode InvokeFloor(QilNode value)
        {
            CheckDouble(value);
            return XsltInvokeEarlyBound(QName("floor"),
                XsltMethods.Floor, T.DoubleX, new QilNode[] { value }
            );
        }

        public QilNode InvokeCeiling(QilNode value)
        {
            CheckDouble(value);
            return XsltInvokeEarlyBound(QName("ceiling"),
                XsltMethods.Ceiling, T.DoubleX, new QilNode[] { value }
            );
        }

        public QilNode InvokeRound(QilNode value)
        {
            CheckDouble(value);
            return XsltInvokeEarlyBound(QName("round"),
                XsltMethods.Round, T.DoubleX, new QilNode[] { value }
            );
        }
        #endregion
    }
}
