// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using FT = MS.Internal.Xml.XPath.Function.FunctionType;

namespace MS.Internal.Xml.XPath
{
    internal sealed class NodeFunctions : ValueQuery
    {
        Query arg = null;
        FT funcType;
        XsltContext xsltContext;

        public NodeFunctions(FT funcType, Query arg)
        {
            this.funcType = funcType;
            this.arg = arg;
        }

        public override void SetXsltContext(XsltContext context)
        {
            this.xsltContext = context.Whitespace ? context : null;
            if (arg != null)
            {
                arg.SetXsltContext(context);
            }
        }

        private XPathNavigator EvaluateArg(XPathNodeIterator context)
        {
            if (arg == null)
            {
                return context.Current;
            }
            arg.Evaluate(context);
            return arg.Advance();
        }

        public override object Evaluate(XPathNodeIterator context)
        {
            XPathNavigator argVal;

            switch (funcType)
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
                    arg.Evaluate(context);
                    int count = 0;
                    if (xsltContext != null)
                    {
                        XPathNavigator nav;
                        while ((nav = arg.Advance()) != null)
                        {
                            if (nav.NodeType != XPathNodeType.Whitespace || xsltContext.PreserveWhitespace(nav))
                            {
                                count++;
                            }
                        }
                    }
                    else
                    {
                        while (arg.Advance() != null)
                        {
                            count++;
                        }
                    }
                    return (double)count;
            }
            return string.Empty;
        }

        public override XPathResultType StaticType { get { return Function.ReturnTypes[(int)funcType]; } }

        public override XPathNodeIterator Clone()
        {
            NodeFunctions method = new NodeFunctions(funcType, Clone(arg));
            method.xsltContext = this.xsltContext;
            return method;
        }

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
