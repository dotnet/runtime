// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal sealed class FunctionQuery : ExtensionQuery
    {
        private IList<Query> args;
        private IXsltContextFunction function;

        public FunctionQuery(string prefix, string name, List<Query> args) : base(prefix, name)
        {
            this.args = args;
        }
        private FunctionQuery(FunctionQuery other) : base(other)
        {
            this.function = other.function;
            Query[] tmp = new Query[other.args.Count];
            {
                for (int i = 0; i < tmp.Length; i++)
                {
                    tmp[i] = Clone(other.args[i]);
                }
                args = tmp;
            }
            this.args = tmp;
        }

        public override void SetXsltContext(XsltContext context)
        {
            if (context == null)
            {
                throw XPathException.Create(SR.Xp_NoContext);
            }
            if (this.xsltContext != context)
            {
                xsltContext = context;
                foreach (Query argument in args)
                {
                    argument.SetXsltContext(context);
                }
                XPathResultType[] argTypes = new XPathResultType[args.Count];
                for (int i = 0; i < args.Count; i++)
                {
                    argTypes[i] = args[i].StaticType;
                }
                function = xsltContext.ResolveFunction(prefix, name, argTypes);
                // KB article allows to return null, see http://support.microsoft.com/?kbid=324462#6
                if (function == null)
                {
                    throw XPathException.Create(SR.Xp_UndefFunc, QName);
                }
            }
        }

        public override object Evaluate(XPathNodeIterator nodeIterator)
        {
            if (xsltContext == null)
            {
                throw XPathException.Create(SR.Xp_NoContext);
            }

            // calculate arguments:
            object[] argVals = new object[args.Count];
            for (int i = 0; i < args.Count; i++)
            {
                argVals[i] = args[i].Evaluate(nodeIterator);
                if (argVals[i] is XPathNodeIterator)
                {// ForBack Compat. To protect our queries from users.
                    argVals[i] = new XPathSelectionIterator(nodeIterator.Current, args[i]);
                }
            }
            try
            {
                return ProcessResult(function.Invoke(xsltContext, argVals, nodeIterator.Current));
            }
            catch (Exception ex)
            {
                throw XPathException.Create(SR.Xp_FunctionFailed, QName, ex);
            }
        }

        public override XPathNavigator MatchNode(XPathNavigator navigator)
        {
            if (name != "key" && prefix.Length != 0)
            {
                throw XPathException.Create(SR.Xp_InvalidPattern);
            }
            this.Evaluate(new XPathSingletonIterator(navigator, /*moved:*/true));
            XPathNavigator nav = null;
            while ((nav = this.Advance()) != null)
            {
                if (nav.IsSamePosition(navigator))
                {
                    return nav;
                }
            }
            return nav;
        }

        public override XPathResultType StaticType
        {
            get
            {
                XPathResultType result = function != null ? function.ReturnType : XPathResultType.Any;
                if (result == XPathResultType.Error)
                {
                    // In v.1 we confused Error & Any so now for backward compatibility we should allow users to return any of them.
                    result = XPathResultType.Any;
                }
                return result;
            }
        }

        public override XPathNodeIterator Clone() { return new FunctionQuery(this); }

        public override void PrintQuery(XmlWriter w)
        {
            w.WriteStartElement(this.GetType().Name);
            w.WriteAttributeString("name", prefix.Length != 0 ? prefix + ':' + name : name);
            foreach (Query arg in this.args)
            {
                arg.PrintQuery(w);
            }
            w.WriteEndElement();
        }
    }
}
