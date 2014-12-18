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
        private Query arg = null;
        private FT ftype;

        public NumberFunctions(FT ftype, Query arg)
        {
            this.arg = arg;
            this.ftype = ftype;
        }
        private NumberFunctions(NumberFunctions other) : base(other)
        {
            this.arg = Clone(other.arg);
            this.ftype = other.ftype;
        }

        public override void SetXsltContext(XsltContext context)
        {
            if (arg != null)
            {
                arg.SetXsltContext(context);
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
            switch (ftype)
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
            if (arg == null)
            {
                return XmlConvertEx.ToXPathDouble(nodeIterator.Current.Value);
            }
            object argVal = arg.Evaluate(nodeIterator);
            switch (GetXPathType(argVal))
            {
                case XPathResultType.NodeSet:
                    XPathNavigator value = arg.Advance();
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
            arg.Evaluate(nodeIterator);
            XPathNavigator nav;
            while ((nav = arg.Advance()) != null)
            {
                sum += Number(nav.Value);
            }
            return sum;
        }

        private double Floor(XPathNodeIterator nodeIterator)
        {
            return Math.Floor((double)arg.Evaluate(nodeIterator));
        }

        private double Ceiling(XPathNodeIterator nodeIterator)
        {
            return Math.Ceiling((double)arg.Evaluate(nodeIterator));
        }

        private double Round(XPathNodeIterator nodeIterator)
        {
            double n = XmlConvertEx.ToXPathDouble(arg.Evaluate(nodeIterator));
            return XmlConvertEx.XPathRound(n);
        }

        public override XPathResultType StaticType { get { return XPathResultType.Number; } }

        public override XPathNodeIterator Clone() { return new NumberFunctions(this); }

        public override void PrintQuery(XmlWriter w)
        {
            w.WriteStartElement(this.GetType().Name);
            w.WriteAttributeString("name", ftype.ToString());
            if (arg != null)
            {
                arg.PrintQuery(w);
            }
            w.WriteEndElement();
        }
    }
}
