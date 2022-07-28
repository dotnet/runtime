// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Internals;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    /// <summary>Provides simple domain name resolution functionality.</summary>
    public static class Dns
    {
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

            return RunAsync(static (s, stopwatch) => {
                IPHostEntry ipHostEntry = GetHostEntryCore((IPAddress)s, AddressFamily.Unspecified, stopwatch);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info((IPAddress)s, $"{ipHostEntry} with {ipHostEntry.AddressList.Length} entries");
                return ipHostEntry;
            }, address, CancellationToken.None);
        }

        public static IAsyncResult BeginGetHostEntry(IPAddress address, AsyncCallback? requestCallback, object? stateObject) =>
            TaskToApm.Begin(GetHostEntryAsync(address), requestCallback, stateObject);

        public static IAsyncResult BeginGetHostEntry(string hostNameOrAddress, AsyncCallback? requestCallback, object? stateObject) =>
            TaskToApm.Begin(GetHostEntryAsync(hostNameOrAddress), requestCallback, stateObject);

        public static IPHostEntry EndGetHostEntry(IAsyncResult asyncResult)
        {
            ArgumentNullException.ThrowIfNull(asyncResult);

            return TaskToApm.End<IPHostEntry>(asyncResult);
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
            TaskToApm.Begin(GetHostAddressesAsync(hostNameOrAddress), requestCallback, state);

        public static IPAddress[] EndGetHostAddresses(IAsyncResult asyncResult)
        {
            ArgumentNullException.ThrowIfNull(asyncResult);

            return TaskToApm.End<IPAddress[]>(asyncResult);
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
            TaskToApm.Begin(GetHostEntryCoreAsync(hostName, justReturnParsedIp: true, throwOnIIPAny: true, AddressFamily.Unspecified, CancellationToken.None), requestCallback, stateObject);

        [Obsolete("EndGetHostByName has been deprecated. Use EndGetHostEntry instead.")]
        public static IPHostEntry EndGetHostByName(IAsyncResult asyncResult)
        {
            ArgumentNullException.ThrowIfNull(asyncResult);

            return TaskToApm.End<IPHostEntry>(asyncResult);
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
            TaskToApm.Begin(GetHostEntryCoreAsync(hostName, justReturnParsedIp: false, throwOnIIPAny: false, AddressFamily.Unspecified, CancellationToken.None), requestCallback, stateObject);

        [Obsolete("EndResolve has been deprecated. Use EndGetHostEntry instead.")]
        public static IPHostEntry EndResolve(IAsyncResult asyncResult)
        {
            IPHostEntry ipHostEntry;

            try
            {
                ipHostEntry = TaskToApm.End<IPHostEntry>(asyncResult);
            }
            catch (SocketException ex)
            {
                object? asyncState = asyncResult switch
                {
                    Task t => t.AsyncState,
                    TaskToApm.TaskAsyncResult twar => twar._task.AsyncState,
                    _ => null
                };

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
                SocketError errorCode = NameResolutionPal.TryGetAddrInfo(hostName, justAddresses, addressFamily, out string? newHostName, out string[] aliases, out IPAddress[] addresses, out int nativeErrorCode);

                if (errorCode != SocketError.Success)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(hostName, $"{hostName} DNS lookup failed with {errorCode}");
                    throw CreateException(errorCode, nativeErrorCode);
                }

                result = justAddresses ? (object)
                    addresses :
                    new IPHostEntry
                    {
                        AddressList = addresses,
                        HostName = newHostName!,
                        Aliases = aliases
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

            SocketError errorCode;
            string? name;
            try
            {
                name = NameResolutionPal.TryGetNameInfo(address, out errorCode, out int nativeErrorCode);
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
            startingTimestamp = NameResolutionTelemetry.Log.BeforeResolution(name);

            object result;
            try
            {
                errorCode = NameResolutionPal.TryGetAddrInfo(name, justAddresses, addressFamily, out string? hostName, out string[] aliases, out IPAddress[] addresses, out int nativeErrorCode);

                if (errorCode != SocketError.Success)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(address, $"forward lookup for '{name}' failed with {errorCode}");
                }

                result = justAddresses ?
                    (object)addresses :
                    new IPHostEntry
                    {
                        HostName = hostName!,
                        Aliases = aliases,
                        AddressList = addresses
                    };
            }
            catch when (LogFailure(startingTimestamp))
            {
                Debug.Fail("LogFailure should return false");
                throw;
            }

            NameResolutionTelemetry.Log.AfterResolution(startingTimestamp, successful: true);

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
                return justAddresses ? (Task)
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
                    return justAddresses ? (Task)
                        Task.FromResult(family == AddressFamily.Unspecified || ipAddress.AddressFamily == family ? new[] { ipAddress } : Array.Empty<IPAddress>()) :
                        Task.FromResult(CreateHostEntryForAddress(ipAddress));
                }

                asyncState = family == AddressFamily.Unspecified ? (object)ipAddress : new KeyValuePair<IPAddress, AddressFamily>(ipAddress, family);
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

                    Task? t;
                    if (NameResolutionTelemetry.Log.IsEnabled())
                    {
                        t = justAddresses
                            ? GetAddrInfoWithTelemetryAsync<IPAddress[]>(hostName, justAddresses, family, cancellationToken)
                            : GetAddrInfoWithTelemetryAsync<IPHostEntry>(hostName, justAddresses, family, cancellationToken);

                    }
                    else
                    {
                        t = NameResolutionPal.GetAddrInfoAsync(hostName, justAddresses, family, cancellationToken);
                    }

                    // If async resolution started, return task to user, otherwise fall back to sync API on threadpool.
                    if (t != null)
                    {
                        return t;
                    }
#pragma warning restore CS0162
                }

                asyncState = family == AddressFamily.Unspecified ? (object)hostName : new KeyValuePair<string, AddressFamily>(hostName, family);
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

        private static Exception CreateException(SocketError error, int nativeError)
        {
            SocketException e = new SocketException((int)error);
            e.HResult = nativeError;
            return e;
        }
    }
}
