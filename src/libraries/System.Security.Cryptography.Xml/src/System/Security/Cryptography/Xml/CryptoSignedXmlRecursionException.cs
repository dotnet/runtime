// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    /// <summary>
    /// This exception helps catch the signed XML recursion limit error.
    /// This is being caught in the SignedXml class while computing the
    /// hash. ComputeHash can throw different kind of exceptions.
    /// This unique exception helps catch the recursion limit issue.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Security, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class CryptoSignedXmlRecursionException : XmlException
    {
        public CryptoSignedXmlRecursionException() : base() { }
        public CryptoSignedXmlRecursionException(string message) : base(message) { }
        public CryptoSignedXmlRecursionException(string message, Exception inner) : base(message, inner) { }
        protected CryptoSignedXmlRecursionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
