// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    /// <summary>Defines a set of error codes for use with <see cref='System.Net.NetworkException'/>.</summary>
    public enum NetworkError : int
    {
        /// <summary>A network error has occurred.</summary>
        /// <remarks>
        /// This value indicates a non-generic, implementation-specific error.
        /// Details could be obtained from <see cref="NetworkException"/>'s inner exception.
        /// </remarks>
        Other = 0,

        /// <summary>The requested EndPoint is already in use.</summary>
        EndPointInUse,

        /// <summary>No such host is known.</summary>
        HostNotFound,

        /// <summary>The connection attempt has timed out.</summary>
        TimedOut,

        /// <summary>No connection could be made because the remote host actively refused it.</summary>
        ConnectionRefused,

        /// <summary>The operation was aborted by the user.</summary>
        OperationAborted,

        /// <summary>The connection was aborted by the local host.</summary>
        ConnectionAborted,

        /// <summary>The connection was forcibly closed by the remote host.</summary>
        ConnectionReset,
    }
}
