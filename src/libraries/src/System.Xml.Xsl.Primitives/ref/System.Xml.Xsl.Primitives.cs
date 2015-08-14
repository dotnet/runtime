// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Xml.Xsl
{
    public partial interface IXsltContextFunction
    {
        System.Xml.XPath.XPathResultType[] ArgTypes { get; }
        int Maxargs { get; }
        int Minargs { get; }
        System.Xml.XPath.XPathResultType ReturnType { get; }
        object Invoke(System.Xml.Xsl.XsltContext xsltContext, object[] args, System.Xml.XPath.XPathNavigator docContext);
    }
    public partial interface IXsltContextVariable
    {
        bool IsLocal { get; }
        bool IsParam { get; }
        System.Xml.XPath.XPathResultType VariableType { get; }
        object Evaluate(System.Xml.Xsl.XsltContext xsltContext);
    }
    public abstract partial class XsltContext : System.Xml.XmlNamespaceManager
    {
        protected XsltContext() : base(default(System.Xml.XmlNameTable)) { }
        protected XsltContext(System.Xml.NameTable table) : base(default(System.Xml.XmlNameTable)) { }
        public abstract bool Whitespace { get; }
        public abstract int CompareDocument(string baseUri, string nextbaseUri);
        public abstract bool PreserveWhitespace(System.Xml.XPath.XPathNavigator node);
        public abstract System.Xml.Xsl.IXsltContextFunction ResolveFunction(string prefix, string name, System.Xml.XPath.XPathResultType[] ArgTypes);
        public abstract System.Xml.Xsl.IXsltContextVariable ResolveVariable(string prefix, string name);
    }
}
