// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        Query arg;
        FT funcType;

        public BooleanFunctions(FT funcType, Query arg)
        {
            this.arg = arg;
            this.funcType = funcType;
        }
        private BooleanFunctions(BooleanFunctions other) : base(other)
        {
            this.arg = Clone(other.arg);
            this.funcType = other.funcType;
        }

        public override void SetXsltContext(XsltContext context)
        {
            if (arg != null)
            {
                arg.SetXsltContext(context);
            }
        }

        public override object Evaluate(XPathNodeIterator nodeIterator)
        {
            switch (funcType)
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
            object result = arg.Evaluate(nodeIterator);
            if (result is XPathNodeIterator) return arg.Advance() != null;
            if (result is string) return toBoolean((string)result);
            if (result is double) return toBoolean((double)result);
            if (result is bool) return (bool)result;
            Debug.Assert(result is XPathNavigator, "Unknown value type");
            return true;
        }

        public override XPathResultType StaticType { get { return XPathResultType.Boolean; } }

        private bool Not(XPathNodeIterator nodeIterator)
        {
            return !(bool)arg.Evaluate(nodeIterator);
        }

        private bool Lang(XPathNodeIterator nodeIterator)
        {
            string str = arg.Evaluate(nodeIterator).ToString();
            string lang = nodeIterator.Current.XmlLang;
            return (
               lang.StartsWith(str, StringComparison.OrdinalIgnoreCase) &&
               (lang.Length == str.Length || lang[str.Length] == '-')
            );
        }

        public override XPathNodeIterator Clone() { return new BooleanFunctions(this); }

        public override void PrintQuery(XmlWriter w)
        {
            w.WriteStartElement(this.GetType().Name);
            w.WriteAttributeString("name", funcType.ToString());
            if (arg != null)
            {
                arg.PrintQuery(w);
            }
            w.WriteEndElement();
        }
    }
}
