// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal sealed class VariableQuery : ExtensionQuery
    {
        private IXsltContextVariable variable;

        public VariableQuery(string name, string prefix) : base(prefix, name) { }
        private VariableQuery(VariableQuery other) : base(other)
        {
            this.variable = other.variable;
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
                variable = xsltContext.ResolveVariable(prefix, name);
                // Since null is allowed for ResolveFunction, allow it for ResolveVariable as well
                if (variable == null)
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

            return ProcessResult(variable.Evaluate(xsltContext));
        }

        public override XPathResultType StaticType
        {
            get
            {
                if (variable != null)
                {  // Temp. fix to overcome dependency on static type
                    return GetXPathType(Evaluate(null));
                }
                XPathResultType result = variable != null ? variable.VariableType : XPathResultType.Any;
                if (result == XPathResultType.Error)
                {
                    // In v.1 we confused Error & Any so now for backward compatibility we should allow users to return any of them.
                    result = XPathResultType.Any;
                }
                return result;
            }
        }

        public override XPathNodeIterator Clone() { return new VariableQuery(this); }

        public override void PrintQuery(XmlWriter w)
        {
            w.WriteStartElement(this.GetType().Name);
            w.WriteAttributeString("name", prefix.Length != 0 ? prefix + ':' + name : name);
            w.WriteEndElement();
        }
    }
}
