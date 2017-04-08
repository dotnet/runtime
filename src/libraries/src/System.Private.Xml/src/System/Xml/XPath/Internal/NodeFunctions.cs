// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using FT = MS.Internal.Xml.XPath.Function.FunctionType;

namespace MS.Internal.Xml.XPath
{
    internal sealed class NodeFunctions : ValueQuery
    {
        private Query _arg = null;
        private FT _funcType;
        private XsltContext _xsltContext;

        public NodeFunctions(FT funcType, Query arg)
        {
            _funcType = funcType;
            _arg = arg;
        }

        public override void SetXsltContext(XsltContext context)
        {
            _xsltContext = context.Whitespace ? context : null;
            if (_arg != null)
            {
                _arg.SetXsltContext(context);
            }
        }

        private XPathNavigator EvaluateArg(XPathNodeIterator context)
        {
            if (_arg == null)
            {
                return context.Current;
            }
            _arg.Evaluate(context);
            return _arg.Advance();
        }

        public override object Evaluate(XPathNodeIterator context)
        {
            XPathNavigator argVal;

            switch (_funcType)
            {
                case FT.FuncPosition:
                    return (double)context.CurrentPosition;
                case FT.FuncLast:
                    return (double)context.Count;
                case FT.FuncNameSpaceUri:
                    argVal = EvaluateArg(context);
                    if (argVal != null)
                    {
                        return argVal.NamespaceURI;
                    }
                    break;
                case FT.FuncLocalName:
                    argVal = EvaluateArg(context);
                    if (argVal != null)
                    {
                        return argVal.LocalName;
                    }
                    break;
                case FT.FuncName:
                    argVal = EvaluateArg(context);
                    if (argVal != null)
                    {
                        return argVal.Name;
                    }
                    break;
                case FT.FuncCount:
                    _arg.Evaluate(context);
                    int count = 0;
                    if (_xsltContext != null)
                    {
                        XPathNavigator nav;
                        while ((nav = _arg.Advance()) != null)
                        {
                            if (nav.NodeType != XPathNodeType.Whitespace || _xsltContext.PreserveWhitespace(nav))
                            {
                                count++;
                            }
                        }
                    }
                    else
                    {
                        while (_arg.Advance() != null)
                        {
                            count++;
                        }
                    }
                    return (double)count;
            }
            return string.Empty;
        }

        public override XPathResultType StaticType { get { return Function.ReturnTypes[(int)_funcType]; } }

        public override XPathNodeIterator Clone()
        {
            NodeFunctions method = new NodeFunctions(_funcType, Clone(_arg));
            method._xsltContext = _xsltContext;
            return method;
        }
    }
}
