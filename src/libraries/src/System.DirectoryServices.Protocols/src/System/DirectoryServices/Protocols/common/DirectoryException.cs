// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    [Serializable]
    public class DirectoryException : Exception
    {
        protected DirectoryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public DirectoryException(string message, Exception inner) : base(message, inner)
        {
            Utility.CheckOSVersion();
        }

        public DirectoryException(string message) : base(message)
        {
            Utility.CheckOSVersion();
        }

        public DirectoryException() : base()
        {
            Utility.CheckOSVersion();
        }
    }

    [Serializable]
    public class DirectoryOperationException : DirectoryException, ISerializable
    {
        internal DirectoryResponse response = null;
        protected DirectoryOperationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public DirectoryOperationException() : base() { }

        public DirectoryOperationException(string message) : base(message) { }

        public DirectoryOperationException(string message, Exception inner) : base(message, inner) { }

        public DirectoryOperationException(DirectoryResponse response) : base(Res.GetString(Res.DefaultOperationsError))
        {
            this.response = response;
        }

        public DirectoryOperationException(DirectoryResponse response, string message) : base(message)
        {
            this.response = response;
        }

        public DirectoryOperationException(DirectoryResponse response, string message, Exception inner) : base(message, inner)
        {
            this.response = response;
        }

        public DirectoryResponse Response
        {
            get
            {
                return response;
            }
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }
    }

    [Serializable]
    public class BerConversionException : DirectoryException
    {
        protected BerConversionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public BerConversionException() : base(Res.GetString(Res.BerConversionError))
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
