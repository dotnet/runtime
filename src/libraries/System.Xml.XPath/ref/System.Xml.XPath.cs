// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Xml.XmlNodeOrder))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Xml.XPath.IXPathNavigable))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Xml.XPath.XmlSortOrder))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Xml.XPath.XmlCaseOrder))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Xml.XPath.XmlDataType))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Xml.XPath.XPathResultType))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Xml.XPath.XPathExpression))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Xml.XPath.XPathItem))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Xml.XPath.XPathNamespaceScope))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Xml.XPath.XPathNavigator))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Xml.XPath.XPathNodeIterator))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Xml.XPath.XPathNodeType))]

namespace System.Xml.XPath
{
    public partial class XPathDocument : System.Xml.XPath.IXPathNavigable
    {
        public XPathDocument(System.IO.Stream stream) { }
        public XPathDocument(System.IO.TextReader textReader) { }
        public XPathDocument(string uri) { }
        public XPathDocument(string uri, System.Xml.XmlSpace space) { }
        public XPathDocument(System.Xml.XmlReader reader) { }
        public XPathDocument(System.Xml.XmlReader reader, System.Xml.XmlSpace space) { }
        public System.Xml.XPath.XPathNavigator CreateNavigator() { throw null; }
    }
    public partial class XPathException : System.SystemException
    {
        public XPathException() { }
        protected XPathException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XPathException(string message) { }
        public XPathException(string message, System.Exception innerException) { }
        public override string Message { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
}
