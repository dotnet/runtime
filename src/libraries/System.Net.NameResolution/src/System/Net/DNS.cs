// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net.Internals;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    /// <devdoc>
    ///    <para>Provides simple
    ///       domain name resolution functionality.</para>
    /// </devdoc>

    public static class Dns
    {
        // Host names any longer than this automatically fail at the winsock level.
        // If the host name is 255 chars, the last char must be a dot.
        private const int MaxHostName = 255;

        [Obsolete("GetHostByName is obsoleted for this type, please use GetHostEntry instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry GetHostByName(string hostName)
        {
            NameResolutionPal.EnsureSocketsAreInitialized();

            if (hostName == null)
            {
                throw new ArgumentNullException(nameof(hostName));
            }

            // See if it's an IP Address.
            IPAddress address;
            if (IPAddress.TryParse(hostName, out address))
            {
                return NameResolutionUtilities.GetUnresolvedAnswer(address);
            }
            return InternalGetHostByName(hostName, false);
        }

        private static IPHostEntry InternalGetHostByName(string hostName, bool includeIPv6)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, hostName);
            IPHostEntry ipHostEntry = null;

            if (hostName.Length > MaxHostName // If 255 chars, the last one must be a dot.
                || hostName.Length == MaxHostName && hostName[MaxHostName - 1] != '.')
            {
                throw new ArgumentOutOfRangeException(nameof(hostName), SR.Format(SR.net_toolong,
                    nameof(hostName), MaxHostName.ToString(NumberFormatInfo.CurrentInfo)));
            }

            //
            // IPv6 Changes: IPv6 requires the use of getaddrinfo() rather
            //               than the traditional IPv4 gethostbyaddr() / gethostbyname().
            //               getaddrinfo() is also protocol independent in that it will also
            //               resolve IPv4 names / addresses. As a result, it is the preferred
            //               resolution mechanism on platforms that support it (Windows 5.1+).
            //               If getaddrinfo() is unsupported, IPv6 resolution does not work.
            //
            // Consider    : If IPv6 is disabled, we could detect IPv6 addresses
            //               and throw an unsupported platform exception.
            //
            // Note        : Whilst getaddrinfo is available on WinXP+, we only
            //               use it if IPv6 is enabled (platform is part of that
            //               decision). This is done to minimize the number of
            //               possible tests that are needed.
            //
            if (includeIPv6 || SocketProtocolSupportPal.OSSupportsIPv6)
            {
                //
                // IPv6 enabled: use getaddrinfo() to obtain DNS information.
                //
                int nativeErrorCode;
                SocketError errorCode = NameResolutionPal.TryGetAddrInfo(hostName, out ipHostEntry, out nativeErrorCode);
                if (errorCode != SocketError.Success)
                {
                    throw SocketExceptionFactory.CreateSocketException(errorCode, nativeErrorCode);
                }
            }
            else
            {
                ipHostEntry = NameResolutionPal.GetHostByName(hostName);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, ipHostEntry);
            return ipHostEntry;
        } // GetHostByName

        [Obsolete("GetHostByAddress is obsoleted for this type, please use GetHostEntry instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry GetHostByAddress(string address)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, address);
            NameResolutionPal.EnsureSocketsAreInitialized();

            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            IPHostEntry ipHostEntry = InternalGetHostByAddress(IPAddress.Parse(address), false);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, ipHostEntry);
            return ipHostEntry;
        } // GetHostByAddress

        [Obsolete("GetHostByAddress is obsoleted for this type, please use GetHostEntry instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry GetHostByAddress(IPAddress address)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, address);
            NameResolutionPal.EnsureSocketsAreInitialized();

            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            IPHostEntry ipHostEntry = InternalGetHostByAddress(address, false);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, ipHostEntry);
            return ipHostEntry;
        } // GetHostByAddress
        
        // Does internal IPAddress reverse and then forward lookups (for Legacy and current public methods).
        private static IPHostEntry InternalGetHostByAddress(IPAddress address, bool includeIPv6)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(null, address);
            
            //
            // IPv6 Changes: We need to use the new getnameinfo / getaddrinfo functions
            //               for resolution of IPv6 addresses.
            //

            if (SocketProtocolSupportPal.OSSupportsIPv6 || includeIPv6)
            {
                //
                // Try to get the data for the host from it's address
                //
                // We need to call getnameinfo first, because getaddrinfo w/ the ipaddress string
                // will only return that address and not the full list.

                // Do a reverse lookup to get the host name.
                SocketError errorCode;
                int nativeErrorCode;
                string name = NameResolutionPal.TryGetNameInfo(address, out errorCode, out nativeErrorCode);
                if (errorCode == SocketError.Success)
                {
                    // Do the forward lookup to get the IPs for that host name
                    IPHostEntry hostEntry;
                    errorCode = NameResolutionPal.TryGetAddrInfo(name, out hostEntry, out nativeErrorCode);
                    if (errorCode == SocketError.Success)
                    {
                        return hostEntry;
                    }

                    if (NetEventSource.IsEnabled) NetEventSource.Error(null, SocketExceptionFactory.CreateSocketException(errorCode, nativeErrorCode));

                    // One of two things happened:
                    // 1. There was a ptr record in dns, but not a corollary A/AAA record.
                    // 2. The IP was a local (non-loopback) IP that resolved to a connection specific dns suffix.
                    //    - Workaround, Check "Use this connection's dns suffix in dns registration" on that network
                    //      adapter's advanced dns settings.

                    // Just return the resolved host name and no IPs.
                    return hostEntry;
                }

                throw SocketExceptionFactory.CreateSocketException(errorCode, nativeErrorCode);
            }

            //
            // If IPv6 is not enabled (maybe config switch) but we've been
            // given an IPv6 address then we need to bail out now.
            //
            else
            {
                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    //
                    // Protocol not supported
                    //
                    throw new SocketException((int)SocketError.ProtocolNotSupported);
                }
                //
                // Use gethostbyaddr() to try to resolve the IP address
                //
                // End IPv6 Changes
                //
                return NameResolutionPal.GetHostByAddr(address);
            }
        } // InternalGetHostByAddress

        /*****************************************************************************
         Function :    gethostname

         Abstract:     Queries the hostname from DNS

         Input Parameters:

         Returns: String
        ******************************************************************************/

        /// <devdoc>
        ///    <para>Gets the host name of the local machine.</para>
        /// </devdoc>
        public static string GetHostName()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(null, null);

            NameResolutionPal.EnsureSocketsAreInitialized();

            return NameResolutionPal.GetHostName();
        }

        [Obsolete("Resolve is obsoleted for this type, please use GetHostEntry instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry Resolve(string hostName)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, hostName);

            NameResolutionPal.EnsureSocketsAreInitialized();

            if (hostName == null)
            {
                throw new ArgumentNullException(nameof(hostName));
            }

            // See if it's an IP Address.
            IPAddress address;
            IPHostEntry ipHostEntry;

            if (IPAddress.TryParse(hostName, out address) && (address.AddressFamily != AddressFamily.InterNetworkV6 || SocketProtocolSupportPal.OSSupportsIPv6))
            {
                try
                {
                    ipHostEntry = InternalGetHostByAddress(address, false);
                }
                catch (SocketException ex)
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Error(null, ex);
                    ipHostEntry = NameResolutionUtilities.GetUnresolvedAnswer(address);
                }
            }
            else
            {
                ipHostEntry = InternalGetHostByName(hostName, false);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, ipHostEntry);
            return ipHostEntry;
        }

        private class ResolveAsyncResult : ContextAwareResult
        {
            // Forward lookup
            internal ResolveAsyncResult(string hostName, object myObject, bool includeIPv6, object myState, AsyncCallback myCallBack) :
                base(myObject, myState, myCallBack)
            {
                this.hostName = hostName;
                this.includeIPv6 = includeIPv6;
            }

            // Reverse lookup
            internal ResolveAsyncResult(IPAddress address, object myObject, bool includeIPv6, object myState, AsyncCallback myCallBack) :
                base(myObject, myState, myCallBack)
            {
                this.includeIPv6 = includeIPv6;
                this.address = address;
            }

            internal readonly string hostName;
            internal bool includeIPv6;
            internal IPAddress address;
        }

        private static void ResolveCallback(object context)
        {
            ResolveAsyncResult result = (ResolveAsyncResult)context;
            IPHostEntry hostEntry;
            try
            {
                if (result.address != null)
                {
                    hostEntry = InternalGetHostByAddress(result.address, result.includeIPv6);
                }
                else
                {
                    hostEntry = InternalGetHostByName(result.hostName, result.includeIPv6);
                }
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception exception)
            {
                result.InvokeCallback(exception);
                return;
            }

            result.InvokeCallback(hostEntry);
        }

        // Helpers for async GetHostByName, ResolveToAddresses, and Resolve - they're almost identical
        // If hostName is an IPString and justReturnParsedIP==true then no reverse lookup will be attempted, but the original address is returned.
        private static IAsyncResult HostResolutionBeginHelper(string hostName, bool justReturnParsedIp, bool includeIPv6, bool throwOnIIPAny, AsyncCallback requestCallback, object state)
        {
            if (hostName == null)
            {
                throw new ArgumentNullException(nameof(hostName));
            }

            if (NetEventSource.IsEnabled) NetEventSource.Info(null, hostName);

            // See if it's an IP Address.
            IPAddress address;
            ResolveAsyncResult asyncResult;
            if (IPAddress.TryParse(hostName, out address))
            {
                if (throwOnIIPAny && (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)))
                {
                    throw new ArgumentException(SR.net_invalid_ip_addr, nameof(hostName));
                }

                asyncResult = new ResolveAsyncResult(address, null, includeIPv6, state, requestCallback);

                if (justReturnParsedIp)
                {
                    IPHostEntry hostEntry = NameResolutionUtilities.GetUnresolvedAnswer(address);
                    asyncResult.StartPostingAsyncOp(false);
                    asyncResult.InvokeCallback(hostEntry);
                    asyncResult.FinishPostingAsyncOp();
                    return asyncResult;
                }
            }
            else
            {
                asyncResult = new ResolveAsyncResult(hostName, null, includeIPv6, state, requestCallback);
            }

            // Set up the context, possibly flow.
            asyncResult.StartPostingAsyncOp(false);

            // Start the resolve.
            Task.Factory.StartNew(
                s => ResolveCallback(s),
                asyncResult,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

            // Finish the flowing, maybe it completed?  This does nothing if we didn't initiate the flowing above.
            asyncResult.FinishPostingAsyncOp();
            return asyncResult;
        }

        private static IAsyncResult HostResolutionBeginHelper(IPAddress address, bool flowContext, bool includeIPv6, AsyncCallback requestCallback, object state)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            {
                throw new ArgumentException(SR.net_invalid_ip_addr, nameof(address));
            }

            if (NetEventSource.IsEnabled) NetEventSource.Info(null, address);

            // Set up the context, possibly flow.
            ResolveAsyncResult asyncResult = new ResolveAsyncResult(address, null, includeIPv6, state, requestCallback);
            if (flowContext)
            {
                asyncResult.StartPostingAsyncOp(false);
            }

            // Start the resolve.
            Task.Factory.StartNew(
                s => ResolveCallback(s),
                asyncResult,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

            // Finish the flowing, maybe it completed?  This does nothing if we didn't initiate the flowing above.
            asyncResult.FinishPostingAsyncOp();
            return asyncResult;
        }

        private static IPHostEntry HostResolutionEndHelper(IAsyncResult asyncResult)
        {
            //
            // parameter validation
            //
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }
            ResolveAsyncResult castedResult = asyncResult as ResolveAsyncResult;
            if (castedResult == null)
            {
                throw new ArgumentException(SR.net_io_invalidasyncresult, nameof(asyncResult));
            }
            if (castedResult.EndCalled)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, nameof(EndResolve)));
            }

            if (NetEventSource.IsEnabled) NetEventSource.Info(null);

            castedResult.InternalWaitForCompletion();
            castedResult.EndCalled = true;

            Exception exception = castedResult.Result as Exception;
            if (exception != null)
            {
                throw exception;
            }

            return (IPHostEntry)castedResult.Result;
        }

        [Obsolete("BeginGetHostByName is obsoleted for this type, please use BeginGetHostEntry instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static IAsyncResult BeginGetHostByName(string hostName, AsyncCallback requestCallback, object stateObject)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, hostName);

            NameResolutionPal.EnsureSocketsAreInitialized();

            IAsyncResult asyncResult = HostResolutionBeginHelper(hostName, true, true, false, requestCallback, stateObject);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, asyncResult);
            return asyncResult;
        } // BeginGetHostByName

        [Obsolete("EndGetHostByName is obsoleted for this type, please use EndGetHostEntry instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry EndGetHostByName(IAsyncResult asyncResult)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, asyncResult);
            NameResolutionPal.EnsureSocketsAreInitialized();

            IPHostEntry ipHostEntry = HostResolutionEndHelper(asyncResult);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, ipHostEntry);
            return ipHostEntry;
        } // EndGetHostByName()

        public static IPHostEntry GetHostEntry(string hostNameOrAddress)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, hostNameOrAddress);
            NameResolutionPal.EnsureSocketsAreInitialized();

            if (hostNameOrAddress == null)
            {
                throw new ArgumentNullException(nameof(hostNameOrAddress));
            }

            // See if it's an IP Address.
            IPAddress address;
            IPHostEntry ipHostEntry;
            if (IPAddress.TryParse(hostNameOrAddress, out address))
            {
                if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
                {
                    throw new ArgumentException(SR.Format(SR.net_invalid_ip_addr, nameof(hostNameOrAddress)));
                }

                ipHostEntry = InternalGetHostByAddress(address, true);
            }
            else
            {
                ipHostEntry = InternalGetHostByName(hostNameOrAddress, true);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, ipHostEntry);
            return ipHostEntry;
        }


        public static IPHostEntry GetHostEntry(IPAddress address)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, address);
            NameResolutionPal.EnsureSocketsAreInitialized();

            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            {
                throw new ArgumentException(SR.Format(SR.net_invalid_ip_addr, nameof(address)));
            }

            IPHostEntry ipHostEntry = InternalGetHostByAddress(address, true);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, ipHostEntry);
            return ipHostEntry;
        } // GetHostEntry

        public static IPAddress[] GetHostAddresses(string hostNameOrAddress)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, hostNameOrAddress);
            NameResolutionPal.EnsureSocketsAreInitialized();

            if (hostNameOrAddress == null)
            {
                throw new ArgumentNullException(nameof(hostNameOrAddress));
            }

            // See if it's an IP Address.
            IPAddress address;
            IPAddress[] addresses;
            if (IPAddress.TryParse(hostNameOrAddress, out address))
            {
                if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
                {
                    throw new ArgumentException(SR.Format(SR.net_invalid_ip_addr, nameof(hostNameOrAddress)));
                }
                addresses = new IPAddress[] { address };
            }
            else
            {
                // InternalGetHostByName works with IP addresses (and avoids a reverse-lookup), but we need
                // explicit handling in order to do the ArgumentException and guarantee the behavior.
                addresses = InternalGetHostByName(hostNameOrAddress, true).AddressList;
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, addresses);
            return addresses;
        }

        public static IAsyncResult BeginGetHostEntry(string hostNameOrAddress, AsyncCallback requestCallback, object stateObject)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, hostNameOrAddress);
            NameResolutionPal.EnsureSocketsAreInitialized();

            IAsyncResult asyncResult = HostResolutionBeginHelper(hostNameOrAddress, false, true, true, requestCallback, stateObject);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, asyncResult);
            return asyncResult;
        } // BeginResolve

        public static IAsyncResult BeginGetHostEntry(IPAddress address, AsyncCallback requestCallback, object stateObject)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, address);

            NameResolutionPal.EnsureSocketsAreInitialized();

            IAsyncResult asyncResult = HostResolutionBeginHelper(address, true, true, requestCallback, stateObject);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, asyncResult);
            return asyncResult;
        } // BeginResolve

        public static IPHostEntry EndGetHostEntry(IAsyncResult asyncResult)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, asyncResult);
            IPHostEntry ipHostEntry = HostResolutionEndHelper(asyncResult);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, ipHostEntry);
            return ipHostEntry;
        } // EndResolve()

        public static IAsyncResult BeginGetHostAddresses(string hostNameOrAddress, AsyncCallback requestCallback, object state)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, hostNameOrAddress);
            NameResolutionPal.EnsureSocketsAreInitialized();

            IAsyncResult asyncResult = HostResolutionBeginHelper(hostNameOrAddress, true, true, true, requestCallback, state);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, asyncResult);
            return asyncResult;
        } // BeginResolve

        public static IPAddress[] EndGetHostAddresses(IAsyncResult asyncResult)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, asyncResult);
            IPHostEntry ipHostEntry = HostResolutionEndHelper(asyncResult);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, ipHostEntry);
            return ipHostEntry.AddressList;
        } // EndResolveToAddresses

        [Obsolete("BeginResolve is obsoleted for this type, please use BeginGetHostEntry instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static IAsyncResult BeginResolve(string hostName, AsyncCallback requestCallback, object stateObject)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, hostName);

            NameResolutionPal.EnsureSocketsAreInitialized();

            IAsyncResult asyncResult = HostResolutionBeginHelper(hostName, false, false, false, requestCallback, stateObject);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, asyncResult);
            return asyncResult;
        } // BeginResolve


        [Obsolete("EndResolve is obsoleted for this type, please use EndGetHostEntry instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry EndResolve(IAsyncResult asyncResult)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, asyncResult);
            IPHostEntry ipHostEntry;

            try
            {
                ipHostEntry = HostResolutionEndHelper(asyncResult);
            }
            catch (SocketException ex)
            {
                IPAddress address = ((ResolveAsyncResult)asyncResult).address;
                if (address == null)
                    throw; // BeginResolve was called with a HostName, not an IPAddress

                if (NetEventSource.IsEnabled) NetEventSource.Error(null, ex);
                ipHostEntry = NameResolutionUtilities.GetUnresolvedAnswer(address);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, ipHostEntry);
            return ipHostEntry;
        } // EndResolve()

        //************* Task-based async public methods *************************
        public static Task<IPAddress[]> GetHostAddressesAsync(string hostNameOrAddress)
        {
            NameResolutionPal.EnsureSocketsAreInitialized();
            return Task<IPAddress[]>.Factory.FromAsync(
                (arg, requestCallback, stateObject) => BeginGetHostAddresses(arg, requestCallback, stateObject),
                asyncResult => EndGetHostAddresses(asyncResult),
                hostNameOrAddress,
                null);
        }

        public static Task<IPHostEntry> GetHostEntryAsync(IPAddress address)
        {
            NameResolutionPal.EnsureSocketsAreInitialized();
            return Task<IPHostEntry>.Factory.FromAsync(
                (arg, requestCallback, stateObject) => BeginGetHostEntry(arg, requestCallback, stateObject),
                asyncResult => EndGetHostEntry(asyncResult),
                address,
                null);
        }

        public static Task<IPHostEntry> GetHostEntryAsync(string hostNameOrAddress)
        {
            NameResolutionPal.EnsureSocketsAreInitialized();
            return Task<IPHostEntry>.Factory.FromAsync(
                (arg, requestCallback, stateObject) => BeginGetHostEntry(arg, requestCallback, stateObject),
                asyncResult => EndGetHostEntry(asyncResult),
                hostNameOrAddress,
                null);
        }
    }
}
