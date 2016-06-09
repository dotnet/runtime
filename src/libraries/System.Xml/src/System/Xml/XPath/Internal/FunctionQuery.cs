// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal.Xml.XPath
{
    using System;
    using System.Xml;
    using System.Xml.Xsl;
    using System.Xml.XPath;
    using System.Diagnostics;
    using System.Globalization;
    using System.Collections.Generic;

    internal sealed class FunctionQuery : ExtensionQuery
    {
        private IList<Query> _args;
        private IXsltContextFunction _function;

        public FunctionQuery(string prefix, string name, List<Query> args) : base(prefix, name)
        {
            _args = args;
        }
        private FunctionQuery(FunctionQuery other) : base(other)
        {
            _function = other._function;
            Query[] tmp = new Query[other._args.Count];
            {
                for (int i = 0; i < tmp.Length; i++)
                {
                    tmp[i] = Clone(other._args[i]);
                }
                _args = tmp;
            }
            _args = tmp;
        }

        public override void SetXsltContext(XsltContext context)
        {
            if (context == null)
            {
                throw XPathException.Create(Res.Xp_NoContext);
            }
            if (this.xsltContext != context)
            {
                xsltContext = context;
                foreach (Query argument in _args)
                {
                    argument.SetXsltContext(context);
                }
                XPathResultType[] argTypes = new XPathResultType[_args.Count];
                for (int i = 0; i < _args.Count; i++)
                {
                    argTypes[i] = _args[i].StaticType;
                }
                _function = xsltContext.ResolveFunction(prefix, name, argTypes);
                // KB article allows to return null, see http://support.microsoft.com/?kbid=324462#6
                if (_function == null)
                {
                    throw XPathException.Create(Res.Xp_UndefFunc, QName);
                }
            }
        }

        public override object Evaluate(XPathNodeIterator nodeIterator)
        {
            if (xsltContext == null)
            {
                throw XPathException.Create(Res.Xp_NoContext);
            }

            // calculate arguments:
            object[] argVals = new object[_args.Count];
            for (int i = 0; i < _args.Count; i++)
            {
                argVals[i] = _args[i].Evaluate(nodeIterator);
                if (argVals[i] is XPathNodeIterator)
                {// ForBack Compat. To protect our queries from users. bug#372077 & 20006123 
                    argVals[i] = new XPathSelectionIterator(nodeIterator.Current, _args[i]);
                }
            }
            try
            {
                return ProcessResult(_function.Invoke(xsltContext, argVals, nodeIterator.Current));
            }
            catch (Exception ex)
            {
                throw XPathException.Create(Res.Xp_FunctionFailed, QName, ex);
            }
        }

        public override XPathNavigator MatchNode(XPathNavigator navigator)
        {
            if (name != "key" && prefix.Length != 0)
            {
                throw XPathException.Create(Res.Xp_InvalidPattern);
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
                XPathResultType result = _function != null ? _function.ReturnType : XPathResultType.Any;
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
            foreach (Query arg in _args)
            {
                arg.PrintQuery(w);
            }
            w.WriteEndElement();
        }
    }
}
