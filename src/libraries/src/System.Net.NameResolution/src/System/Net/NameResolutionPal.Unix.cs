// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Internals;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Net
{
    internal static partial class NameResolutionPal
    {
        private static SocketError GetSocketErrorForErrno(int errno)
        {
            switch (errno)
            {
                case 0:
                    return SocketError.Success;
                case (int)Interop.Sys.GetHostErrorCodes.HOST_NOT_FOUND:
                    return SocketError.HostNotFound;
                case (int)Interop.Sys.GetHostErrorCodes.NO_DATA:
                    return SocketError.NoData;
                case (int)Interop.Sys.GetHostErrorCodes.NO_RECOVERY:
                    return SocketError.NoRecovery;
                case (int)Interop.Sys.GetHostErrorCodes.TRY_AGAIN:
                    return SocketError.TryAgain;
                default:
                    Debug.Fail("Unexpected errno: " + errno.ToString());
                    return SocketError.SocketError;
            }
        }

        private static SocketError GetSocketErrorForNativeError(int error)
        {
            switch (error)
            {
                case 0:
                    return SocketError.Success;
                case (int)Interop.Sys.GetAddrInfoErrorFlags.EAI_AGAIN:
                    return SocketError.TryAgain;
                case (int)Interop.Sys.GetAddrInfoErrorFlags.EAI_BADFLAGS:
                case (int)Interop.Sys.GetAddrInfoErrorFlags.EAI_BADARG:
                    return SocketError.InvalidArgument;
                case (int)Interop.Sys.GetAddrInfoErrorFlags.EAI_FAIL:
                    return SocketError.NoRecovery;
                case (int)Interop.Sys.GetAddrInfoErrorFlags.EAI_FAMILY:
                    return SocketError.AddressFamilyNotSupported;
                case (int)Interop.Sys.GetAddrInfoErrorFlags.EAI_NONAME:
                    return SocketError.HostNotFound;
                default:
                    Debug.Fail("Unexpected error: " + error.ToString());
                    return SocketError.SocketError;
            }
        }

        private static unsafe IPHostEntry CreateIPHostEntry(Interop.Sys.HostEntry hostEntry)
        {
            string hostName = null;
            if (hostEntry.CanonicalName != null)
            {
                hostName = Marshal.PtrToStringAnsi((IntPtr)hostEntry.CanonicalName);
            }

            int numAddresses = hostEntry.IPAddressCount;

            IPAddress[] ipAddresses;
            if (numAddresses == 0)
            {
                ipAddresses = Array.Empty<IPAddress>();
            }
            else
            {
                //
                // getaddrinfo returns multiple entries per address, for each socket type (datagram, stream, etc.).
                // Our callers expect just one entry for each address.  So we need to deduplicate the results.
                // It's important to keep the addresses in order, since they are returned in the order in which
                // connections should be attempted.
                // 
                // We assume that the list returned by getaddrinfo is relatively short; after all, the intent is that
                // the caller may need to attempt to contact every address in the list before giving up on a connection
                // attempt.  So an O(N^2) algorithm should be fine here.  Keep in mind that any "better" algorithm 
                // is likely to involve extra allocations, hashing, etc., and so will probably be more expensive than
                // this one in the typical (short list) case.
                //
                var nativeAddresses = new Interop.Sys.IPAddress[hostEntry.IPAddressCount];
                var nativeAddressCount = 0;

                var addressListHandle = hostEntry.AddressListHandle;
                for (int i = 0; i < hostEntry.IPAddressCount; i++)
                {
                    var nativeIPAddress = default(Interop.Sys.IPAddress);
                    int err = Interop.Sys.GetNextIPAddress(&hostEntry, &addressListHandle, &nativeIPAddress);
                    Debug.Assert(err == 0);

                    if (Array.IndexOf(nativeAddresses, nativeIPAddress, 0, nativeAddressCount) == -1)
                    {
                        nativeAddresses[nativeAddressCount] = nativeIPAddress;
                        nativeAddressCount++;
                    }
                }

                ipAddresses = new IPAddress[nativeAddressCount];
                for (int i = 0; i < nativeAddressCount; i++)
                {
                    ipAddresses[i] = nativeAddresses[i].GetIPAddress();
                }
            }

            int numAliases;
            for (numAliases = 0; hostEntry.Aliases != null && hostEntry.Aliases[numAliases] != null; numAliases++)
            {
            }

            string[] aliases;
            if (numAliases == 0)
            {
                aliases = Array.Empty<string>();
            }
            else
            {
                aliases = new string[numAliases];
                for (int i = 0; i < numAliases; i++)
                {
                    Debug.Assert(hostEntry.Aliases[i] != null);
                    aliases[i] = Marshal.PtrToStringAnsi((IntPtr)hostEntry.Aliases[i]);
                }
            }

            Interop.Sys.FreeHostEntry(&hostEntry);

            return new IPHostEntry
            {
                HostName = hostName,
                AddressList = ipAddresses,
                Aliases = aliases
            };
        }

        public static unsafe IPHostEntry GetHostByName(string hostName)
        {
            if (hostName == "")
            {
                // To match documented behavior on Windows, if an empty string is passed in, use the local host's name.
                hostName = Dns.GetHostName();
            }

            Interop.Sys.HostEntry entry;
            int err = Interop.Sys.GetHostByName(hostName, &entry);
            if (err != 0)
            {
                throw SocketExceptionFactory.CreateSocketException(GetSocketErrorForErrno(err), err);
            }

            return CreateIPHostEntry(entry);
        }

        public static unsafe IPHostEntry GetHostByAddr(IPAddress addr)
        {
            // TODO #2891: Optimize this (or decide if this legacy code can be removed):
            Interop.Sys.IPAddress address = addr.GetNativeIPAddress();
            Interop.Sys.HostEntry entry;
            int err = Interop.Sys.GetHostByAddress(&address, &entry);
            if (err != 0)
            {
                throw SocketExceptionFactory.CreateSocketException(GetSocketErrorForErrno(err), err);
            }

            return CreateIPHostEntry(entry);
        }

        public static unsafe SocketError TryGetAddrInfo(string name, out IPHostEntry hostinfo, out int nativeErrorCode)
        {
            if (name == "")
            {
                // To match documented behavior on Windows, if an empty string is passed in, use the local host's name.
                name = Dns.GetHostName();
            }

            Interop.Sys.HostEntry entry;
            int result = Interop.Sys.GetHostEntryForName(name, &entry);
            if (result != 0)
            {
                hostinfo = NameResolutionUtilities.GetUnresolvedAnswer(name);
                nativeErrorCode = result;
                return GetSocketErrorForNativeError(result);
            }

            hostinfo = CreateIPHostEntry(entry);

            nativeErrorCode = 0;
            return SocketError.Success;
        }

        public static unsafe string TryGetNameInfo(IPAddress addr, out SocketError socketError, out int nativeErrorCode)
        {
            byte* buffer = stackalloc byte[Interop.Sys.NI_MAXHOST + 1 /*for null*/];

            // TODO #2891: Remove the copying step to improve performance. This requires a change in the contracts.
            byte[] addressBuffer = addr.GetAddressBytes();

            int error;
            fixed (byte* rawAddress = &addressBuffer[0])
            {
                error = Interop.Sys.GetNameInfo(
                    rawAddress,
                    unchecked((uint)addressBuffer.Length),
                    addr.AddressFamily == AddressFamily.InterNetworkV6 ? (byte)1 : (byte)0,
                    buffer,
                    Interop.Sys.NI_MAXHOST,
                    null,
                    0,
                    Interop.Sys.GetNameInfoFlags.NI_NAMEREQD);
            }

            socketError = GetSocketErrorForNativeError(error);
            nativeErrorCode = error;
           return socketError != SocketError.Success ? null : Marshal.PtrToStringAnsi((IntPtr)buffer);
        }

        public static string GetHostName()
        {
            return Interop.Sys.GetHostName();
        }

        public static void EnsureSocketsAreInitialized()
        {
            // No-op for Unix.
        }
    }
}
