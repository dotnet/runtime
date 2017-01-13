// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.IO.Compression
{
    [Serializable]
    internal partial class ZLibException : IOException, ISerializable
    {
        /// <summary>
        /// Initializes a new ZLibException with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected ZLibException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _zlibErrorContext = info.GetString("zlibErrorContext");
            _zlibErrorCode = (ZLibNative.ErrorCode)info.GetInt32("zlibErrorCode");
            _zlibErrorMessage = info.GetString("zlibErrorMessage");
        }

        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
        {
            base.GetObjectData(si, context);
            si.AddValue("zlibErrorContext", _zlibErrorContext);
            si.AddValue("zlibErrorCode", (int)_zlibErrorCode);
            si.AddValue("zlibErrorMessage", _zlibErrorMessage);
        }
    }
}
