// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using FT = MS.Internal.Xml.XPath.Function.FunctionType;

namespace MS.Internal.Xml.XPath
{
    internal sealed class NumberFunctions : ValueQuery
    {
        private Query _arg = null;
        private FT _ftype;

        public NumberFunctions(FT ftype, Query arg)
        {
            _arg = arg;
            _ftype = ftype;
        }
        private NumberFunctions(NumberFunctions other) : base(other)
        {
            _arg = Clone(other._arg);
            _ftype = other._ftype;
        }

        public override void SetXsltContext(XsltContext context)
        {
            if (_arg != null)
            {
                _arg.SetXsltContext(context);
            }
        }

        internal static double Number(bool arg)
        {
            return arg ? 1.0 : 0.0;
        }
        internal static double Number(string arg)
        {
            return XmlConvertEx.ToXPathDouble(arg);
        }

        public override object Evaluate(XPathNodeIterator nodeIterator)
        {
            switch (_ftype)
            {
                case FT.FuncNumber: return Number(nodeIterator);
                case FT.FuncSum: return Sum(nodeIterator);
                case FT.FuncFloor: return Floor(nodeIterator);
                case FT.FuncCeiling: return Ceiling(nodeIterator);
                case FT.FuncRound: return Round(nodeIterator);
            }
            return null;
        }

        private double Number(XPathNodeIterator nodeIterator)
        {
            if (_arg == null)
            {
                return XmlConvertEx.ToXPathDouble(nodeIterator.Current.Value);
            }
            object argVal = _arg.Evaluate(nodeIterator);
            switch (GetXPathType(argVal))
            {
                case XPathResultType.NodeSet:
                    XPathNavigator value = _arg.Advance();
                    if (value != null)
                    {
                        return Number(value.Value);
                    }
                    break;
                case XPathResultType.String:
                    return Number((string)argVal);
                case XPathResultType.Boolean:
                    return Number((bool)argVal);
                case XPathResultType.Number:
                    return (double)argVal;
                case XPathResultType_Navigator:
                    return Number(((XPathNavigator)argVal).Value);
            }
            return double.NaN;
        }

        private double Sum(XPathNodeIterator nodeIterator)
        {
            double sum = 0;
            _arg.Evaluate(nodeIterator);
            XPathNavigator nav;
            while ((nav = _arg.Advance()) != null)
            {
                sum += Number(nav.Value);
            }
            return sum;
        }

        private double Floor(XPathNodeIterator nodeIterator)
        {
            return Math.Floor((double)_arg.Evaluate(nodeIterator));
        }

        private double Ceiling(XPathNodeIterator nodeIterator)
        {
            return Math.Ceiling((double)_arg.Evaluate(nodeIterator));
        }

        private double Round(XPathNodeIterator nodeIterator)
        {
            double n = XmlConvertEx.ToXPathDouble(_arg.Evaluate(nodeIterator));
            return XmlConvertEx.XPathRound(n);
        }

        public override XPathResultType StaticType { get { return XPathResultType.Number; } }

        public override XPathNodeIterator Clone() { return new NumberFunctions(this); }

        public override void PrintQuery(XmlWriter w)
        {
            w.WriteStartElement(this.GetType().Name);
            w.WriteAttributeString("name", _ftype.ToString());
            if (_arg != null)
            {
                _arg.PrintQuery(w);
            }
            w.WriteEndElement();
        }
    }
}
