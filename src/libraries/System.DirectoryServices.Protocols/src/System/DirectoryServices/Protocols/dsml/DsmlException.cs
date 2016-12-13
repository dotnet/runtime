// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    [Serializable]
    public class DsmlInvalidDocumentException : DirectoryException
    {
        public DsmlInvalidDocumentException() : base(Res.GetString(Res.InvalidDocument)) { }
        public DsmlInvalidDocumentException(string message) : base(message) { }
        public DsmlInvalidDocumentException(string message, Exception inner) : base(message, inner) { }
        protected DsmlInvalidDocumentException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ErrorResponseException : DirectoryException, ISerializable
    {
        private DsmlErrorResponse _errorResponse = null;
        protected ErrorResponseException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public ErrorResponseException() : base() { }

        public ErrorResponseException(string message) : base(message) { }

        public ErrorResponseException(string message, Exception inner) : base(message, inner) { }

        public ErrorResponseException(DsmlErrorResponse response) : this(response, Res.GetString(Res.ErrorResponse), null) { }

        public ErrorResponseException(DsmlErrorResponse response, string message) : this(response, message, null) { }

        public ErrorResponseException(DsmlErrorResponse response, string message, Exception inner) : base(message, inner)
        {
            _errorResponse = response;
        }

        public DsmlErrorResponse Response
        {
            get
            {
                return _errorResponse;
            }
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }
    }
}
