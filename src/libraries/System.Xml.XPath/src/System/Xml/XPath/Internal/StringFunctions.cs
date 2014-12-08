// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal sealed class StringFunctions : ValueQuery
    {
        Function.FunctionType funcType;
        private IList<Query> argList;

        public StringFunctions(Function.FunctionType funcType, IList<Query> argList)
        {
            Debug.Assert(argList != null, "Use 'new Query[]{}' instead.");
            this.funcType = funcType;
            this.argList = argList;
        }
        private StringFunctions(StringFunctions other) : base(other)
        {
            this.funcType = other.funcType;
            Query[] tmp = new Query[other.argList.Count];
            {
                for (int i = 0; i < tmp.Length; i++)
                {
                    tmp[i] = Clone(other.argList[i]);
                }
            }
            this.argList = tmp;
        }

        public override void SetXsltContext(XsltContext context)
        {
            for (int i = 0; i < argList.Count; i++)
            {
                argList[i].SetXsltContext(context);
            }
        }

        public override object Evaluate(XPathNodeIterator nodeIterator)
        {
            switch (funcType)
            {
                case Function.FunctionType.FuncString: return toString(nodeIterator);
                case Function.FunctionType.FuncConcat: return Concat(nodeIterator);
                case Function.FunctionType.FuncStartsWith: return StartsWith(nodeIterator);
                case Function.FunctionType.FuncContains: return Contains(nodeIterator);
                case Function.FunctionType.FuncSubstringBefore: return SubstringBefore(nodeIterator);
                case Function.FunctionType.FuncSubstringAfter: return SubstringAfter(nodeIterator);
                case Function.FunctionType.FuncSubstring: return Substring(nodeIterator);
                case Function.FunctionType.FuncStringLength: return StringLength(nodeIterator);
                case Function.FunctionType.FuncNormalize: return Normalize(nodeIterator);
                case Function.FunctionType.FuncTranslate: return Translate(nodeIterator);
            }
            return string.Empty;
        }

        internal static string toString(double num)
        {
            return num.ToString("R", NumberFormatInfo.InvariantInfo);
        }

        internal static string toString(bool b)
        {
            return b ? "true" : "false";
        }

        private string toString(XPathNodeIterator nodeIterator)
        {
            if (argList.Count > 0)
            {
                object argVal = argList[0].Evaluate(nodeIterator);

                switch (GetXPathType(argVal))
                {
                    case XPathResultType.NodeSet:
                        XPathNavigator value = argList[0].Advance();
                        return value != null ? value.Value : string.Empty;
                    case XPathResultType.String:
                        return (string)argVal;
                    case XPathResultType.Boolean:
                        return ((bool)argVal) ? "true" : "false";
                    case XPathResultType_Navigator:
                        return ((XPathNavigator)argVal).Value;
                    default:
                        Debug.Assert(GetXPathType(argVal) == XPathResultType.Number);
                        return toString((double)argVal);
                }
            }
            return nodeIterator.Current.Value;
        }

        public override XPathResultType StaticType
        {
            get
            {
                if (funcType == Function.FunctionType.FuncStringLength)
                {
                    return XPathResultType.Number;
                }
                if (
                    funcType == Function.FunctionType.FuncStartsWith ||
                    funcType == Function.FunctionType.FuncContains
                )
                {
                    return XPathResultType.Boolean;
                }
                return XPathResultType.String;
            }
        }

        private string Concat(XPathNodeIterator nodeIterator)
        {
            int count = 0;
            StringBuilder s = new StringBuilder();
            while (count < argList.Count)
            {
                s.Append(argList[count++].Evaluate(nodeIterator).ToString());
            }
            return s.ToString();
        }

        private bool StartsWith(XPathNodeIterator nodeIterator)
        {
            string s1 = argList[0].Evaluate(nodeIterator).ToString();
            string s2 = argList[1].Evaluate(nodeIterator).ToString();
            return s1.Length >= s2.Length && string.CompareOrdinal(s1, 0, s2, 0, s2.Length) == 0;
        }

        private static readonly CompareInfo compareInfo = CultureInfo.InvariantCulture.CompareInfo;

        private bool Contains(XPathNodeIterator nodeIterator)
        {
            string s1 = argList[0].Evaluate(nodeIterator).ToString();
            string s2 = argList[1].Evaluate(nodeIterator).ToString();
            return compareInfo.IndexOf(s1, s2, CompareOptions.Ordinal) >= 0;
        }

        private string SubstringBefore(XPathNodeIterator nodeIterator)
        {
            string s1 = argList[0].Evaluate(nodeIterator).ToString();
            string s2 = argList[1].Evaluate(nodeIterator).ToString();
            if (s2.Length == 0) { return s2; }
            int idx = compareInfo.IndexOf(s1, s2, CompareOptions.Ordinal);
            return (idx < 1) ? string.Empty : s1.Substring(0, idx);
        }

        private string SubstringAfter(XPathNodeIterator nodeIterator)
        {
            string s1 = argList[0].Evaluate(nodeIterator).ToString();
            string s2 = argList[1].Evaluate(nodeIterator).ToString();
            if (s2.Length == 0) { return s1; }
            int idx = compareInfo.IndexOf(s1, s2, CompareOptions.Ordinal);
            return (idx < 0) ? string.Empty : s1.Substring(idx + s2.Length);
        }

        private string Substring(XPathNodeIterator nodeIterator)
        {
            string str1 = argList[0].Evaluate(nodeIterator).ToString();
            double num = XmlConvertEx.XPathRound(XmlConvertEx.ToXPathDouble(argList[1].Evaluate(nodeIterator))) - 1;

            if (Double.IsNaN(num) || str1.Length <= num)
            {
                return string.Empty;
            }
            if (argList.Count == 3)
            {
                double num1 = XmlConvertEx.XPathRound(XmlConvertEx.ToXPathDouble(argList[2].Evaluate(nodeIterator)));
                if (Double.IsNaN(num1))
                {
                    return string.Empty;
                }
                if (num < 0 || num1 < 0)
                {
                    num1 = num + num1;
                    // NOTE: condition is true for NaN
                    if (!(num1 > 0))
                    {
                        return string.Empty;
                    }
                    num = 0;
                }
                double maxlength = str1.Length - num;
                if (num1 > maxlength)
                {
                    num1 = maxlength;
                }
                return str1.Substring((int)num, (int)num1);
            }
            if (num < 0)
            {
                num = 0;
            }
            return str1.Substring((int)num);
        }

        private Double StringLength(XPathNodeIterator nodeIterator)
        {
            if (argList.Count > 0)
            {
                return argList[0].Evaluate(nodeIterator).ToString().Length;
            }
            return nodeIterator.Current.Value.Length;
        }

        private string Normalize(XPathNodeIterator nodeIterator)
        {
            string str1;
            if (argList.Count > 0)
            {
                str1 = argList[0].Evaluate(nodeIterator).ToString();
            }
            else
            {
                str1 = nodeIterator.Current.Value;
            }
            str1 = XmlConvertEx.TrimString(str1);
            int count = 0;
            StringBuilder str2 = new StringBuilder();
            bool FirstSpace = true;
            XmlCharType xmlCharType = XmlCharType.Instance;
            while (count < str1.Length)
            {
                if (!xmlCharType.IsWhiteSpace(str1[count]))
                {
                    FirstSpace = true;
                    str2.Append(str1[count]);
                }
                else if (FirstSpace)
                {
                    FirstSpace = false;
                    str2.Append(' ');
                }
                count++;
            }
            return str2.ToString();
        }
        private string Translate(XPathNodeIterator nodeIterator)
        {
            string str1 = argList[0].Evaluate(nodeIterator).ToString();
            string str2 = argList[1].Evaluate(nodeIterator).ToString();
            string str3 = argList[2].Evaluate(nodeIterator).ToString();
            int count = 0, index;
            StringBuilder str = new StringBuilder();
            while (count < str1.Length)
            {
                index = str2.IndexOf(str1[count]);
                if (index != -1)
                {
                    if (index < str3.Length)
                    {
                        str.Append(str3[index]);
                    }
                }
                else
                {
                    str.Append(str1[count]);
                }
                count++;
            }
            return str.ToString();
        }

        public override XPathNodeIterator Clone() { return new StringFunctions(this); }

        public override void PrintQuery(XmlWriter w)
        {
            w.WriteStartElement(this.GetType().Name);
            w.WriteAttributeString("name", funcType.ToString());
            foreach (Query arg in this.argList)
            {
                arg.PrintQuery(w);
            }
            w.WriteEndElement();
        }
    }
}
