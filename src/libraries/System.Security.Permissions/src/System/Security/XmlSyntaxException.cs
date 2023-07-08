// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Security
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed partial class XmlSyntaxException : SystemException
    {
        public XmlSyntaxException() { }
        public XmlSyntaxException(int lineNumber) { }
        public XmlSyntaxException(int lineNumber, string message) { }
        public XmlSyntaxException(string message) { }
        public XmlSyntaxException(string message, Exception inner) { }
#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
        private XmlSyntaxException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
