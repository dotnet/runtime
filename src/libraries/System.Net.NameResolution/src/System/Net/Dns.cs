// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    /// <summary>Provides simple domain name resolution functionality.</summary>
    public static class Dns
    {
        /// <summary>Name of environment variable storing optional hosts file used by Dns.</summary>
        /// <remarks>
        /// If this environment variable is set, its value is used as the path to a hosts file.
        /// If that hosts file is found and it contains a name or IP address being searched for,
        /// its data will be used exclusively instead of performing a DNS query via the OS.
        /// </remarks>
        private const string HostsFileEnvVarName = "DOTNET_SYSTEM_NET_HOSTS";

        /// <summary>Optional overrides specified in a custom host file.</summary>
        /// <remarks>
        /// If non-null, Dns queries first consult the tables in these overrides.
        /// If the specified host name or address is found, the override data is used exclusively.
        /// Otherwise, the query falls through to normal operation.
        /// </remarks>
        private static readonly HostsOverridesLookup? s_hostsOverrides = LoadHostsOverrides();

        /// <summary>Gets the host name of the local machine.</summary>
        public static string GetHostName()
        {
            long startingTimestamp = NameResolutionTelemetry.Log.BeforeResolution(string.Empty);

            string name;
            try
            {
                name = NameResolutionPal.GetHostName();
            }
            catch when (LogFailure(startingTimestamp))
            {
                Debug.Fail("LogFailure should return false");
                throw;
            }

            NameResolutionTelemetry.Log.AfterResolution(startingTimestamp, successful: true);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, name);
            return name;
        }

        public static IPHostEntry GetHostEntry(IPAddress address)
        {
            ArgumentNullException.ThrowIfNull(address);

            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(address, $"Invalid address '{address}'");
                throw new ArgumentException(SR.net_invalid_ip_addr, nameof(address));
            }

            IPHostEntry ipHostEntry = GetHostEntryCore(address, AddressFamily.Unspecified);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(address, $"{ipHostEntry} with {ipHostEntry.AddressList.Length} entries");
            return ipHostEntry;
        }

        public static IPHostEntry GetHostEntry(string hostNameOrAddress) =>
            GetHostEntry(hostNameOrAddress, AddressFamily.Unspecified);

        /// <summary>
        /// Resolves a host name or IP address to an <see cref="IPHostEntry"/> instance.
        /// </summary>
        /// <param name="hostNameOrAddress">The host name or IP address to resolve.</param>
        /// <param name="family">The address family for which IPs should be retrieved. If <see cref="AddressFamily.Unspecified"/>, retrieve all IPs regardless of address family.</param>
        /// <returns>
        /// An <see cref="IPHostEntry"/> instance that contains the address information about the host specified in <paramref name="hostNameOrAddress"/>.
        /// </returns>
        public static IPHostEntry GetHostEntry(string hostNameOrAddress, AddressFamily family)
        {
            ArgumentNullException.ThrowIfNull(hostNameOrAddress);

            // See if it's an IP Address.
            IPHostEntry ipHostEntry;
            if (IPAddress.TryParse(hostNameOrAddress, out IPAddress? address))
            {
                if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(address, $"Invalid address '{address}'");
                    throw new ArgumentException(SR.net_invalid_ip_addr, nameof(hostNameOrAddress));
                }

                ipHostEntry = GetHostEntryCore(address, family);
            }
            else
            {
                ipHostEntry = GetHostEntryCore(hostNameOrAddress, family);
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(hostNameOrAddress, $"{ipHostEntry} with {ipHostEntry.AddressList.Length} entries");
            return ipHostEntry;
        }

        public static Task<IPHostEntry> GetHostEntryAsync(string hostNameOrAddress) =>
            GetHostEntryAsync(hostNameOrAddress, AddressFamily.Unspecified, CancellationToken.None);

        /// <summary>
        /// Resolves a host name or IP address to an <see cref="IPHostEntry"/> instance as an asynchronous operation.
        /// </summary>
        /// <param name="hostNameOrAddress">The host name or IP address to resolve.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to signal the asynchronous operation should be canceled.</param>
        /// <returns>
        /// The task object representing the asynchronous operation. The <see cref="Task{TResult}.Result"/> property on the task object returns
        /// an <see cref="IPHostEntry"/> instance that contains the address information about the host specified in <paramref name="hostNameOrAddress"/>.
        /// </returns>
        public static Task<IPHostEntry> GetHostEntryAsync(string hostNameOrAddress, CancellationToken cancellationToken) =>
            GetHostEntryAsync(hostNameOrAddress, AddressFamily.Unspecified, cancellationToken);

        /// <summary>
        /// Resolves a host name or IP address to an <see cref="IPHostEntry"/> instance as an asynchronous operation.
        /// </summary>
        /// <param name="hostNameOrAddress">The host name or IP address to resolve.</param>
        /// <param name="family">The address family for which IPs should be retrieved. If <see cref="AddressFamily.Unspecified"/>, retrieve all IPs regardless of address family.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to signal the asynchronous operation should be canceled.</param>
        /// <returns>
        /// The task object representing the asynchronous operation. The <see cref="Task{TResult}.Result"/> property on the task object returns
        /// an <see cref="IPHostEntry"/> instance that contains the address information about the host specified in <paramref name="hostNameOrAddress"/>.
        /// </returns>
        public static Task<IPHostEntry> GetHostEntryAsync(string hostNameOrAddress, AddressFamily family, CancellationToken cancellationToken = default)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                Task<IPHostEntry> t = GetHostEntryCoreAsync(hostNameOrAddress, justReturnParsedIp: false, throwOnIIPAny: true, family, cancellationToken);
                t.ContinueWith(static (t, s) =>
                {
                    string hostNameOrAddress = (string)s!;

                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        NetEventSource.Info(hostNameOrAddress, $"{t.Result} with {t.Result.AddressList.Length} entries");
                    }

                    Exception? ex = t.Exception?.InnerException;

                    if (ex is SocketException soex)
                    {
                        NetEventSource.Error(hostNameOrAddress, $"{hostNameOrAddress} DNS lookup failed with {soex.ErrorCode}");
                    }
                    else if (ex is OperationCanceledException)
                    {
                        NetEventSource.Error(hostNameOrAddress, $"{hostNameOrAddress} DNS lookup was canceled");
                    }
                }, hostNameOrAddress, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                return t;
            }
            else
            {
                return GetHostEntryCoreAsync(hostNameOrAddress, justReturnParsedIp: false, throwOnIIPAny: true, family, cancellationToken);
            }
        }

        public static Task<IPHostEntry> GetHostEntryAsync(IPAddress address)
        {
            ArgumentNullException.ThrowIfNull(address);

            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(address, $"Invalid address '{address}'");
                throw new ArgumentException(SR.net_invalid_ip_addr, nameof(address));
            }

            return RunAsync(static (s, stopwatch) =>
            {
                IPHostEntry ipHostEntry = GetHostEntryCore((IPAddress)s, AddressFamily.Unspecified, stopwatch);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info((IPAddress)s, $"{ipHostEntry} with {ipHostEntry.AddressList.Length} entries");
                return ipHostEntry;
            }, address, CancellationToken.None);
        }

        public static IAsyncResult BeginGetHostEntry(IPAddress address, AsyncCallback? requestCallback, object? stateObject) =>
            TaskToAsyncResult.Begin(GetHostEntryAsync(address), requestCallback, stateObject);

        public static IAsyncResult BeginGetHostEntry(string hostNameOrAddress, AsyncCallback? requestCallback, object? stateObject) =>
            TaskToAsyncResult.Begin(GetHostEntryAsync(hostNameOrAddress), requestCallback, stateObject);

        public static IPHostEntry EndGetHostEntry(IAsyncResult asyncResult)
        {
            ArgumentNullException.ThrowIfNull(asyncResult);

            return TaskToAsyncResult.End<IPHostEntry>(asyncResult);
        }

        public static IPAddress[] GetHostAddresses(string hostNameOrAddress)
            => GetHostAddresses(hostNameOrAddress, AddressFamily.Unspecified);

        /// <summary>
        /// Returns the Internet Protocol (IP) addresses for the specified host.
        /// </summary>
        /// <param name="hostNameOrAddress">The host name or IP address to resolve.</param>
        /// <param name="family">The address family for which IPs should be retrieved. If <see cref="AddressFamily.Unspecified"/>, retrieve all IPs regardless of address family.</param>
        /// <returns>
        /// An array of type <see cref="IPAddress"/> that holds the IP addresses for the host that is specified by the <paramref name="hostNameOrAddress"/> parameter.
        /// </returns>
        public static IPAddress[] GetHostAddresses(string hostNameOrAddress, AddressFamily family)
        {
            ArgumentNullException.ThrowIfNull(hostNameOrAddress);

            // See if it's an IP Address.
            IPAddress[] addresses;
            if (IPAddress.TryParse(hostNameOrAddress, out IPAddress? address))
            {
                if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(address, $"Invalid address '{address}'");
                    throw new ArgumentException(SR.net_invalid_ip_addr, nameof(hostNameOrAddress));
                }

                addresses = (family == AddressFamily.Unspecified || address.AddressFamily == family) ? new IPAddress[] { address } : Array.Empty<IPAddress>();
            }
            else
            {
                addresses = GetHostAddressesCore(hostNameOrAddress, family);
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(hostNameOrAddress, addresses);
            return addresses;
        }

        public static Task<IPAddress[]> GetHostAddressesAsync(string hostNameOrAddress) =>
            (Task<IPAddress[]>)GetHostEntryOrAddressesCoreAsync(hostNameOrAddress, justReturnParsedIp: true, throwOnIIPAny: true, justAddresses: true, AddressFamily.Unspecified, CancellationToken.None);

        /// <summary>
        /// Returns the Internet Protocol (IP) addresses for the specified host as an asynchronous operation.
        /// </summary>
        /// <param name="hostNameOrAddress">The host name or IP address to resolve.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to signal the asynchronous operation should be canceled.</param>
        /// <returns>
        /// The task object representing the asynchronous operation. The <see cref="Task{TResult}.Result"/> property on the task object returns an array of
        /// type <see cref="IPAddress"/> that holds the IP addresses for the host that is specified by the <paramref name="hostNameOrAddress"/> parameter.
        /// </returns>
        public static Task<IPAddress[]> GetHostAddressesAsync(string hostNameOrAddress, CancellationToken cancellationToken) =>
            (Task<IPAddress[]>)GetHostEntryOrAddressesCoreAsync(hostNameOrAddress, justReturnParsedIp: true, throwOnIIPAny: true, justAddresses: true, AddressFamily.Unspecified, cancellationToken);

        /// <summary>
        /// Returns the Internet Protocol (IP) addresses for the specified host as an asynchronous operation.
        /// </summary>
        /// <param name="hostNameOrAddress">The host name or IP address to resolve.</param>
        /// <param name="family">The address family for which IPs should be retrieved. If <see cref="AddressFamily.Unspecified"/>, retrieve all IPs regardless of address family.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to signal the asynchronous operation should be canceled.</param>
        /// <returns>
        /// The task object representing the asynchronous operation. The <see cref="Task{TResult}.Result"/> property on the task object returns an array of
        /// type <see cref="IPAddress"/> that holds the IP addresses for the host that is specified by the <paramref name="hostNameOrAddress"/> parameter.
        /// </returns>
        public static Task<IPAddress[]> GetHostAddressesAsync(string hostNameOrAddress, AddressFamily family, CancellationToken cancellationToken = default) =>
            (Task<IPAddress[]>)GetHostEntryOrAddressesCoreAsync(hostNameOrAddress, justReturnParsedIp: true, throwOnIIPAny: true, justAddresses: true, family, cancellationToken);

        public static IAsyncResult BeginGetHostAddresses(string hostNameOrAddress, AsyncCallback? requestCallback, object? state) =>
            TaskToAsyncResult.Begin(GetHostAddressesAsync(hostNameOrAddress), requestCallback, state);

        public static IPAddress[] EndGetHostAddresses(IAsyncResult asyncResult)
        {
            ArgumentNullException.ThrowIfNull(asyncResult);

            return TaskToAsyncResult.End<IPAddress[]>(asyncResult);
        }

        [Obsolete("GetHostByName has been deprecated. Use GetHostEntry instead.")]
        public static IPHostEntry GetHostByName(string hostName)
        {
            ArgumentNullException.ThrowIfNull(hostName);

            if (IPAddress.TryParse(hostName, out IPAddress? address))
            {
                return CreateHostEntryForAddress(address);
            }

            return GetHostEntryCore(hostName, AddressFamily.Unspecified);
        }

        [Obsolete("BeginGetHostByName has been deprecated. Use BeginGetHostEntry instead.")]
        public static IAsyncResult BeginGetHostByName(string hostName, AsyncCallback? requestCallback, object? stateObject) =>
            TaskToAsyncResult.Begin(GetHostEntryCoreAsync(hostName, justReturnParsedIp: true, throwOnIIPAny: true, AddressFamily.Unspecified, CancellationToken.None), requestCallback, stateObject);

        [Obsolete("EndGetHostByName has been deprecated. Use EndGetHostEntry instead.")]
        public static IPHostEntry EndGetHostByName(IAsyncResult asyncResult)
        {
            ArgumentNullException.ThrowIfNull(asyncResult);

            return TaskToAsyncResult.End<IPHostEntry>(asyncResult);
        }

        [Obsolete("GetHostByAddress has been deprecated. Use GetHostEntry instead.")]
        public static IPHostEntry GetHostByAddress(string address)
        {
            ArgumentNullException.ThrowIfNull(address);

            IPHostEntry ipHostEntry = GetHostEntryCore(IPAddress.Parse(address), AddressFamily.Unspecified);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(address, ipHostEntry);
            return ipHostEntry;
        }

        [Obsolete("GetHostByAddress has been deprecated. Use GetHostEntry instead.")]
        public static IPHostEntry GetHostByAddress(IPAddress address)
        {
            ArgumentNullException.ThrowIfNull(address);

            IPHostEntry ipHostEntry = GetHostEntryCore(address, AddressFamily.Unspecified);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(address, ipHostEntry);
            return ipHostEntry;
        }

        [Obsolete("Resolve has been deprecated. Use GetHostEntry instead.")]
        public static IPHostEntry Resolve(string hostName)
        {
            ArgumentNullException.ThrowIfNull(hostName);

            // See if it's an IP Address.
            IPHostEntry ipHostEntry;
            if (IPAddress.TryParse(hostName, out IPAddress? address) &&
                (address.AddressFamily != AddressFamily.InterNetworkV6 || SocketProtocolSupportPal.OSSupportsIPv6))
            {
                try
                {
                    ipHostEntry = GetHostEntryCore(address, AddressFamily.Unspecified);
                }
                catch (SocketException ex)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(hostName, ex);
                    ipHostEntry = CreateHostEntryForAddress(address);
                }
            }
            else
            {
                ipHostEntry = GetHostEntryCore(hostName, AddressFamily.Unspecified);
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(hostName, ipHostEntry);
            return ipHostEntry;
        }

        [Obsolete("BeginResolve has been deprecated. Use BeginGetHostEntry instead.")]
        public static IAsyncResult BeginResolve(string hostName, AsyncCallback? requestCallback, object? stateObject) =>
            TaskToAsyncResult.Begin(GetHostEntryCoreAsync(hostName, justReturnParsedIp: false, throwOnIIPAny: false, AddressFamily.Unspecified, CancellationToken.None), requestCallback, stateObject);

        [Obsolete("EndResolve has been deprecated. Use EndGetHostEntry instead.")]
        public static IPHostEntry EndResolve(IAsyncResult asyncResult)
        {
            IPHostEntry ipHostEntry;

            try
            {
                ipHostEntry = TaskToAsyncResult.End<IPHostEntry>(asyncResult);
            }
            catch (SocketException ex)
            {
                object? asyncState = TaskToAsyncResult.Unwrap(asyncResult).AsyncState;

                IPAddress? address = asyncState switch
                {
                    IPAddress a => a,
                    KeyValuePair<IPAddress, AddressFamily> t => t.Key,
                    _ => null
                };

                if (address is null)
                    throw; // BeginResolve was called with a HostName, not an IPAddress

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, ex);

                ipHostEntry = CreateHostEntryForAddress(address);
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, ipHostEntry);
            return ipHostEntry;
        }

        private static IPHostEntry GetHostEntryCore(string hostName, AddressFamily addressFamily, long startingTimestamp = 0) =>
            (IPHostEntry)GetHostEntryOrAddressesCore(hostName, justAddresses: false, addressFamily, startingTimestamp);

        private static IPAddress[] GetHostAddressesCore(string hostName, AddressFamily addressFamily, long startingTimestamp = 0) =>
            (IPAddress[])GetHostEntryOrAddressesCore(hostName, justAddresses: true, addressFamily, startingTimestamp);

        private static object GetHostEntryOrAddressesCore(string hostName, bool justAddresses, AddressFamily addressFamily, long startingTimestamp = 0)
        {
            ValidateHostName(hostName);

            if (startingTimestamp == 0)
            {
                startingTimestamp = NameResolutionTelemetry.Log.BeforeResolution(hostName);
            }

            object result;
            try
            {
                string? newHostName = null;
                IPAddress[]? addresses = null;
                string[]? aliases = null;
                SocketError errorCode;
                int nativeErrorCode;

                if (s_hostsOverrides is not null && s_hostsOverrides.NameToHostEntry.TryGetValue(hostName, out IPHostEntry? overridesEntry))
                {
                    newHostName = overridesEntry.HostName;
                    addresses = CloneAddresses(overridesEntry.AddressList, addressFamily);
                    aliases = !justAddresses && overridesEntry.Aliases.Length != 0 ? (string[])overridesEntry.Aliases.Clone() : Array.Empty<string>();

                    errorCode = addresses.Length != 0 ? SocketError.Success : SocketError.HostNotFound;
                    nativeErrorCode = 0;
                }
                else
                {
                    errorCode = NameResolutionPal.TryGetAddrInfo(hostName, justAddresses, addressFamily, out newHostName, out aliases, out addresses, out nativeErrorCode);
                }

                if (errorCode != SocketError.Success)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(hostName, $"{hostName} DNS lookup failed with {errorCode}");
                    throw CreateException(errorCode, nativeErrorCode);
                }

                result = justAddresses ?
                    addresses :
                    new IPHostEntry
                    {
                        AddressList = addresses,
                        HostName = newHostName!,
                        Aliases = aliases!,
                    };
            }
            catch when (LogFailure(startingTimestamp))
            {
                Debug.Fail("LogFailure should return false");
                throw;
            }

            NameResolutionTelemetry.Log.AfterResolution(startingTimestamp, successful: true);

            return result;
        }

        private static IPHostEntry GetHostEntryCore(IPAddress address, AddressFamily addressFamily, long startingTimestamp = 0) =>
            (IPHostEntry)GetHostEntryOrAddressesCore(address, justAddresses: false, addressFamily, startingTimestamp);

        private static IPAddress[] GetHostAddressesCore(IPAddress address, AddressFamily addressFamily, long startingTimestamp) =>
            (IPAddress[])GetHostEntryOrAddressesCore(address, justAddresses: true, addressFamily, startingTimestamp);

        // Does internal IPAddress reverse and then forward lookups (for Legacy and current public methods).
        private static object GetHostEntryOrAddressesCore(IPAddress address, bool justAddresses, AddressFamily addressFamily, long startingTimestamp)
        {
            // Try to get the data for the host from its address.
            // We need to call getnameinfo first, because getaddrinfo w/ the ipaddress string
            // will only return that address and not the full list.

            // Do a reverse lookup to get the host name.
            if (startingTimestamp == 0)
            {
                startingTimestamp = NameResolutionTelemetry.Log.BeforeResolution(address);
            }

            string? name;
            string[]? aliases = null;
            IPAddress[]? addresses = null;
            SocketError errorCode;
            int nativeErrorCode;

            try
            {
                if (s_hostsOverrides is not null && s_hostsOverrides.AddressToHostEntry.TryGetValue(address, out IPHostEntry? overridesEntry))
                {
                    name = overridesEntry.HostName;
                    addresses = CloneAddresses(overridesEntry.AddressList, addressFamily);
                    aliases = !justAddresses && overridesEntry.Aliases.Length != 0 ? (string[])overridesEntry.Aliases.Clone() : Array.Empty<string>();

                    errorCode = addresses.Length != 0 ? SocketError.Success : SocketError.HostNotFound;
                    nativeErrorCode = 0;
                }
                else
                {
                    name = NameResolutionPal.TryGetNameInfo(address, out errorCode, out nativeErrorCode);
                }

                if (errorCode != SocketError.Success)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(address, $"{address} DNS lookup failed with {errorCode}");
                    throw CreateException(errorCode, nativeErrorCode);
                }

                Debug.Assert(name != null);
            }
            catch when (LogFailure(startingTimestamp))
            {
                Debug.Fail("LogFailure should return false");
                throw;
            }

            NameResolutionTelemetry.Log.AfterResolution(startingTimestamp, successful: true);

            // Do the forward lookup to get the IPs for that host name
            object result;
            try
            {
                if (addresses is null)
                {
                    startingTimestamp = NameResolutionTelemetry.Log.BeforeResolution(name);

                    errorCode = NameResolutionPal.TryGetAddrInfo(name, justAddresses, addressFamily, out name, out aliases, out addresses, out nativeErrorCode);
                    if (errorCode != SocketError.Success)
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(address, $"forward lookup for '{name}' failed with {errorCode}");
                    }

                    NameResolutionTelemetry.Log.AfterResolution(startingTimestamp, successful: true);
                }

                Debug.Assert(name is not null);
                Debug.Assert(justAddresses || aliases is not null);
                Debug.Assert(addresses is not null);

                result = justAddresses ?
                    addresses :
                    new IPHostEntry
                    {
                        HostName = name,
                        Aliases = aliases!,
                        AddressList = addresses
                    };
            }
            catch when (LogFailure(startingTimestamp))
            {
                Debug.Fail("LogFailure should return false");
                throw;
            }

            // One of three things happened:
            // 1. Success.
            // 2. There was a ptr record in dns, but not a corollary A/AAA record.
            // 3. The IP was a local (non-loopback) IP that resolved to a connection specific dns suffix.
            //    - Workaround, Check "Use this connection's dns suffix in dns registration" on that network
            //      adapter's advanced dns settings.
            // Return whatever we got.
            return result;
        }

        private static Task<IPHostEntry> GetHostEntryCoreAsync(string hostName, bool justReturnParsedIp, bool throwOnIIPAny, AddressFamily family, CancellationToken cancellationToken) =>
            (Task<IPHostEntry>)GetHostEntryOrAddressesCoreAsync(hostName, justReturnParsedIp, throwOnIIPAny, justAddresses: false, family, cancellationToken);

        // If hostName is an IPString and justReturnParsedIP==true then no reverse lookup will be attempted, but the original address is returned.
        private static Task GetHostEntryOrAddressesCoreAsync(string hostName, bool justReturnParsedIp, bool throwOnIIPAny, bool justAddresses, AddressFamily family, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(hostName);

            if (cancellationToken.IsCancellationRequested)
            {
                return justAddresses ?
                    Task.FromCanceled<IPAddress[]>(cancellationToken) :
                    Task.FromCanceled<IPHostEntry>(cancellationToken);
            }

            object asyncState;

            // See if it's an IP Address.
            if (IPAddress.TryParse(hostName, out IPAddress? ipAddress))
            {
                if (throwOnIIPAny && (ipAddress.Equals(IPAddress.Any) || ipAddress.Equals(IPAddress.IPv6Any)))
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(hostName, $"Invalid address '{ipAddress}'");
                    throw new ArgumentException(SR.net_invalid_ip_addr, nameof(hostName));
                }

                if (justReturnParsedIp)
                {
                    return justAddresses ?
                        Task.FromResult(family == AddressFamily.Unspecified || ipAddress.AddressFamily == family ? new[] { ipAddress } : Array.Empty<IPAddress>()) :
                        Task.FromResult(CreateHostEntryForAddress(ipAddress));
                }

                asyncState = family == AddressFamily.Unspecified ? ipAddress : new KeyValuePair<IPAddress, AddressFamily>(ipAddress, family);
            }
            else
            {
                if (NameResolutionPal.SupportsGetAddrInfoAsync)
                {
#pragma warning disable CS0162 // Unreachable code detected -- SupportsGetAddrInfoAsync is a constant on *nix.

                    // If the OS supports it and 'hostName' is not an IP Address, resolve the name asynchronously
                    // instead of calling the synchronous version in the ThreadPool.
                    // If it fails, we will fall back to ThreadPool as well.

                    ValidateHostName(hostName);

                    Task? t =
                        s_hostsOverrides is not null && s_hostsOverrides.NameToHostEntry.TryGetValue(hostName, out _) ? null : // to avoid code duplication, force sync path at the expense of an extra dictionary lookup
                        !NameResolutionTelemetry.Log.IsEnabled() ? NameResolutionPal.GetAddrInfoAsync(hostName, justAddresses, family, cancellationToken) :
                        justAddresses ? GetAddrInfoWithTelemetryAsync<IPAddress[]>(hostName, justAddresses, family, cancellationToken) :
                        GetAddrInfoWithTelemetryAsync<IPHostEntry>(hostName, justAddresses, family, cancellationToken);

                    // If async resolution started, return task to user, otherwise fall back to sync API on threadpool.
                    if (t != null)
                    {
                        return t;
                    }
#pragma warning restore CS0162
                }

                asyncState = family == AddressFamily.Unspecified ? hostName : new KeyValuePair<string, AddressFamily>(hostName, family);
            }

            if (justAddresses)
            {
                return RunAsync(static (s, startingTimestamp) => s switch
                {
                    string h => GetHostAddressesCore(h, AddressFamily.Unspecified, startingTimestamp),
                    KeyValuePair<string, AddressFamily> t => GetHostAddressesCore(t.Key, t.Value, startingTimestamp),
                    IPAddress a => GetHostAddressesCore(a, AddressFamily.Unspecified, startingTimestamp),
                    KeyValuePair<IPAddress, AddressFamily> t => GetHostAddressesCore(t.Key, t.Value, startingTimestamp),
                    _ => null
                }, asyncState, cancellationToken);
            }
            else
            {
                return RunAsync(static (s, startingTimestamp) => s switch
                {
                    string h => GetHostEntryCore(h, AddressFamily.Unspecified, startingTimestamp),
                    KeyValuePair<string, AddressFamily> t => GetHostEntryCore(t.Key, t.Value, startingTimestamp),
                    IPAddress a => GetHostEntryCore(a, AddressFamily.Unspecified, startingTimestamp),
                    KeyValuePair<IPAddress, AddressFamily> t => GetHostEntryCore(t.Key, t.Value, startingTimestamp),
                    _ => null
                }, asyncState, cancellationToken);
            }
        }

        private static Task<T>? GetAddrInfoWithTelemetryAsync<T>(string hostName, bool justAddresses, AddressFamily addressFamily, CancellationToken cancellationToken)
             where T : class
        {
            Debug.Assert(s_hostsOverrides is null || !s_hostsOverrides.NameToHostEntry.ContainsKey(hostName),
                "We shouldn't be using the async path if a hosts file was specified and contained this host");

            long startingTimestamp = Stopwatch.GetTimestamp();
            Task? task = NameResolutionPal.GetAddrInfoAsync(hostName, justAddresses, addressFamily, cancellationToken);

            if (task != null)
            {
                return CompleteAsync(task, hostName, startingTimestamp);
            }

            // If resolution even did not start don't bother with telemetry.
            // We will retry on thread-pool.
            return null;

            static async Task<T> CompleteAsync(Task task, string hostName, long startingTimestamp)
            {
                _  = NameResolutionTelemetry.Log.BeforeResolution(hostName);
                T? result = null;
                try
                {
                    result = await ((Task<T>)task).ConfigureAwait(false);
                    return result;
                }
                finally
                {
                    NameResolutionTelemetry.Log.AfterResolution(startingTimestamp, successful: result is not null);
                }
            }
        }

        private static IPHostEntry CreateHostEntryForAddress(IPAddress address) =>
            new IPHostEntry
            {
                HostName = address.ToString(),
                Aliases = Array.Empty<string>(),
                AddressList = new IPAddress[] { address }
            };

        private static void ValidateHostName(string hostName)
        {
            const int MaxHostName = 255;

            if (hostName.Length > MaxHostName ||
               (hostName.Length == MaxHostName && hostName[MaxHostName - 1] != '.')) // If 255 chars, the last one must be a dot.
            {
                throw new ArgumentOutOfRangeException(nameof(hostName),
                    SR.Format(SR.net_toolong, nameof(hostName), MaxHostName.ToString(NumberFormatInfo.CurrentInfo)));
            }
        }

        private static bool LogFailure(long startingTimestamp)
        {
            NameResolutionTelemetry.Log.AfterResolution(startingTimestamp, successful: false);
            return false;
        }

        /// <summary>Mapping from key to current task in flight for that key.</summary>
        private static readonly Dictionary<object, Task> s_tasks = new Dictionary<object, Task>();

        /// <summary>Queue the function to be invoked asynchronously.</summary>
        /// <remarks>
        /// Since this is doing synchronous work on a thread pool thread, we want to limit how many threads end up being
        /// blocked.  We could employ a semaphore to limit overall usage, but a common case is that DNS requests are made
        /// for only a handful of endpoints, and a reasonable compromise is to ensure that requests for a given host are
        /// serialized.  Once the data for that host is cached locally by the OS, the subsequent requests should all complete
        /// very quickly, and if the head-of-line request is taking a long time due to the connection to the server, we won't
        /// block lots of threads all getting data for that one host.  We also still want to issue the request to the OS, rather
        /// than having all concurrent requests for the same host share the exact same task, so that any shuffling of the results
        /// by the OS to enable round robin is still perceived.
        /// </remarks>
        private static Task<TResult> RunAsync<TResult>(Func<object, long, TResult> func, object key, CancellationToken cancellationToken)
        {
            long startingTimestamp = NameResolutionTelemetry.Log.BeforeResolution(key);

            Task<TResult>? task = null;

            lock (s_tasks)
            {
                // Get the previous task for this key, if there is one.
                s_tasks.TryGetValue(key, out Task? prevTask);
                prevTask ??= Task.CompletedTask;

                // Invoke the function in a queued work item when the previous task completes. Note that some callers expect the
                // returned task to have the key as the task's AsyncState.
                task = prevTask.ContinueWith(delegate
                {
                    Debug.Assert(!Monitor.IsEntered(s_tasks));
                    try
                    {
                        return func(key, startingTimestamp);
                    }
                    finally
                    {
                        // When the work is done, remove this key/task pair from the dictionary if this is still the current task.
                        // Because the work item is created and stored into both the local and the dictionary while the lock is
                        // held, and since we take the same lock here, inside this lock it's guaranteed to see the changes
                        // made by the call site.
                        lock (s_tasks)
                        {
                            ((ICollection<KeyValuePair<object, Task>>)s_tasks).Remove(new KeyValuePair<object, Task>(key!, task!));
                        }
                    }
                }, key, cancellationToken, TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);

                // If it's possible the task may end up getting canceled, it won't have a chance to remove itself from
                // the dictionary if it is canceled, so use a separate continuation to do so.
                if (cancellationToken.CanBeCanceled)
                {
                    task.ContinueWith((task, key) =>
                    {
                        lock (s_tasks)
                        {
                            ((ICollection<KeyValuePair<object, Task>>)s_tasks).Remove(new KeyValuePair<object, Task>(key!, task));
                        }
                    }, key, CancellationToken.None, TaskContinuationOptions.OnlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }

                // Finally, store the task into the dictionary as the current task for this key.
                s_tasks[key] = task;
            }

            return task;
        }

        private static SocketException CreateException(SocketError error, int nativeError) =>
            new SocketException((int)error) { HResult = nativeError };

        /// <summary>
        /// Creates an array of the addresses in <paramref name="addresses"/> that match the specified <paramref name="addressFamily"/>.
        /// If the <paramref name="addressFamily"/> is <see cref="AddressFamily.Unspecified"/>, then all addresses are returned.
        /// </summary>
        private static IPAddress[] CloneAddresses(IPAddress[] addresses, AddressFamily addressFamily)
        {
            // Count how many addresses match the specified address family.
            int count = 0;
            foreach (IPAddress address in addresses)
            {
                if (IncludeAddress(address, addressFamily))
                {
                    count++;
                }
            }

            // If none match, return an empty array.
            if (count == 0)
            {
                return Array.Empty<IPAddress>();
            }

            var result = new IPAddress[count];
            int i = 0;
            Span<byte> bytes = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
            foreach (IPAddress address in addresses)
            {
                if (IncludeAddress(address, addressFamily))
                {
                    // Clone the IPAddress before handing it out, as it's mutable.
                    address.TryWriteBytes(bytes, out int bytesWritten);
                    Debug.Assert(bytesWritten > 0);
                    result[i++] = new IPAddress(bytes.Slice(0, bytesWritten));
                }
            }

            return result;

            static bool IncludeAddress(IPAddress address, AddressFamily addressFamily) =>
                (addressFamily is AddressFamily.Unspecified || addressFamily == address.AddressFamily) &&
                ((address.AddressFamily is AddressFamily.InterNetwork && SocketProtocolSupportPal.OSSupportsIPv4) ||
                 (address.AddressFamily is AddressFamily.InterNetworkV6 && SocketProtocolSupportPal.OSSupportsIPv6));
        }

        /// <summary>Loads hosts overrides if any were specified.</summary>
        /// <remarks>The parsed overrides, or null if none were specified or valid.</remarks>
        private static HostsOverridesLookup? LoadHostsOverrides()
        {
            // NOTE:
            // Several loops in this implementation could be worst case O(N^2) in both time and allocation,
            // e.g. every time we encounter a new alias for an address, we'll grow the alias list for that address by 1.
            // However, a) the hosts file is trusted (if it weren't, there would already be problems as it would enable
            // an untrusted file to dictate the destination of network traffic), and b) the hosts file is expected
            // to be small, in which case O(N^2) is fine and actually better than the overhead of more temporary O(1) data structures.

            // Read the path to the overrides file.  This file is expected to be in the same format as the
            // a hosts file, with one line per entry, where each line consists of an IP address followed by
            // at least one space or tab, followed by one or more host names, each separated by one or more
            // spaces or tabs.  Blank lines and any text on a line starting with and after a '#' are ignored.
            if (Environment.GetEnvironmentVariable(HostsFileEnvVarName) is string path)
            {
                // Read the hosts file. Ignore any failures, e.g. if the file doesn't exist or is malformed, and just return null.
                string hosts;
                try
                {
                    hosts = File.ReadAllText(path);
                }
                catch (Exception ex)
                {
                    if (NameResolutionTelemetry.Log.IsEnabled()) NameResolutionTelemetry.Log.HostsOverrideError(path, $"Unable to load file. {ex.Message}");
                    return null;
                }

                // Create the result object containing the forward and reverse lookup dictionaries.
                var result = new HostsOverridesLookup();

                // Process each line.
                ReadOnlySpan<char> spaceChars = stackalloc char[] { ' ', '\t' };
                foreach (ReadOnlySpan<char> line in hosts.AsSpan().EnumerateLines())
                {
                    scoped ReadOnlySpan<char> span = line;

                    // Trim off comments
                    int commentPos = span.IndexOf('#');
                    if (commentPos >= 0)
                    {
                        span = span.Slice(0, commentPos);
                    }
                    span = span.Trim(spaceChars);
                    if (span.IsEmpty)
                    {
                        // Skip blank and comment-only lines
                        continue;
                    }

                    // See if there's any remaining space in the line.  If there isn't,
                    // then at most we have an address with no host name, and we'll ignore it.
                    int spacePos = span.IndexOfAny(spaceChars);
                    if (spacePos < 0)
                    {
                        if (NameResolutionTelemetry.Log.IsEnabled()) NameResolutionTelemetry.Log.HostsOverrideError(path, $"Invalid line '{line}'");
                        continue;
                    }

                    // We parse the address to ensure it's valid and ignore the line if it's not.
                    if (!IPAddress.TryParse(span.Slice(0, spacePos), out IPAddress? address))
                    {
                        if (NameResolutionTelemetry.Log.IsEnabled()) NameResolutionTelemetry.Log.HostsOverrideError(path, $"Unable to parse '{span.Slice(0, spacePos)}' as IPAddress");
                        continue;
                    }

                    // Parse each host name after it.
                    span = span.Slice(spacePos).TrimStart(spaceChars);
                    while (!span.IsEmpty)
                    {
                        // Find the end of the name.
                        int end = span.IndexOfAny(spaceChars);
                        if (end < 0)
                        {
                            end = span.Length;
                        }

                        // Parse out the name and ensure it's a valid DNS name.
                        string hostName = span.Slice(0, end).ToString();
                        span = span.Slice(end).TrimStart(spaceChars);
                        if (Uri.CheckHostName(hostName) != UriHostNameType.Dns)
                        {
                            if (NameResolutionTelemetry.Log.IsEnabled()) NameResolutionTelemetry.Log.HostsOverrideError(path, $"Invalid DNS hostname '{hostName}'");
                            continue;
                        }

                        // Create or augment the forward lookup entry for this name to IP.  If the entry already exists,
                        // add the IPAddress as an additional one to the AddressList if it's not already there.
                        ref IPHostEntry? nameEntry = ref CollectionsMarshal.GetValueRefOrAddDefault(result.NameToHostEntry, hostName, out _);
                        if (nameEntry is null)
                        {
                            nameEntry = new IPHostEntry() { HostName = hostName, AddressList = new[] { address }, Aliases = Array.Empty<string>() };
                        }
                        else
                        {
                            nameEntry.AddressList = AddIfMissing(nameEntry.AddressList, address);
                        }

                        // Create or augment the reverse lookup entry for this IP to name.  If the entry already exists,
                        // add the hostname as an alias if it's not already the hostname or included as an alias.
                        ref IPHostEntry? addressEntry = ref CollectionsMarshal.GetValueRefOrAddDefault(result.AddressToHostEntry, address, out _);
                        if (addressEntry is null)
                        {
                            addressEntry = new IPHostEntry() { HostName = hostName, AddressList = new[] { address }, Aliases = Array.Empty<string>() };
                        }
                        else if (!hostName.Equals(addressEntry.HostName, StringComparison.OrdinalIgnoreCase))
                        {
                            addressEntry.Aliases = AddIfMissing(addressEntry.Aliases, hostName, StringComparer.OrdinalIgnoreCase);
                        }

                        // Log the mapping.
                        if (NameResolutionTelemetry.Log.IsEnabled()) NameResolutionTelemetry.Log.HostsOverrideMappingAdded(hostName, address.ToString());

                        // Creates a new array with the specified value added to the end of the input array
                        // if it's not already present, OrdinalIgnoreCase.
                        static T[] AddIfMissing<T>(T[] array, T value, IEqualityComparer<T>? comparer = null)
                        {
                            comparer ??= EqualityComparer<T>.Default;

                            foreach (T item in array)
                            {
                                if (comparer.Equals(item, value))
                                {
                                    return array;
                                }
                            }

                            T[] newArray = new T[array.Length + 1];
                            Array.Copy(array, newArray, array.Length);
                            newArray[^1] = value;
                            return newArray;
                        }
                    }
                }

                // If we got any valid entries, return them.
                if (result.NameToHostEntry.Count != 0)
                {
                    Debug.Assert(result.AddressToHostEntry.Count != 0);
                    return result;
                }
            }

            // No valid overrides were specified.
            return null;
        }

        /// <summary>Hosts overrides lookup tables, populated from a custom hosts file if one was specified.</summary>
        private sealed class HostsOverridesLookup
        {
            /// <summary>Mapping from a string host name to the corresponding <see cref="IPHostEntry"/>.</summary>
            public Dictionary<string, IPHostEntry> NameToHostEntry { get; } = new Dictionary<string, IPHostEntry>(StringComparer.OrdinalIgnoreCase);

            /// <summary>Mapping from an <see cref="IPAddress"/> to the corresponding <see cref="IPHostEntry"/>.</summary>
            public Dictionary<IPAddress, IPHostEntry> AddressToHostEntry { get; } = new Dictionary<IPAddress, IPHostEntry>();
        }
    }
}
