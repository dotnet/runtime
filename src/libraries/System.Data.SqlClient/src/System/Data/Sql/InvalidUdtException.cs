// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Data.Common;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Microsoft.SqlServer.Server
{
    [Serializable]
    public sealed class InvalidUdtException : SystemException
    {
        private const int InvalidUdtHResult = unchecked((int)0x80131937);

        internal InvalidUdtException() : base()
        {
            HResult = InvalidUdtHResult;
        }

        internal InvalidUdtException(string message) : base(message)
        {
            HResult = InvalidUdtHResult;
        }

        internal InvalidUdtException(string message, Exception innerException) : base(message, innerException)
        {
            HResult = InvalidUdtHResult;
        }

        private InvalidUdtException(SerializationInfo si, StreamingContext sc) : base(si, sc)
        {
        }

        public override void GetObjectData(SerializationInfo si, StreamingContext context)
        {
            base.GetObjectData(si, context);
        }

        internal static InvalidUdtException Create(Type udtType, string resourceReason)
        {
            string reason = SR.GetString(resourceReason);
            string message = SR.GetString(SR.SqlUdt_InvalidUdtMessage, udtType.FullName, reason);
            InvalidUdtException e = new InvalidUdtException(message);
            ADP.TraceExceptionAsReturnValue(e);
            return e;
        }
    }
}
