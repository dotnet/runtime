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

        internal static IPHostEntry InternalGetHostByName(string hostName, bool includeIPv6)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.Socket, "DNS", "GetHostByName", hostName);
            IPHostEntry ipHostEntry = null;

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("Dns.GetHostByName: " + hostName);
            }

            NameResolutionPal.EnsureSocketsAreInitialized();

            if (hostName.Length > MaxHostName // If 255 chars, the last one must be a dot.
                || hostName.Length == MaxHostName && hostName[MaxHostName - 1] != '.')
            {
                throw new ArgumentOutOfRangeException(nameof(hostName), SR.Format(SR.net_toolong,
                    "hostName", MaxHostName.ToString(NumberFormatInfo.CurrentInfo)));
            }

            //
            // IPv6 Changes: IPv6 requires the use of getaddrinfo() rather
            //               than the traditional IPv4 gethostbyaddr() / gethostbyname().
            //               getaddrinfo() is also protocol independant in that it will also
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
                    throw new InternalSocketException(errorCode, nativeErrorCode);
                }
            }
            else
            {
                ipHostEntry = NameResolutionPal.GetHostByName(hostName);
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.Socket, "DNS", "GetHostByName", ipHostEntry);
            return ipHostEntry;
        } // GetHostByName

        // Does internal IPAddress reverse and then forward lookups (for Legacy and current public methods).
        internal static IPHostEntry InternalGetHostByAddress(IPAddress address, bool includeIPv6)
        {
            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("Dns.InternalGetHostByAddress: " + address.ToString());
            }
            
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
                        return hostEntry;

                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Exception(NetEventSource.ComponentType.Socket, "DNS",
                        "InternalGetHostByAddress", new InternalSocketException(errorCode, nativeErrorCode));
                    }

                    // One of two things happened:
                    // 1. There was a ptr record in dns, but not a corollary A/AAA record.
                    // 2. The IP was a local (non-loopback) IP that resolved to a connection specific dns suffix.
                    //    - Workaround, Check "Use this connection's dns suffix in dns registration" on that network
                    //      adapter's advanced dns settings.

                    // Just return the resolved host name and no IPs.
                    return hostEntry;
                }
                throw new InternalSocketException(errorCode, nativeErrorCode);
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
            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("Dns.GetHostName");
            }

            return NameResolutionPal.GetHostName();
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
        // If hostName is an IPString and justReturnParsedIP==true then no reverse lookup will be attempted, but the orriginal address is returned.
        private static IAsyncResult HostResolutionBeginHelper(string hostName, bool justReturnParsedIp, AsyncCallback requestCallback, object state)
        {
            if (hostName == null)
            {
                throw new ArgumentNullException(nameof(hostName));
            }

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("Dns.HostResolutionBeginHelper: " + hostName);
            }

            // See if it's an IP Address.
            IPAddress address;
            ResolveAsyncResult asyncResult;
            if (IPAddress.TryParse(hostName, out address))
            {
                if ((address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)))
                {
                    throw new ArgumentException(SR.net_invalid_ip_addr, "hostNameOrAddress");
                }

                asyncResult = new ResolveAsyncResult(address, null, true, state, requestCallback);

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
                asyncResult = new ResolveAsyncResult(hostName, null, true, state, requestCallback);
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

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("Dns.HostResolutionBeginHelper: " + address);
            }

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
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndResolve"));
            }

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("Dns.HostResolutionEndHelper");
            }

            castedResult.InternalWaitForCompletion();
            castedResult.EndCalled = true;

            Exception exception = castedResult.Result as Exception;
            if (exception != null)
            {
                throw exception;
            }

            return (IPHostEntry)castedResult.Result;
        }

        private static IAsyncResult BeginGetHostEntry(string hostNameOrAddress, AsyncCallback requestCallback, object stateObject)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.Socket, "DNS", "BeginGetHostEntry", hostNameOrAddress);
            IAsyncResult asyncResult = HostResolutionBeginHelper(hostNameOrAddress, false, requestCallback, stateObject);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.Socket, "DNS", "BeginGetHostEntry", asyncResult);
            return asyncResult;
        } // BeginResolve

        private static IAsyncResult BeginGetHostEntry(IPAddress address, AsyncCallback requestCallback, object stateObject)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.Socket, "DNS", "BeginGetHostEntry", address);
            IAsyncResult asyncResult = HostResolutionBeginHelper(address, true, true, requestCallback, stateObject);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.Socket, "DNS", "BeginGetHostEntry", asyncResult);
            return asyncResult;
        } // BeginResolve

        private static IPHostEntry EndGetHostEntry(IAsyncResult asyncResult)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.Socket, "DNS", "EndGetHostEntry", asyncResult);
            IPHostEntry ipHostEntry = HostResolutionEndHelper(asyncResult);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.Socket, "DNS", "EndGetHostEntry", ipHostEntry);
            return ipHostEntry;
        } // EndResolve()

        private static IAsyncResult BeginGetHostAddresses(string hostNameOrAddress, AsyncCallback requestCallback, object state)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.Socket, "DNS", "BeginGetHostAddresses", hostNameOrAddress);
            IAsyncResult asyncResult = HostResolutionBeginHelper(hostNameOrAddress, true, requestCallback, state);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.Socket, "DNS", "BeginGetHostAddresses", asyncResult);
            return asyncResult;
        } // BeginResolve

        private static IPAddress[] EndGetHostAddresses(IAsyncResult asyncResult)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.Socket, "DNS", "EndGetHostAddresses", asyncResult);
            IPHostEntry ipHostEntry = HostResolutionEndHelper(asyncResult);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.Socket, "DNS", "EndGetHostAddresses", ipHostEntry);
            return ipHostEntry.AddressList;
        } // EndResolveToAddresses

        //************* Task-based async public methods *************************
        public static Task<IPAddress[]> GetHostAddressesAsync(string hostNameOrAddress)
        {
            return Task<IPAddress[]>.Factory.FromAsync(
                (arg, requestCallback, stateObject) => BeginGetHostAddresses(arg, requestCallback, stateObject),
                asyncResult => EndGetHostAddresses(asyncResult),
                hostNameOrAddress,
                null);
        }

        public static Task<IPHostEntry> GetHostEntryAsync(IPAddress address)
        {
            return Task<IPHostEntry>.Factory.FromAsync(
                (arg, requestCallback, stateObject) => BeginGetHostEntry(arg, requestCallback, stateObject),
                asyncResult => EndGetHostEntry(asyncResult),
                address,
                null);
        }

        public static Task<IPHostEntry> GetHostEntryAsync(string hostNameOrAddress)
        {
            return Task<IPHostEntry>.Factory.FromAsync(
                (arg, requestCallback, stateObject) => BeginGetHostEntry(arg, requestCallback, stateObject),
                asyncResult => EndGetHostEntry(asyncResult),
                hostNameOrAddress,
                null);
        }
    }
}
