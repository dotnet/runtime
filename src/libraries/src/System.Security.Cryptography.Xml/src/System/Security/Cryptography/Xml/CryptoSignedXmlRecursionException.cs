// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    internal class CryptoSignedXmlRecursionException : XmlException
    {
        public CryptoSignedXmlRecursionException() : base() { }
        public CryptoSignedXmlRecursionException(string message) : base(message) { }
        public CryptoSignedXmlRecursionException(string message, System.Exception inner) : base(message, inner) { }
        // A constructor is needed for serialization when an 
        // exception propagates from a remoting server to the client.  
        protected CryptoSignedXmlRecursionException(System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context)
        { }
    }
}
