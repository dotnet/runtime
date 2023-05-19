// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Xml.XPath
{
    public partial class XPathDocument : System.Xml.XPath.IXPathNavigable
    {
        public XPathDocument(System.IO.Stream stream) { }
        public XPathDocument(System.IO.TextReader textReader) { }
        public XPathDocument([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string uri) { }
        public XPathDocument([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string uri, System.Xml.XmlSpace space) { }
        public XPathDocument(System.Xml.XmlReader reader) { }
        public XPathDocument(System.Xml.XmlReader reader, System.Xml.XmlSpace space) { }
        public System.Xml.XPath.XPathNavigator CreateNavigator() { throw null; }
    }
    public partial class XPathException : System.SystemException
    {
        public XPathException() { }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected XPathException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XPathException(string? message) { }
        public XPathException(string? message, System.Exception? innerException) { }
        public override string Message { get { throw null; } }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
}
