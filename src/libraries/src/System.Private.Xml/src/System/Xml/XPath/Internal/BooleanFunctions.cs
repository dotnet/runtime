// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using FT = MS.Internal.Xml.XPath.Function.FunctionType;

namespace MS.Internal.Xml.XPath
{
    internal sealed class BooleanFunctions : ValueQuery
    {
        private Query _arg;
        private FT _funcType;

        public BooleanFunctions(FT funcType, Query arg)
        {
            _arg = arg;
            _funcType = funcType;
        }
        private BooleanFunctions(BooleanFunctions other) : base(other)
        {
            _arg = Clone(other._arg);
            _funcType = other._funcType;
        }

        public override void SetXsltContext(XsltContext context)
        {
            if (_arg != null)
            {
                _arg.SetXsltContext(context);
            }
        }

        public override object Evaluate(XPathNodeIterator nodeIterator)
        {
            switch (_funcType)
            {
                case FT.FuncBoolean: return toBoolean(nodeIterator);
                case FT.FuncNot: return Not(nodeIterator);
                case FT.FuncTrue: return true;
                case FT.FuncFalse: return false;
                case FT.FuncLang: return Lang(nodeIterator);
            }
            return false;
        }

        internal static bool toBoolean(double number)
        {
            return number != 0 && !double.IsNaN(number);
        }
        internal static bool toBoolean(string str)
        {
            return str.Length > 0;
        }

        internal bool toBoolean(XPathNodeIterator nodeIterator)
        {
            object result = _arg.Evaluate(nodeIterator);
            if (result is XPathNodeIterator) return _arg.Advance() != null;

            string str = result as string;
            if (str != null)
                return toBoolean(str);

            if (result is double) return toBoolean((double)result);
            if (result is bool) return (bool)result;
            Debug.Assert(result is XPathNavigator, "Unknown value type");
            return true;
        }

        public override XPathResultType StaticType { get { return XPathResultType.Boolean; } }

        private bool Not(XPathNodeIterator nodeIterator)
        {
            return !(bool)_arg.Evaluate(nodeIterator);
        }

        private bool Lang(XPathNodeIterator nodeIterator)
        {
            string str = _arg.Evaluate(nodeIterator).ToString();
            string lang = nodeIterator.Current.XmlLang;
            return (
               lang.StartsWith(str, StringComparison.OrdinalIgnoreCase) &&
               (lang.Length == str.Length || lang[str.Length] == '-')
            );
        }

        public override XPathNodeIterator Clone() { return new BooleanFunctions(this); }
    }
}
