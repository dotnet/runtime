// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
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
        private readonly Function.FunctionType _funcType;
        private readonly IList<Query> _argList;

        public StringFunctions(Function.FunctionType funcType, IList<Query> argList)
        {
            Debug.Assert(argList != null, "Use 'new Query[]{}' instead.");
            _funcType = funcType;
            _argList = argList;
        }
        private StringFunctions(StringFunctions other) : base(other)
        {
            _funcType = other._funcType;
            Query[] tmp = new Query[other._argList.Count];
            {
                for (int i = 0; i < tmp.Length; i++)
                {
                    tmp[i] = Clone(other._argList[i]);
                }
            }
            _argList = tmp;
        }

        public override void SetXsltContext(XsltContext context)
        {
            for (int i = 0; i < _argList.Count; i++)
            {
                _argList[i].SetXsltContext(context);
            }
        }

        public override object Evaluate(XPathNodeIterator nodeIterator) =>
            _funcType switch
            {
                Function.FunctionType.FuncString => toString(nodeIterator),
                Function.FunctionType.FuncConcat => Concat(nodeIterator),
                Function.FunctionType.FuncStartsWith => StartsWith(nodeIterator),
                Function.FunctionType.FuncContains => Contains(nodeIterator),
                Function.FunctionType.FuncSubstringBefore => SubstringBefore(nodeIterator),
                Function.FunctionType.FuncSubstringAfter => SubstringAfter(nodeIterator),
                Function.FunctionType.FuncSubstring => Substring(nodeIterator),
                Function.FunctionType.FuncStringLength => StringLength(nodeIterator),
                Function.FunctionType.FuncNormalize => Normalize(nodeIterator),
                Function.FunctionType.FuncTranslate => Translate(nodeIterator),
                _ => string.Empty,
            };

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
            if (_argList.Count > 0)
            {
                object argVal = _argList[0].Evaluate(nodeIterator);

                switch (GetXPathType(argVal))
                {
                    case XPathResultType.NodeSet:
                        XPathNavigator? value = _argList[0].Advance();
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
            Debug.Assert(nodeIterator.Current != null);
            return nodeIterator.Current.Value;
        }

        public override XPathResultType StaticType
        {
            get
            {
                if (_funcType == Function.FunctionType.FuncStringLength)
                {
                    return XPathResultType.Number;
                }
                if (
                    _funcType == Function.FunctionType.FuncStartsWith ||
                    _funcType == Function.FunctionType.FuncContains
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
            while (count < _argList.Count)
            {
                s.Append(_argList[count++].Evaluate(nodeIterator).ToString());
            }
            return s.ToString();
        }

        private bool StartsWith(XPathNodeIterator nodeIterator)
        {
            Debug.Assert(_argList.Count > 1);
            string s1 = _argList[0].Evaluate(nodeIterator).ToString()!;
            string s2 = _argList[1].Evaluate(nodeIterator).ToString()!;
            return s1.Length >= s2.Length && string.CompareOrdinal(s1, 0, s2, 0, s2.Length) == 0;
        }

        private static readonly CompareInfo s_compareInfo = CultureInfo.InvariantCulture.CompareInfo;

        private bool Contains(XPathNodeIterator nodeIterator)
        {
            Debug.Assert(_argList.Count > 1);
            string s1 = _argList[0].Evaluate(nodeIterator).ToString()!;
            string s2 = _argList[1].Evaluate(nodeIterator).ToString()!;
            return s_compareInfo.IndexOf(s1, s2, CompareOptions.Ordinal) >= 0;
        }

        private string SubstringBefore(XPathNodeIterator nodeIterator)
        {
            Debug.Assert(_argList.Count > 1);
            string s1 = _argList[0].Evaluate(nodeIterator).ToString()!;
            string s2 = _argList[1].Evaluate(nodeIterator).ToString()!;
            if (s2.Length == 0) { return s2; }
            int idx = s_compareInfo.IndexOf(s1, s2, CompareOptions.Ordinal);
            return (idx < 1) ? string.Empty : s1.Substring(0, idx);
        }

        private string SubstringAfter(XPathNodeIterator nodeIterator)
        {
            Debug.Assert(_argList.Count > 1);
            string s1 = _argList[0].Evaluate(nodeIterator).ToString()!;
            string s2 = _argList[1].Evaluate(nodeIterator).ToString()!;
            if (s2.Length == 0) { return s1; }
            int idx = s_compareInfo.IndexOf(s1, s2, CompareOptions.Ordinal);
            return (idx < 0) ? string.Empty : s1.Substring(idx + s2.Length);
        }

        private string Substring(XPathNodeIterator nodeIterator)
        {
            Debug.Assert(_argList.Count > 0);
            string str1 = _argList[0].Evaluate(nodeIterator).ToString()!;
            double num = XmlConvert.XPathRound(XmlConvert.ToXPathDouble(_argList[1].Evaluate(nodeIterator))) - 1;

            if (double.IsNaN(num) || str1.Length <= num)
            {
                return string.Empty;
            }
            if (_argList.Count == 3)
            {
                double num1 = XmlConvert.XPathRound(XmlConvert.ToXPathDouble(_argList[2].Evaluate(nodeIterator)));
                if (double.IsNaN(num1))
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

        private double StringLength(XPathNodeIterator nodeIterator)
        {
            if (_argList.Count > 0)
            {
                return _argList[0].Evaluate(nodeIterator).ToString()!.Length;
            }
            Debug.Assert(nodeIterator.Current != null);
            return nodeIterator.Current.Value.Length;
        }

        private string Normalize(XPathNodeIterator nodeIterator)
        {
            string value;
            if (_argList.Count > 0)
            {
                value = _argList[0].Evaluate(nodeIterator).ToString()!;
            }
            else
            {
                Debug.Assert(nodeIterator.Current != null);
                value = nodeIterator.Current.Value;
            }
            int modifyPos = -1;
            char[] chars = value.ToCharArray();
            bool firstSpace = false; // Start false to trim the beginning
            XmlCharType xmlCharType = XmlCharType.Instance;

            for (int comparePos = 0; comparePos < chars.Length; comparePos++)
            {
                if (!xmlCharType.IsWhiteSpace(chars[comparePos]))
                {
                    firstSpace = true;
                    modifyPos++;
                    chars[modifyPos] = chars[comparePos];
                }
                else if (firstSpace)
                {
                    firstSpace = false;
                    modifyPos++;
                    chars[modifyPos] = ' ';
                }
            }

            // Trim end
            if (modifyPos > -1 && chars[modifyPos] == ' ')
                modifyPos--;

            return new string(chars, 0, modifyPos + 1);
        }

        private string Translate(XPathNodeIterator nodeIterator)
        {
            Debug.Assert(_argList.Count > 2);
            string value = _argList[0].Evaluate(nodeIterator).ToString()!;
            string mapFrom = _argList[1].Evaluate(nodeIterator).ToString()!;
            string mapTo = _argList[2].Evaluate(nodeIterator).ToString()!;
            int modifyPos = -1;
            char[] chars = value.ToCharArray();

            for (int comparePos = 0; comparePos < chars.Length; comparePos++)
            {
                int index = mapFrom.IndexOf(chars[comparePos]);
                if (index != -1)
                {
                    if (index < mapTo.Length)
                    {
                        modifyPos++;
                        chars[modifyPos] = mapTo[index];
                    }
                }
                else
                {
                    modifyPos++;
                    chars[modifyPos] = chars[comparePos];
                }
            }

            return new string(chars, 0, modifyPos + 1);
        }

        public override XPathNodeIterator Clone() { return new StringFunctions(this); }
    }
}
