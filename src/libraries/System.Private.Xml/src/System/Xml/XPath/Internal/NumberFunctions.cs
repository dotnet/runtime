// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using FT = MS.Internal.Xml.XPath.Function.FunctionType;

namespace MS.Internal.Xml.XPath
{
    internal sealed class NumberFunctions : ValueQuery
    {
        private readonly Query? _arg;
        private readonly FT _ftype;

        public NumberFunctions(FT ftype, Query? arg)
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
            _arg?.SetXsltContext(context);
        }

        internal static double Number(bool arg)
        {
            return arg ? 1.0 : 0.0;
        }
        internal static double Number(string arg)
        {
            return XmlConvert.ToXPathDouble(arg);
        }

        public override object Evaluate(XPathNodeIterator nodeIterator) =>
            _ftype switch
            {
                FT.FuncNumber => (object)Number(nodeIterator),
                FT.FuncSum => Sum(nodeIterator),
                FT.FuncFloor => Floor(nodeIterator),
                FT.FuncCeiling => Ceiling(nodeIterator),
                FT.FuncRound => Round(nodeIterator),
                _ => throw new InvalidOperationException(),
            };

        private double Number(XPathNodeIterator nodeIterator)
        {
            if (_arg == null)
            {
                Debug.Assert(nodeIterator!.Current != null);
                return XmlConvert.ToXPathDouble(nodeIterator.Current.Value);
            }
            object argVal = _arg.Evaluate(nodeIterator);
            switch (GetXPathType(argVal))
            {
                case XPathResultType.NodeSet:
                    XPathNavigator? value = _arg.Advance();
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
            Debug.Assert(_arg != null);
            _arg.Evaluate(nodeIterator);
            XPathNavigator? nav;
            while ((nav = _arg.Advance()) != null)
            {
                sum += Number(nav.Value);
            }
            return sum;
        }

        private double Floor(XPathNodeIterator nodeIterator)
        {
            Debug.Assert(_arg != null);
            return Math.Floor((double)_arg.Evaluate(nodeIterator));
        }

        private double Ceiling(XPathNodeIterator nodeIterator)
        {
            Debug.Assert(_arg != null);
            return Math.Ceiling((double)_arg.Evaluate(nodeIterator));
        }

        private double Round(XPathNodeIterator nodeIterator)
        {
            Debug.Assert(_arg != null);
            double n = XmlConvert.ToXPathDouble(_arg.Evaluate(nodeIterator));
            return XmlConvert.XPathRound(n);
        }

        public override XPathResultType StaticType { get { return XPathResultType.Number; } }

        public override XPathNodeIterator Clone() { return new NumberFunctions(this); }
    }
}
