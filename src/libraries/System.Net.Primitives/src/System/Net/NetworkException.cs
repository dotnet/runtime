// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Serialization;

namespace System.Net
{
    /// <summary>Provides socket exceptions to the application.</summary>
    [Serializable]
    public class NetworkException : IOException
    {
        /// <summary>Creates a new instance of the <see cref='System.Net.NetworkException'/> class with the specified error code.</summary>
        protected NetworkException(NetworkError error, Exception? innerException = null, string? message = null)
            : base(message ?? GetExceptionMessage(error), innerException)
        {
            NetworkError = error;
        }

        protected NetworkException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
            NetworkError = (NetworkError)serializationInfo.GetInt32("NetworkError");
        }

        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
            serializationInfo.AddValue("NetworkError", (int)NetworkError);
        }

        /// <summary>Returns the specific kind of error.</summary>
        public NetworkError NetworkError { get; }

        /// <summary>Creates a new instance of the <see cref='System.Net.NetworkException'/> class with the specified error code.</summary>
        public static NetworkException Create(NetworkError error, Exception? innerException = null, string? message = null)
        {
            return new NetworkException(error, innerException, message);
        }

        private static string GetExceptionMessage(NetworkError error) => error switch
        {
            NetworkError.AddressInUse => SR.networkerror_addressinuse,
            NetworkError.InvalidAddress => SR.networkerror_invalidaddress,
            NetworkError.HostNotFound => SR.networkerror_hostnotfound,
            NetworkError.ConnectionRefused => SR.networkerror_connectionrefused,
            NetworkError.ConnectionAborted => SR.networkerror_connectionaborted,
            NetworkError.ConnectionReset => SR.networkerror_connectionreset,
            _ => SR.networkerror_unknown
        };
    }
}
