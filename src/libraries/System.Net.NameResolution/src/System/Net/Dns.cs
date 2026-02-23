// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;

namespace System.Net
{
    /// <summary>Provides simple domain name resolution functionality.</summary>
    public static class Dns
    {
        /// <summary>Gets the host name of the local machine.</summary>
        public static string GetHostName()
        {
            NameResolutionActivity activity = NameResolutionTelemetry.Log.BeforeResolution(string.Empty);

            string name;
            try
            {
                name = NameResolutionPal.GetHostName();
            }
            catch (Exception ex) when (LogFailure(string.Empty, activity, ex))
            {
                Debug.Fail("LogFailure should return false");
                throw;
            }

            NameResolutionTelemetry.Log.AfterResolution(string.Empty, activity, answer: name);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, name);
            return name;
        }

        public static IPHostEntry GetHostEntry(IPAddress address)
        {
            if (OperatingSystem.IsWasi()) throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185

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
            if (NameResolutionPal.SupportsGetNameInfo && IPAddress.TryParse(hostNameOrAddress, out IPAddress? address))
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
            if (OperatingSystem.IsWasi()) throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185

            ArgumentNullException.ThrowIfNull(address);

            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(address, $"Invalid address '{address}'");
                throw new ArgumentException(SR.net_invalid_ip_addr, nameof(address));
            }

            return RunAsync(static (s, activity) => {
                if (OperatingSystem.IsWasi()) throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185

                IPHostEntry ipHostEntry = GetHostEntryCore((IPAddress)s, AddressFamily.Unspecified, activity);
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
            if (OperatingSystem.IsWasi()) throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185

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
            if (OperatingSystem.IsWasi()) throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185

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
            if (OperatingSystem.IsWasi()) throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185

            ArgumentNullException.ThrowIfNull(asyncResult);

            return TaskToAsyncResult.End<IPHostEntry>(asyncResult);
        }

        [Obsolete("GetHostByAddress has been deprecated. Use GetHostEntry instead.")]
        public static IPHostEntry GetHostByAddress(string address)
        {
            if (OperatingSystem.IsWasi()) throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185

            ArgumentNullException.ThrowIfNull(address);

            IPHostEntry ipHostEntry = GetHostEntryCore(IPAddress.Parse(address), AddressFamily.Unspecified);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(address, ipHostEntry);
            return ipHostEntry;
        }

        [Obsolete("GetHostByAddress has been deprecated. Use GetHostEntry instead.")]
        public static IPHostEntry GetHostByAddress(IPAddress address)
        {
            if (OperatingSystem.IsWasi()) throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185

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
            if (NameResolutionPal.SupportsGetNameInfo && IPAddress.TryParse(hostName, out IPAddress? address) &&
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
            if (OperatingSystem.IsWasi()) throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185

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

        private static IPHostEntry GetHostEntryCore(string hostName, AddressFamily addressFamily, NameResolutionActivity? activityOrDefault = default) =>
            (IPHostEntry)GetHostEntryOrAddressesCore(hostName, justAddresses: false, addressFamily, activityOrDefault);

        private static IPAddress[] GetHostAddressesCore(string hostName, AddressFamily addressFamily, NameResolutionActivity? activityOrDefault = default) =>
            (IPAddress[])GetHostEntryOrAddressesCore(hostName, justAddresses: true, addressFamily, activityOrDefault);

        private static bool ValidateAddressFamily(ref AddressFamily addressFamily, string hostName, bool justAddresses, [NotNullWhen(false)] out object? resultOnFailure)
        {
            if (!SocketProtocolSupportPal.OSSupportsIPv6)
            {
                if (addressFamily == AddressFamily.InterNetworkV6)
                {
                    // The caller requested IPv6, but the OS doesn't support it; return an empty result.
                    IPAddress[] addresses = Array.Empty<IPAddress>();
                    resultOnFailure = justAddresses ? (object)
                        addresses :
                        new IPHostEntry
                        {
                            AddressList = addresses,
                            HostName = hostName,
                            Aliases = Array.Empty<string>()
                        };
                    return false;
                }
                else if (addressFamily == AddressFamily.Unspecified)
                {
                    // Narrow the query to IPv4.
                    addressFamily = AddressFamily.InterNetwork;
                }
            }

            resultOnFailure = null;
            return true;
        }

        private const string Localhost = "localhost";
        private const string InvalidDomain = "invalid";

        /// <summary>
        /// Checks if the given host name matches a reserved name or is a subdomain of it.
        /// For example, IsReservedName("foo.localhost", "localhost") returns true.
        /// Also handles trailing dots: IsReservedName("foo.localhost.", "localhost") returns true.
        /// Returns false for malformed hostnames (starting with dot or containing consecutive dots).
        /// </summary>
        private static bool IsReservedName(string hostName, string reservedName)
        {
            // Reject malformed hostnames - let OS resolver handle them (and reject them)
            if (hostName.StartsWith('.') || hostName.Contains("..", StringComparison.Ordinal))
            {
                return false;
            }

            // Strip trailing dot if present (DNS root notation)
            ReadOnlySpan<char> hostSpan = hostName.AsSpan();
            if (hostSpan.EndsWith('.'))
            {
                hostSpan = hostSpan.Slice(0, hostSpan.Length - 1);
            }

            // Matches "reservedName" exactly, or "*.reservedName" (subdomain)
            return hostSpan.EndsWith(reservedName, StringComparison.OrdinalIgnoreCase) &&
                   (hostSpan.Length == reservedName.Length ||
                    hostSpan[hostSpan.Length - reservedName.Length - 1] == '.');
        }

        /// <summary>
        /// Checks if the given host name is a subdomain of localhost (e.g., "foo.localhost").
        /// Plain "localhost" or "localhost." returns false.
        /// </summary>
        private static bool IsLocalhostSubdomain(string hostName)
        {
            // Strip trailing dot for length comparison
            int length = hostName.Length;
            if (hostName.EndsWith('.'))
            {
                length--;
            }

            // Must be longer than "localhost" (not just equal with trailing dot)
            return length > Localhost.Length && IsReservedName(hostName, Localhost);
        }

        /// <summary>
        /// Tries to handle RFC 6761 "invalid" domain names.
        /// Returns true if the host name is an invalid domain (exception will be set).
        /// </summary>
        private static bool TryHandleRfc6761InvalidDomain(string hostName, out SocketException? exception)
        {
            // RFC 6761 Section 6.4: "invalid" and "*.invalid" must always return NXDOMAIN.
            if (IsReservedName(hostName, InvalidDomain))
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(hostName, "RFC 6761: Returning NXDOMAIN for 'invalid' domain");
                exception = new SocketException((int)SocketError.HostNotFound);
                return true;
            }

            exception = null;
            return false;
        }

        private static object GetHostEntryOrAddressesCore(string hostName, bool justAddresses, AddressFamily addressFamily, NameResolutionActivity? activityOrDefault = default)
        {
            ValidateHostName(hostName);

            if (!ValidateAddressFamily(ref addressFamily, hostName, justAddresses, out object? resultOnFailure))
            {
                Debug.Assert(!activityOrDefault.HasValue);
                return resultOnFailure;
            }

            // NameResolutionActivity may have already been set if we're being called from RunAsync.
            NameResolutionActivity activity = activityOrDefault ?? NameResolutionTelemetry.Log.BeforeResolution(hostName);

            // RFC 6761 Section 6.4: "invalid" domains must return NXDOMAIN.
            if (TryHandleRfc6761InvalidDomain(hostName, out SocketException? invalidDomainException))
            {
                NameResolutionTelemetry.Log.AfterResolution(hostName, activity, answer: null, exception: invalidDomainException);
                throw invalidDomainException!;
            }

            bool fallbackToLocalhost = false;
            object? result = null;
            try
            {
                SocketError errorCode = NameResolutionPal.TryGetAddrInfo(hostName, justAddresses, addressFamily, out string? newHostName, out string[] aliases, out IPAddress[] addresses, out int nativeErrorCode);

                if (errorCode != SocketError.Success)
                {
                    // RFC 6761 Section 6.3: If localhost subdomain fails, fall back to resolving plain "localhost".
                    if (IsLocalhostSubdomain(hostName))
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(hostName, "RFC 6761: Localhost subdomain resolution failed, falling back to 'localhost'");
                        NameResolutionTelemetry.Log.AfterResolution(hostName, activity, answer: null, exception: CreateException(errorCode, nativeErrorCode));
                        fallbackToLocalhost = true;
                    }
                    else
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(hostName, $"{hostName} DNS lookup failed with {errorCode}");
                        throw CreateException(errorCode, nativeErrorCode);
                    }
                }
                else if (addresses.Length == 0 && IsLocalhostSubdomain(hostName))
                {
                    // RFC 6761 Section 6.3: If localhost subdomain returns empty addresses, fall back to plain "localhost".
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(hostName, "RFC 6761: Localhost subdomain returned empty, falling back to 'localhost'");
                    NameResolutionTelemetry.Log.AfterResolution(hostName, activity, answer: justAddresses ? addresses : (object)new IPHostEntry { AddressList = addresses, HostName = newHostName!, Aliases = aliases }, exception: null);
                    fallbackToLocalhost = true;
                }

                if (!fallbackToLocalhost)
                {
                    result = justAddresses ? (object)
                        addresses :
                        new IPHostEntry
                        {
                            AddressList = addresses,
                            HostName = newHostName!,
                            Aliases = aliases
                        };
                }
            }
            catch (Exception ex) when (LogFailure(hostName, activity, ex))
            {
                Debug.Fail("LogFailure should return false");
                throw;
            }

            if (fallbackToLocalhost)
            {
                return GetHostEntryOrAddressesCore(Localhost, justAddresses, addressFamily);
            }

            Debug.Assert(result is not null);
            NameResolutionTelemetry.Log.AfterResolution(hostName, activity, answer: result);

            return result;
        }

        private static IPHostEntry GetHostEntryCore(IPAddress address, AddressFamily addressFamily, NameResolutionActivity? activityOrDefault = default) =>
            (IPHostEntry)GetHostEntryOrAddressesCore(address, justAddresses: false, addressFamily, activityOrDefault);

        private static IPAddress[] GetHostAddressesCore(IPAddress address, AddressFamily addressFamily, NameResolutionActivity? activityOrDefault = default) =>
            (IPAddress[])GetHostEntryOrAddressesCore(address, justAddresses: true, addressFamily, activityOrDefault);

        // Does internal IPAddress reverse and then forward lookups (for Legacy and current public methods).
        private static object GetHostEntryOrAddressesCore(IPAddress address, bool justAddresses, AddressFamily addressFamily, NameResolutionActivity? activityOrDefault = default)
        {
            if (OperatingSystem.IsWasi()) throw new PlatformNotSupportedException(); // TODO remove with https://github.com/dotnet/runtime/pull/107185

            // Try to get the data for the host from its address.
            // We need to call getnameinfo first, because getaddrinfo w/ the ipaddress string
            // will only return that address and not the full list.

            // Do a reverse lookup to get the host name.
            // NameResolutionActivity may have already been set if we're being called from RunAsync.
            NameResolutionActivity activity = activityOrDefault ?? NameResolutionTelemetry.Log.BeforeResolution(address);

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
            catch (Exception ex) when (LogFailure(address, activity, ex))
            {
                Debug.Fail("LogFailure should return false");
                throw;
            }

            NameResolutionTelemetry.Log.AfterResolution(address, activity, answer: name);

            if (!ValidateAddressFamily(ref addressFamily, name, justAddresses, out object? resultOnFailure))
            {
                return resultOnFailure;
            }

            // Do the forward lookup to get the IPs for that host name
            activity = NameResolutionTelemetry.Log.BeforeResolution(name);

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
            catch (Exception ex) when (LogFailure(name, activity, ex))
            {
                Debug.Fail("LogFailure should return false");
                throw;
            }

            NameResolutionTelemetry.Log.AfterResolution(name, activity, answer: result);

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

            if (!ValidateAddressFamily(ref family, hostName, justAddresses, out object? resultOnFailure))
            {
                return justAddresses ? (Task)
                    Task.FromResult((IPAddress[])resultOnFailure) :
                    Task.FromResult((IPHostEntry)resultOnFailure);
            }

            object asyncState;

            // See if it's an IP Address.
            if (NameResolutionPal.SupportsGetNameInfo && IPAddress.TryParse(hostName, out IPAddress? ipAddress))
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
                // Validate hostname before any processing
                ValidateHostName(hostName);

                // RFC 6761 Section 6.4: "invalid" domains must return NXDOMAIN.
                if (TryHandleRfc6761InvalidDomain(hostName, out SocketException? invalidDomainException))
                {
                    NameResolutionActivity activity = NameResolutionTelemetry.Log.BeforeResolution(hostName);
                    NameResolutionTelemetry.Log.AfterResolution(hostName, activity, answer: null, exception: invalidDomainException);
                    return justAddresses ? (Task)
                        Task.FromException<IPAddress[]>(invalidDomainException!) :
                        Task.FromException<IPHostEntry>(invalidDomainException!);
                }

                // For localhost subdomains (RFC 6761 Section 6.3), we try the OS resolver first.
                // If it fails or returns empty, we fall back to resolving plain "localhost".
                // This fallback logic is handled in GetHostEntryOrAddressesCore and GetAddrInfoWithTelemetryAsync.

                if (NameResolutionPal.SupportsGetAddrInfoAsync)
                {
#pragma warning disable CS0162 // Unreachable code detected -- SupportsGetAddrInfoAsync is a constant on *nix.

                    // If the OS supports it and 'hostName' is not an IP Address, resolve the name asynchronously
                    // instead of calling the synchronous version in the ThreadPool.
                    // If it fails, we will fall back to ThreadPool as well.

                    // Always use the telemetry-enabled path for localhost subdomains to ensure fallback handling.
                    // For other hostnames, use the non-telemetry path if diagnostics are disabled.
                    bool isLocalhostSubdomain = IsLocalhostSubdomain(hostName);
                    Task? t;
                    if (NameResolutionTelemetry.AnyDiagnosticsEnabled() || isLocalhostSubdomain)
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
                return RunAsync(static (s, activity) => s switch
                {
                    string h => GetHostAddressesCore(h, AddressFamily.Unspecified, activity),
                    KeyValuePair<string, AddressFamily> t => GetHostAddressesCore(t.Key, t.Value, activity),
                    IPAddress a => GetHostAddressesCore(a, AddressFamily.Unspecified, activity),
                    KeyValuePair<IPAddress, AddressFamily> t => GetHostAddressesCore(t.Key, t.Value, activity),
                    _ => null
                }, asyncState, cancellationToken);
            }
            else
            {
                return RunAsync(static (s, activity) => s switch
                {
                    string h => GetHostEntryCore(h, AddressFamily.Unspecified, activity),
                    KeyValuePair<string, AddressFamily> t => GetHostEntryCore(t.Key, t.Value, activity),
                    IPAddress a => GetHostEntryCore(a, AddressFamily.Unspecified, activity),
                    KeyValuePair<IPAddress, AddressFamily> t => GetHostEntryCore(t.Key, t.Value, activity),
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
                bool isLocalhostSubdomain = IsLocalhostSubdomain(hostName);
                return CompleteAsync(task, hostName, justAddresses, addressFamily, isLocalhostSubdomain, startingTimestamp, cancellationToken);
            }

            // If resolution even did not start don't bother with telemetry.
            // We will retry on thread-pool.
            return null;

            static async Task<T> CompleteAsync(Task task, string hostName, bool justAddresses, AddressFamily addressFamily, bool isLocalhostSubdomain, long startingTimeStamp, CancellationToken cancellationToken)
            {
                NameResolutionActivity activity = NameResolutionTelemetry.Log.BeforeResolution(hostName, startingTimeStamp);
                Exception? exception = null;
                T? result = null;
                bool fallbackOccurred = false;
                try
                {
                    result = await ((Task<T>)task).ConfigureAwait(false);

                    // RFC 6761 Section 6.3: If localhost subdomain returns empty addresses, fall back to plain "localhost".
                    if (isLocalhostSubdomain && result is IPAddress[] addresses && addresses.Length == 0)
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(hostName, "RFC 6761: Localhost subdomain returned empty, falling back to 'localhost'");
                        NameResolutionTelemetry.Log.AfterResolution(hostName, activity, answer: result, exception: null);
                        fallbackOccurred = true;

                        // result is IPAddress[] so justAddresses is guaranteed true here.
                        return await ((Task<T>)(Task)Dns.GetHostAddressesAsync(Localhost, addressFamily, cancellationToken)).ConfigureAwait(false);
                    }

                    if (isLocalhostSubdomain && result is IPHostEntry entry && entry.AddressList.Length == 0)
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(hostName, "RFC 6761: Localhost subdomain returned empty, falling back to 'localhost'");
                        NameResolutionTelemetry.Log.AfterResolution(hostName, activity, answer: result, exception: null);
                        fallbackOccurred = true;

                        // result is IPHostEntry so justAddresses is guaranteed false here.
                        return await ((Task<T>)(Task)Dns.GetHostEntryAsync(Localhost, addressFamily, cancellationToken)).ConfigureAwait(false);
                    }

                    return result;
                }
                catch (SocketException ex) when (isLocalhostSubdomain && !fallbackOccurred)
                {
                    // RFC 6761 Section 6.3: If localhost subdomain fails, fall back to resolving plain "localhost".
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(hostName, "RFC 6761: Localhost subdomain resolution failed, falling back to 'localhost'");
                    NameResolutionTelemetry.Log.AfterResolution(hostName, activity, answer: null, exception: ex);
                    fallbackOccurred = true;

                    return await ((Task<T>)(justAddresses
                        ? (Task)Dns.GetHostAddressesAsync(Localhost, addressFamily, cancellationToken)
                        : Dns.GetHostEntryAsync(Localhost, addressFamily, cancellationToken))).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exception = ex;
                    throw;
                }
                finally
                {
                    if (!fallbackOccurred)
                    {
                        NameResolutionTelemetry.Log.AfterResolution(hostName, activity, answer: result, exception: exception);
                    }
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

        private static bool LogFailure(object hostNameOrAddress, in NameResolutionActivity activity, Exception exception)
        {
            NameResolutionTelemetry.Log.AfterResolution(hostNameOrAddress, activity, answer: null, exception: exception);
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
        private static Task<TResult> RunAsync<TResult>(Func<object, NameResolutionActivity, TResult> func, object key, CancellationToken cancellationToken)
        {
            bool tracingEnabled = NameResolutionActivity.IsTracingEnabled();
            Activity? activityToRestore = tracingEnabled ? Activity.Current : null;
            NameResolutionActivity activity = NameResolutionTelemetry.Log.BeforeResolution(key);
            if (tracingEnabled)
            {
                // Do not overwrite Activity.Current in the caller's ExecutionContext.
                Activity.Current = activityToRestore;
            }

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
                        return func(key, activity);
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
                        // Since it was canceled, func(..) had not executed and call AfterResolution it needs to be called here.
                        NameResolutionTelemetry.Log.AfterResolution(key!, activity, new OperationCanceledException());
                    }, key, CancellationToken.None, TaskContinuationOptions.OnlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }

                // Finally, store the task into the dictionary as the current task for this key.
                s_tasks[key] = task;
            }

            return task;
        }

        private static SocketException CreateException(SocketError error, int nativeError) =>
            new SocketException((int)error) { HResult = nativeError };
    }
}
