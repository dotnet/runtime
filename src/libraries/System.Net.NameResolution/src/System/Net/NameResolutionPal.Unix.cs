// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
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

        private static unsafe IPHostEntry CreateHostEntry(Interop.libc.hostent* hostent)
        {
            string hostName = null;
            if (hostent->h_name != null)
            {
                hostName = Marshal.PtrToStringAnsi((IntPtr)hostent->h_name);
            }

            int numAddresses;
            for (numAddresses = 0; hostent->h_addr_list[numAddresses] != null; numAddresses++)
            {
            }

            IPAddress[] ipAddresses;
            if (numAddresses == 0)
            {
                ipAddresses = Array.Empty<IPAddress>();
            }
            else
            {
                ipAddresses = new IPAddress[numAddresses];
                for (int i = 0; i < numAddresses; i++)
                {
                    Debug.Assert(hostent->h_addr_list[i] != null);
                    ipAddresses[i] = new IPAddress(*(int*)hostent->h_addr_list[i]);
                }
            }

            int numAliases;
            for (numAliases = 0; hostent->h_aliases[numAliases] != null; numAliases++)
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
                    Debug.Assert(hostent->h_aliases[i] != null);
                    aliases[i] = Marshal.PtrToStringAnsi((IntPtr)hostent->h_aliases[i]);
                }
            }

            return new IPHostEntry
            {
                HostName = hostName,
                AddressList = ipAddresses,
                Aliases = aliases
            };
        }

        public static unsafe SocketError TryGetAddrInfo(string name, out IPHostEntry hostinfo, out int nativeErrorCode)
        {
            Interop.Sys.HostEntry entry;
            int result = Interop.Sys.GetHostEntryForName(name, &entry);
            if (result != 0)
            {
                hostinfo = NameResolutionUtilities.GetUnresolvedAnswer(name);
                nativeErrorCode = result;
                return GetSocketErrorForNativeError(result);
            }

            try
            {
                string canonicalName = Marshal.PtrToStringAnsi((IntPtr)entry.CanonicalName);

                hostinfo = new IPHostEntry
                {
                    HostName = string.IsNullOrEmpty(canonicalName) ? name : canonicalName,
                    Aliases = Array.Empty<string>(),
                    AddressList = new IPAddress[entry.IPAddressCount]
                };

                void* addressListHandle = entry.AddressListHandle;
                var nativeIPAddress = default(Interop.Sys.IPAddress);
                for (int i = 0; i < entry.IPAddressCount; i++)
                {
                    int err = Interop.Sys.GetNextIPAddress(&addressListHandle, &nativeIPAddress);
                    Debug.Assert(err == 0);

                    IPAddress ipAddress;
                    if (nativeIPAddress.IsIPv6 == 0)
                    {
                        uint address = *(uint*)nativeIPAddress.Address;
                        ipAddress = new IPAddress((long)address);
                    }
                    else
                    {
                        byte[] address = new byte[Interop.Sys.NUM_BYTES_IN_IPV6_ADDRESS];
                        for (int b = 0; b < Interop.Sys.NUM_BYTES_IN_IPV6_ADDRESS; b++)
                        {
                            address[b] = nativeIPAddress.Address[b];
                        }

                        ipAddress = new IPAddress(address, (long)nativeIPAddress.ScopeId);
                    }

                    hostinfo.AddressList[i] = ipAddress;
                }
            }
            finally
            {
                Interop.Sys.FreeHostEntry(&entry);
            }

            nativeErrorCode = 0;
            return SocketError.Success;
        }

        public static unsafe string TryGetNameInfo(IPAddress addr, out SocketError socketError, out int nativeErrorCode)
        {
            byte* buffer = stackalloc byte[Interop.Sys.NI_MAXHOST + 1 /*for null*/];

            // TODO #2891: Remove the copying step to improve performance. This requires a change in the contracts.
            byte[] addressBuffer = addr.GetAddressBytes();

            int error;
            fixed (byte* rawAddress = addressBuffer)
            {
                error = Interop.Sys.GetNameInfo(
                    rawAddress,
                    unchecked((uint)addressBuffer.Length),
                    addr.AddressFamily == AddressFamily.InterNetworkV6,
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
