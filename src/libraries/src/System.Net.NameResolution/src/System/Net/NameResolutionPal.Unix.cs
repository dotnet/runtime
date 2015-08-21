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
                case Interop.libc.HOST_NOT_FOUND:
                    return SocketError.HostNotFound;
                case Interop.libc.NO_DATA:
                    return SocketError.NoData;
                case Interop.libc.NO_RECOVERY:
                    return SocketError.NoRecovery;
                case Interop.libc.TRY_AGAIN:
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
                case Interop.libc.EAI_AGAIN:
                    return SocketError.TryAgain;
                case Interop.libc.EAI_BADFLAGS:
                    return SocketError.InvalidArgument;
                case Interop.libc.EAI_FAIL:
                    return SocketError.NoRecovery;
                case Interop.libc.EAI_FAMILY:
                    return SocketError.AddressFamilyNotSupported;
                case Interop.libc.EAI_NONAME:
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
            var hints = new Interop.libc.addrinfo
            {
                ai_family = Interop.libc.AF_UNSPEC, // Get all address families
                ai_flags = Interop.libc.AI_CANONNAME
            };

            Interop.libc.addrinfo* addrinfo = null;
            string canonicalName = null;
            IPAddress[] ipAddresses;
            try
            {
                int errorCode = Interop.libc.getaddrinfo(name, null, &hints, &addrinfo);
                if (errorCode != 0)
                {
                    hostinfo = NameResolutionUtilities.GetUnresolvedAnswer(name);
                    nativeErrorCode = errorCode;
                    return GetSocketErrorForNativeError(errorCode);
                }

                int numAddresses = 0;
                for (Interop.libc.addrinfo* ai = addrinfo; ai != null; ai = ai->ai_next)
                {
                    if (canonicalName == null && ai->ai_canonname != null)
                    {
                        canonicalName = Marshal.PtrToStringAnsi((IntPtr)ai->ai_canonname);
                    }

                    if ((ai->ai_family != Interop.libc.AF_INET) &&
                        (ai->ai_family != Interop.libc.AF_INET6 || !SocketProtocolSupportPal.OSSupportsIPv6))
                    {
                        continue;
                    }

                    numAddresses++;
                }

                if (numAddresses == 0)
                {
                    ipAddresses = Array.Empty<IPAddress>();
                }
                else
                {
                    ipAddresses = new IPAddress[numAddresses];
                    Interop.libc.addrinfo* ai = addrinfo;
                    for (int i = 0; i < numAddresses; ai = ai->ai_next)
                    {
                        Debug.Assert(ai != null);

                        if ((ai->ai_family != Interop.libc.AF_INET) &&
                            (ai->ai_family != Interop.libc.AF_INET6 || !SocketProtocolSupportPal.OSSupportsIPv6))
                        {
                            continue;
                        }

                        var sockaddr = new SocketAddress(
                            ai->ai_family == Interop.libc.AF_INET ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6,
                            checked((int)ai->ai_addrlen));
                        for (int d = 0; d < ai->ai_addrlen; d++)
                        {
                            sockaddr[d] = ((byte*)ai->ai_addr)[d];
                        }

                        if (ai->ai_family == Interop.libc.AF_INET)
                        {
                            ipAddresses[i] = ((IPEndPoint)IPEndPointStatics.Any.Create(sockaddr)).Address;
                        }
                        else
                        {
                            ipAddresses[i] = ((IPEndPoint)IPEndPointStatics.IPv6Any.Create(sockaddr)).Address;
                        }

                        i++;
                    }
                }
            }
            finally
            {
                if (addrinfo != null)
                {
                    Interop.libc.freeaddrinfo(addrinfo);
                }
            }

            hostinfo = new IPHostEntry
            {
                HostName = canonicalName ?? name,
                Aliases = Array.Empty<string>(),
                AddressList = ipAddresses
            };
            nativeErrorCode = 0;
            return SocketError.Success;
        }

        public static unsafe string TryGetNameInfo(IPAddress addr, out SocketError socketError, out int nativeErrorCode)
        {
            SocketAddress address = (new IPEndPoint(addr, 0)).Serialize();

            // TODO #2894: Consider using stackalloc or StringBuilderCache:
            StringBuilder hostname = new StringBuilder(Interop.libc.NI_MAXHOST);

            // TODO #2891: Remove the copying step to improve performance. This requires a change in the contracts.
            byte[] addressBuffer = new byte[address.Size];
            for (int i = 0; i < address.Size; i++)
            {
                addressBuffer[i] = address[i];
            }

            int error;
            fixed (byte* rawAddress = addressBuffer)
            {
                error = Interop.libc.getnameinfo(
                    (Interop.libc.sockaddr*)rawAddress,
                    unchecked((uint)addressBuffer.Length),
                    hostname,
                    unchecked((uint)hostname.Capacity),
                    null,
                    0,
                    Interop.libc.NI_NAMEREQD);
            }

            socketError = GetSocketErrorForNativeError(error);
            nativeErrorCode = error;
            return socketError != SocketError.Success ? null : hostname.ToString();
        }

        public static string GetHostName()
        {
            return Interop.libc.gethostname();
        }

        public static void EnsureSocketsAreInitialized()
        {
            // No-op for Unix.
        }
    }
}
