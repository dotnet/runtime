// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

// this file is only placeholder for supporting Xslt
// if we add Xslt remove this file and add appropriate reference assembly
namespace System.Xml.Xsl
{
    internal interface IXsltContextFunction
    {
        int Minargs { get; }
        int Maxargs { get; }
        XPathResultType ReturnType { get; }
        XPathResultType[] ArgTypes { get; }
        object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext);
    }

    internal interface IXsltContextVariable
    {
        bool IsLocal { get; }
        bool IsParam { get; }
        XPathResultType VariableType { get; }
        object Evaluate(XsltContext xsltContext);
    }

    internal abstract class XsltContext : XmlNamespaceManager
    {
        protected XsltContext(NameTable table) : base(table) { }
        protected XsltContext() : base(new NameTable()) { }
        public abstract IXsltContextVariable ResolveVariable(string prefix, string name);
        public abstract IXsltContextFunction ResolveFunction(string prefix, string name, XPathResultType[] ArgTypes);
        public abstract bool Whitespace { get; }
        public abstract bool PreserveWhitespace(XPathNavigator node);
        public abstract int CompareDocument(string baseUri, string nextbaseUri);
    }
}
