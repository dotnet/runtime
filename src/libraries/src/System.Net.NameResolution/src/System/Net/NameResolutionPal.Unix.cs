// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                ipAddresses = new IPAddress[numAddresses];

                void* addressListHandle = hostEntry.AddressListHandle;
                var nativeIPAddress = default(Interop.Sys.IPAddress);
                for (int i = 0; i < numAddresses; i++)
                {
                    int err = Interop.Sys.GetNextIPAddress(&hostEntry, &addressListHandle, &nativeIPAddress);
                    Debug.Assert(err == 0);

                    ipAddresses[i] = nativeIPAddress.GetIPAddress();
                }
            }

            int numAliases;
            for (numAliases = 0; hostEntry.Aliases[numAliases] != null; numAliases++)
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
            Interop.Sys.HostEntry entry;
            int err = Interop.Sys.GetHostByName(hostName, &entry);
            if (err != 0)
            {
                throw new InternalSocketException(GetSocketErrorForErrno(err), err);
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
                throw new InternalSocketException(GetSocketErrorForErrno(err), err);
            }

            return CreateIPHostEntry(entry);
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
                    int err = Interop.Sys.GetNextIPAddress(&entry, &addressListHandle, &nativeIPAddress);
                    Debug.Assert(err == 0);

                    hostinfo.AddressList[i] = nativeIPAddress.GetIPAddress();
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
