// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.DirectoryServices.Protocols
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.DirectoryServices.Protocols, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DirectoryException : Exception
    {
        protected DirectoryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public DirectoryException(string message, Exception inner) : base(message, inner)
        {
        }

        public DirectoryException(string message) : base(message)
        {
        }

        public DirectoryException() : base()
        {
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.DirectoryServices.Protocols, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DirectoryOperationException : DirectoryException, ISerializable
    {
        protected DirectoryOperationException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public DirectoryOperationException() : base() { }

        public DirectoryOperationException(string message) : base(message) { }

        public DirectoryOperationException(string message, Exception inner) : base(message, inner) { }

        public DirectoryOperationException(DirectoryResponse response) : base(SR.DefaultOperationsError)
        {
            Response = response;
        }

        public DirectoryOperationException(DirectoryResponse response, string message) : base(message)
        {
            Response = response;
        }

        public DirectoryOperationException(DirectoryResponse response, string message, Exception inner) : base(message, inner)
        {
            Response = response;
        }

        public DirectoryResponse Response { get; internal set; }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.DirectoryServices.Protocols, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BerConversionException : DirectoryException
    {
        protected BerConversionException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public BerConversionException() : base(SR.BerConversionError)
        {
        }

        public BerConversionException(string message) : base(message)
        {
        }

        public BerConversionException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
