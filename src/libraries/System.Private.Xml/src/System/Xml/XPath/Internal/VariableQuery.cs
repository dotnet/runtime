// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal sealed class VariableQuery : ExtensionQuery
    {
        private IXsltContextVariable? _variable;

        public VariableQuery(string name, string prefix) : base(prefix, name) { }
        private VariableQuery(VariableQuery other) : base(other)
        {
            _variable = other._variable;
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
                _variable = xsltContext.ResolveVariable(prefix, name);
                // Since null is allowed for ResolveFunction, allow it for ResolveVariable as well
                if (_variable == null)
                {
                    throw XPathException.Create(SR.Xp_UndefVar, QName);
                }
            }
        }

        public override object Evaluate(XPathNodeIterator nodeIterator)
        {
            if (xsltContext == null)
            {
                throw XPathException.Create(SR.Xp_NoContext);
            }

            Debug.Assert(_variable != null);
            object? retVal = ProcessResult(_variable.Evaluate(xsltContext));

            // ProcessResult may return null when the input value is XmlNode and here doesn't seem to be the case.
            Debug.Assert(retVal != null);
            return retVal;
        }

        public override XPathResultType StaticType
        {
            get
            {
                if (_variable != null)
                {  // Temp. fix to overcome dependency on static type
                    return GetXPathType(Evaluate(null!));
                }
                XPathResultType result = _variable != null ? _variable.VariableType : XPathResultType.Any;
                if (result == XPathResultType.Error)
                {
                    // In v.1 we confused Error & Any so now for backward compatibility we should allow users to return any of them.
                    result = XPathResultType.Any;
                }
                return result;
            }
        }

        public override XPathNodeIterator Clone() { return new VariableQuery(this); }
    }
}
