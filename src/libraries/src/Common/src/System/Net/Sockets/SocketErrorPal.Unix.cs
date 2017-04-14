// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.Sockets
{
    internal static class SocketErrorPal
    {
#if DEBUG
        static SocketErrorPal()
        {
            Debug.Assert(s_nativeErrorToSocketError.Count == NativeErrorToSocketErrorCount,
                $"Expected s_nativeErrorToSocketError to have {NativeErrorToSocketErrorCount} count instead of {s_nativeErrorToSocketError.Count}.");

            Debug.Assert(s_socketErrorToNativeError.Count == SocketErrorToNativeErrorCount,
                $"Expected s_socketErrorToNativeError to have {SocketErrorToNativeErrorCount} count instead of {s_socketErrorToNativeError.Count}.");
        }
#endif

        private const int NativeErrorToSocketErrorCount = 41;
        private const int SocketErrorToNativeErrorCount = 40;

        // No Interop.Errors are included for the following SocketErrors, as there's no good mapping:
        // - SocketError.NoRecovery
        // - SocketError.NotInitialized
        // - SocketError.ProcessLimit
        // - SocketError.SocketError
        // - SocketError.SystemNotReady
        // - SocketError.TypeNotFound
        // - SocketError.VersionNotSupported

        private static readonly Dictionary<Interop.Error, SocketError> s_nativeErrorToSocketError = new Dictionary<Interop.Error, SocketError>(NativeErrorToSocketErrorCount)
        {
            { Interop.Error.EACCES, SocketError.AccessDenied },
            { Interop.Error.EADDRINUSE, SocketError.AddressAlreadyInUse },
            { Interop.Error.EADDRNOTAVAIL, SocketError.AddressNotAvailable },
            { Interop.Error.EAFNOSUPPORT, SocketError.AddressFamilyNotSupported },
            { Interop.Error.EAGAIN, SocketError.WouldBlock },
            { Interop.Error.EALREADY, SocketError.AlreadyInProgress },
            { Interop.Error.EBADF, SocketError.InvalidArgument },
            { Interop.Error.ECANCELED, SocketError.OperationAborted },
            { Interop.Error.ECONNABORTED, SocketError.ConnectionAborted },
            { Interop.Error.ECONNREFUSED, SocketError.ConnectionRefused },
            { Interop.Error.ECONNRESET, SocketError.ConnectionReset },
            { Interop.Error.EDESTADDRREQ, SocketError.DestinationAddressRequired },
            { Interop.Error.EFAULT, SocketError.Fault },
            { Interop.Error.EHOSTDOWN, SocketError.HostDown },
            { Interop.Error.ENXIO, SocketError.HostNotFound }, // not perfect, but closest match available
            { Interop.Error.EHOSTUNREACH, SocketError.HostUnreachable },
            { Interop.Error.EINPROGRESS, SocketError.InProgress },
            { Interop.Error.EINTR, SocketError.Interrupted },
            { Interop.Error.EINVAL, SocketError.InvalidArgument },
            { Interop.Error.EISCONN, SocketError.IsConnected },
            { Interop.Error.EMFILE, SocketError.TooManyOpenSockets },
            { Interop.Error.EMSGSIZE, SocketError.MessageSize },
            { Interop.Error.ENETDOWN, SocketError.NetworkDown },
            { Interop.Error.ENETRESET, SocketError.NetworkReset },
            { Interop.Error.ENETUNREACH, SocketError.NetworkUnreachable },
            { Interop.Error.ENFILE, SocketError.TooManyOpenSockets },
            { Interop.Error.ENOBUFS, SocketError.NoBufferSpaceAvailable },
            { Interop.Error.ENODATA, SocketError.NoData },
            { Interop.Error.ENOENT, SocketError.AddressNotAvailable },
            { Interop.Error.ENOPROTOOPT, SocketError.ProtocolOption },
            { Interop.Error.ENOTCONN, SocketError.NotConnected },
            { Interop.Error.ENOTSOCK, SocketError.NotSocket },
            { Interop.Error.ENOTSUP, SocketError.OperationNotSupported },
            { Interop.Error.EPIPE, SocketError.Shutdown },
            { Interop.Error.EPFNOSUPPORT, SocketError.ProtocolFamilyNotSupported },
            { Interop.Error.EPROTONOSUPPORT, SocketError.ProtocolNotSupported },
            { Interop.Error.EPROTOTYPE, SocketError.ProtocolType },
            { Interop.Error.ESOCKTNOSUPPORT, SocketError.SocketNotSupported },
            { Interop.Error.ESHUTDOWN, SocketError.Disconnecting },
            { Interop.Error.SUCCESS, SocketError.Success },
            { Interop.Error.ETIMEDOUT, SocketError.TimedOut },
        };

        private static readonly Dictionary<SocketError, Interop.Error> s_socketErrorToNativeError = new Dictionary<SocketError, Interop.Error>(SocketErrorToNativeErrorCount)
        {
            // This is *mostly* an inverse mapping of s_nativeErrorToSocketError.  However, some options have multiple mappings and thus
            // can't be inverted directly.  Other options don't have a mapping from native to SocketError, but when presented with a SocketError,
            // we want to provide the closest relevant Error possible, e.g. EINPROGRESS maps to SocketError.InProgress, and vice versa, but 
            // SocketError.IOPending also maps closest to EINPROGRESS.  As such, roundtripping won't necessarily provide the original value 100% of the time,
            // but it's the best we can do given the mismatch between Interop.Error and SocketError.

            { SocketError.AccessDenied, Interop.Error.EACCES},
            { SocketError.AddressAlreadyInUse, Interop.Error.EADDRINUSE  },
            { SocketError.AddressNotAvailable, Interop.Error.EADDRNOTAVAIL },
            { SocketError.AddressFamilyNotSupported, Interop.Error.EAFNOSUPPORT  },
            { SocketError.AlreadyInProgress, Interop.Error.EALREADY },
            { SocketError.ConnectionAborted, Interop.Error.ECONNABORTED },
            { SocketError.ConnectionRefused, Interop.Error.ECONNREFUSED },
            { SocketError.ConnectionReset, Interop.Error.ECONNRESET },
            { SocketError.DestinationAddressRequired, Interop.Error.EDESTADDRREQ },
            { SocketError.Disconnecting, Interop.Error.ESHUTDOWN },
            { SocketError.Fault, Interop.Error.EFAULT },
            { SocketError.HostDown, Interop.Error.EHOSTDOWN },
            { SocketError.HostNotFound, Interop.Error.ENXIO }, // not perfect, but closest match available
            { SocketError.HostUnreachable, Interop.Error.EHOSTUNREACH },
            { SocketError.InProgress, Interop.Error.EINPROGRESS },
            { SocketError.Interrupted, Interop.Error.EINTR },
            { SocketError.InvalidArgument, Interop.Error.EINVAL }, // could also have been EBADF, though that's logically an invalid argument
            { SocketError.IOPending, Interop.Error.EINPROGRESS },
            { SocketError.IsConnected, Interop.Error.EISCONN },
            { SocketError.MessageSize, Interop.Error.EMSGSIZE },
            { SocketError.NetworkDown, Interop.Error.ENETDOWN },
            { SocketError.NetworkReset, Interop.Error.ENETRESET },
            { SocketError.NetworkUnreachable, Interop.Error.ENETUNREACH },
            { SocketError.NoBufferSpaceAvailable, Interop.Error.ENOBUFS },
            { SocketError.NoData, Interop.Error.ENODATA },
            { SocketError.NotConnected, Interop.Error.ENOTCONN },
            { SocketError.NotSocket, Interop.Error.ENOTSOCK },
            { SocketError.OperationAborted, Interop.Error.ECANCELED },
            { SocketError.OperationNotSupported, Interop.Error.ENOTSUP },
            { SocketError.ProtocolFamilyNotSupported, Interop.Error.EPFNOSUPPORT },
            { SocketError.ProtocolNotSupported, Interop.Error.EPROTONOSUPPORT },
            { SocketError.ProtocolOption, Interop.Error.ENOPROTOOPT },
            { SocketError.ProtocolType, Interop.Error.EPROTOTYPE },
            { SocketError.Shutdown, Interop.Error.EPIPE },
            { SocketError.SocketNotSupported, Interop.Error.ESOCKTNOSUPPORT },
            { SocketError.Success, Interop.Error.SUCCESS },
            { SocketError.TimedOut, Interop.Error.ETIMEDOUT },
            { SocketError.TooManyOpenSockets, Interop.Error.ENFILE }, // could also have been EMFILE
            { SocketError.TryAgain, Interop.Error.EAGAIN }, // not a perfect mapping, but better than nothing
            { SocketError.WouldBlock, Interop.Error.EAGAIN  },
        };

        internal static SocketError GetSocketErrorForNativeError(Interop.Error errno)
        {
            SocketError result;
            return s_nativeErrorToSocketError.TryGetValue(errno, out result) ? 
                result : 
                SocketError.SocketError; // unknown native error, just treat it as a generic SocketError
        }

        internal static Interop.Error GetNativeErrorForSocketError(SocketError error)
        {
            Interop.Error errno;
            if (!TryGetNativeErrorForSocketError(error, out errno))
            {
                // Use the SocketError's value, as it at least retains some useful info
                errno = (Interop.Error)(int)error;
            }
            return errno;
        }

        internal static bool TryGetNativeErrorForSocketError(SocketError error, out Interop.Error errno)
        {
            return s_socketErrorToNativeError.TryGetValue(error, out errno);
        }
    }
}
