// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal sealed class FunctionQuery : ExtensionQuery
    {
        private readonly IList<Query> _args;
        private IXsltContextFunction? _function;

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
                throw XPathException.Create(SR.Xp_NoContext);
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
            object[] argVals = new object[_args.Count];
            for (int i = 0; i < _args.Count; i++)
            {
                argVals[i] = _args[i].Evaluate(nodeIterator);
                if (argVals[i] is XPathNodeIterator)
                {// ForBack Compat. To protect our queries from users.
                    Debug.Assert(nodeIterator.Current != null);
                    argVals[i] = new XPathSelectionIterator(nodeIterator.Current, _args[i]);
                }
            }
            try
            {
                Debug.Assert(_function != null);
                object? retVal = ProcessResult(_function.Invoke(xsltContext, argVals, nodeIterator.Current));

                // ProcessResult may return null when the input value is XmlNode and here doesn't seem to be the case.
                Debug.Assert(retVal != null);
                return retVal;
            }
            catch (Exception ex)
            {
                throw XPathException.Create(SR.Xp_FunctionFailed, QName, ex);
            }
        }

        public override XPathNavigator? MatchNode(XPathNavigator? navigator)
        {
            if (name != "key" && prefix.Length != 0)
            {
                throw XPathException.Create(SR.Xp_InvalidPattern);
            }
            this.Evaluate(new XPathSingletonIterator(navigator!, /*moved:*/true));
            XPathNavigator? nav = null;
            while ((nav = this.Advance()) != null)
            {
                if (nav.IsSamePosition(navigator!))
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
    }
}
