// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Schema;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;
using System.Xml.Xsl.XPath;

namespace System.Xml.Xsl.Xslt
{
    using T = XmlQueryTypeFactory;

    internal sealed class XsltQilFactory : XPathQilFactory
    {
        public XsltQilFactory(QilFactory f, bool debug) : base(f, debug) { }

        [Conditional("DEBUG")]
        public void CheckXsltType(QilNode n)
        {
            // Five possible types are: anyType, node-set, string, boolean, and number
            XmlQueryType xt = n.XmlType!;
            switch (xt.TypeCode)
            {
                case XmlTypeCode.String:
                case XmlTypeCode.Boolean:
                case XmlTypeCode.Double:
                    Debug.Assert(xt.IsSingleton && xt.IsStrict, "Xslt assumes that these types will always be singleton and strict");
                    break;
                case XmlTypeCode.Item:
                case XmlTypeCode.None:
                    break;
                case XmlTypeCode.QName:
                    Debug.Assert(IsDebug, "QName is reserved as the marker for missing values");
                    break;
                default:
                    Debug.Assert(xt.IsNode, "Unexpected expression type: " + xt.ToString());
                    break;
            }
        }

        [Conditional("DEBUG")]
        public void CheckQName(QilNode n)
        {
            Debug.Assert(n != null && n.XmlType!.IsSubtypeOf(T.QNameX), "Must be a singleton QName");
        }

        // We use a value of XmlQualifiedName type to denote a missing parameter
        public QilNode DefaultValueMarker()
        {
            return QName("default-value", XmlReservedNs.NsXslDebug);
        }

        public QilNode InvokeIsSameNodeSort(QilNode n1, QilNode n2)
        {
            CheckNodeNotRtf(n1);
            CheckNodeNotRtf(n2);
            return XsltInvokeEarlyBound(QName("is-same-node-sort"),
                XsltMethods.IsSameNodeSort, T.BooleanX, new QilNode[] { n1, n2 }
            );
        }

        public QilNode InvokeSystemProperty(QilNode n)
        {
            CheckQName(n);
            return XsltInvokeEarlyBound(QName("system-property"),
                XsltMethods.SystemProperty, T.Choice(T.DoubleX, T.StringX), new QilNode[] { n }
            );
        }

        public QilNode InvokeElementAvailable(QilNode n)
        {
            CheckQName(n);
            return XsltInvokeEarlyBound(QName("element-available"),
                XsltMethods.ElementAvailable, T.BooleanX, new QilNode[] { n }
            );
        }

        public QilNode InvokeCheckScriptNamespace(string nsUri)
        {
            return XsltInvokeEarlyBound(QName("register-script-namespace"),
                XsltMethods.CheckScriptNamespace, T.IntX, new QilNode[] { String(nsUri) }
            );
        }

        public QilNode InvokeFunctionAvailable(QilNode n)
        {
            CheckQName(n);
            return XsltInvokeEarlyBound(QName("function-available"),
                XsltMethods.FunctionAvailable, T.BooleanX, new QilNode[] { n }
            );
        }

        public QilNode InvokeBaseUri(QilNode n)
        {
            CheckNode(n);
            return XsltInvokeEarlyBound(QName("base-uri"),
                XsltMethods.BaseUri, T.StringX, new QilNode[] { n }
            );
        }

        public QilNode InvokeOnCurrentNodeChanged(QilNode n)
        {
            CheckNode(n);
            return XsltInvokeEarlyBound(QName("on-current-node-changed"),
                XsltMethods.OnCurrentNodeChanged, T.IntX, new QilNode[] { n }
            );
        }

        public QilNode InvokeLangToLcid(QilNode n, bool fwdCompat)
        {
            CheckString(n);
            return XsltInvokeEarlyBound(QName("lang-to-lcid"),
                XsltMethods.LangToLcid, T.IntX, new QilNode[] { n, Boolean(fwdCompat) }
            );
        }

        public QilNode InvokeNumberFormat(QilNode value, QilNode format,
            QilNode lang, QilNode letterValue, QilNode groupingSeparator, QilNode groupingSize)
        {
            Debug.Assert(value != null && (
                value.XmlType!.IsSubtypeOf(T.IntXS) ||
                value.XmlType.IsSubtypeOf(T.DoubleX)),
                "Value must be either a sequence of ints, or a double singleton"
            );
            CheckString(format);
            CheckDouble(lang);
            CheckString(letterValue);
            CheckString(groupingSeparator);
            CheckDouble(groupingSize);

            return XsltInvokeEarlyBound(QName("number-format"),
                XsltMethods.NumberFormat, T.StringX,
                new QilNode[] { value, format, lang, letterValue, groupingSeparator, groupingSize }
            );
        }

        public QilNode InvokeRegisterDecimalFormat(DecimalFormatDecl format)
        {
            Debug.Assert(format != null);
            return XsltInvokeEarlyBound(QName("register-decimal-format"),
                XsltMethods.RegisterDecimalFormat, T.IntX,
                new QilNode[] {
                    QName(format.Name.Name, format.Name.Namespace),
                    String(format.InfinitySymbol), String(format.NanSymbol), String(new string(format.Characters))
                }
            );
        }

        public QilNode InvokeRegisterDecimalFormatter(QilNode formatPicture, DecimalFormatDecl format)
        {
            CheckString(formatPicture);
            Debug.Assert(format != null);
            return XsltInvokeEarlyBound(QName("register-decimal-formatter"),
                XsltMethods.RegisterDecimalFormatter, T.DoubleX,
                new QilNode[] {
                    formatPicture,
                    String(format.InfinitySymbol), String(format.NanSymbol), String(new string(format.Characters))
                }
            );
        }

        public QilNode InvokeFormatNumberStatic(QilNode value, QilNode decimalFormatIndex)
        {
            CheckDouble(value);
            CheckDouble(decimalFormatIndex);
            return XsltInvokeEarlyBound(QName("format-number-static"),
                XsltMethods.FormatNumberStatic, T.StringX, new QilNode[] { value, decimalFormatIndex }
            );
        }

        public QilNode InvokeFormatNumberDynamic(QilNode value, QilNode formatPicture, QilNode decimalFormatName, QilNode errorMessageName)
        {
            CheckDouble(value);
            CheckString(formatPicture);
            CheckQName(decimalFormatName);
            CheckString(errorMessageName);
            return XsltInvokeEarlyBound(QName("format-number-dynamic"),
                XsltMethods.FormatNumberDynamic, T.StringX, new QilNode[] { value, formatPicture, decimalFormatName, errorMessageName }
            );
        }

        public QilNode InvokeOuterXml(QilNode n)
        {
            CheckNode(n);
            return XsltInvokeEarlyBound(QName("outer-xml"),
                XsltMethods.OuterXml, T.StringX, new QilNode[] { n }
            );
        }

        public QilNode InvokeMsFormatDateTime(QilNode datetime, QilNode format, QilNode lang, QilNode isDate)
        {
            CheckString(datetime);
            CheckString(format);
            CheckString(lang);
            CheckBool(isDate);
            return XsltInvokeEarlyBound(QName("ms:format-date-time"),
                XsltMethods.MSFormatDateTime, T.StringX, new QilNode[] { datetime, format, lang, isDate }
            );
        }

        public QilNode InvokeMsStringCompare(QilNode x, QilNode y, QilNode lang, QilNode options)
        {
            CheckString(x);
            CheckString(y);
            CheckString(lang);
            CheckString(options);
            return XsltInvokeEarlyBound(QName("ms:string-compare"),
                XsltMethods.MSStringCompare, T.DoubleX, new QilNode[] { x, y, lang, options }
            );
        }

        public QilNode InvokeMsUtc(QilNode n)
        {
            CheckString(n);
            return XsltInvokeEarlyBound(QName("ms:utc"),
                XsltMethods.MSUtc, T.StringX, new QilNode[] { n }
            );
        }

        public QilNode InvokeMsNumber(QilNode n)
        {
            return XsltInvokeEarlyBound(QName("ms:number"),
                XsltMethods.MSNumber, T.DoubleX, new QilNode[] { n }
            );
        }

        public QilNode InvokeMsLocalName(QilNode n)
        {
            CheckString(n);
            return XsltInvokeEarlyBound(QName("ms:local-name"),
                XsltMethods.MSLocalName, T.StringX, new QilNode[] { n }
            );
        }

        public QilNode InvokeMsNamespaceUri(QilNode n, QilNode currentNode)
        {
            CheckString(n);
            CheckNodeNotRtf(currentNode);
            return XsltInvokeEarlyBound(QName("ms:namespace-uri"),
                XsltMethods.MSNamespaceUri, T.StringX, new QilNode[] { n, currentNode }
            );
        }

        public QilNode InvokeEXslObjectType(QilNode n)
        {
            return XsltInvokeEarlyBound(QName("exsl:object-type"),
                XsltMethods.EXslObjectType, T.StringX, new QilNode[] { n }
            );
        }
    }
}
