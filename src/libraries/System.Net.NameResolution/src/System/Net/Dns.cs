// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Net.Internals;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace System.Net
{
    /// <summary>Provides simple domain name resolution functionality.</summary>
    public static class Dns
    {
        /// <summary>Gets the host name of the local machine.</summary>
        public static string GetHostName()
        {
            NameResolutionPal.EnsureSocketsAreInitialized();

            ValueStopwatch stopwatch = NameResolutionTelemetry.Log.BeforeResolution(string.Empty);

            string name;
            try
            {
                name = NameResolutionPal.GetHostName();
            }
            catch when (LogFailure(stopwatch))
            {
                Debug.Fail("LogFailure should return false");
                throw;
            }

            if (NameResolutionTelemetry.Log.IsEnabled())
                NameResolutionTelemetry.Log.AfterResolution(stopwatch, successful: true);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, name);
            return name;
        }

        public static IPHostEntry GetHostEntry(IPAddress address)
        {
            NameResolutionPal.EnsureSocketsAreInitialized();

            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(address, $"Invalid address '{address}'");
                throw new ArgumentException(SR.Format(SR.net_invalid_ip_addr, nameof(address)));
            }

            IPHostEntry ipHostEntry = GetHostEntryCore(address);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(address, $"{ipHostEntry} with {ipHostEntry.AddressList.Length} entries");
            return ipHostEntry;
        }

        public static IPHostEntry GetHostEntry(string hostNameOrAddress)
        {
            NameResolutionPal.EnsureSocketsAreInitialized();

            if (hostNameOrAddress is null)
            {
                throw new ArgumentNullException(nameof(hostNameOrAddress));
            }

            // See if it's an IP Address.
            IPHostEntry ipHostEntry;
            if (IPAddress.TryParse(hostNameOrAddress, out IPAddress? address))
            {
                if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(address, $"Invalid address '{address}'");
                    throw new ArgumentException(SR.Format(SR.net_invalid_ip_addr, nameof(hostNameOrAddress)));
                }

                ipHostEntry = GetHostEntryCore(address);
            }
            else
            {
                ipHostEntry = GetHostEntryCore(hostNameOrAddress);
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(hostNameOrAddress, $"{ipHostEntry} with {ipHostEntry.AddressList.Length} entries");
            return ipHostEntry;
        }

        public static Task<IPHostEntry> GetHostEntryAsync(string hostNameOrAddress)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                Task<IPHostEntry> t = GetHostEntryCoreAsync(hostNameOrAddress, justReturnParsedIp: false, throwOnIIPAny: true);
                t.ContinueWith((t, s) => NetEventSource.Info((string)s!, $"{t.Result} with {((IPHostEntry)t.Result).AddressList.Length} entries"),
                    hostNameOrAddress, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
                return t;
            }
            else
            {
                return GetHostEntryCoreAsync(hostNameOrAddress, justReturnParsedIp: false, throwOnIIPAny: true);
            }
        }

        public static Task<IPHostEntry> GetHostEntryAsync(IPAddress address)
        {
            NameResolutionPal.EnsureSocketsAreInitialized();

            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(address, $"Invalid address '{address}'");
                throw new ArgumentException(SR.net_invalid_ip_addr, nameof(address));
            }

            return RunAsync(s => {
                IPHostEntry ipHostEntry = GetHostEntryCore((IPAddress)s);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info((IPAddress)s, $"{ipHostEntry} with {ipHostEntry.AddressList.Length} entries");
                return ipHostEntry;
            }, address);
        }

        public static IAsyncResult BeginGetHostEntry(IPAddress address, AsyncCallback? requestCallback, object? stateObject) =>
            TaskToApm.Begin(GetHostEntryAsync(address), requestCallback, stateObject);

        public static IAsyncResult BeginGetHostEntry(string hostNameOrAddress, AsyncCallback? requestCallback, object? stateObject) =>
            TaskToApm.Begin(GetHostEntryAsync(hostNameOrAddress), requestCallback, stateObject);

        public static IPHostEntry EndGetHostEntry(IAsyncResult asyncResult) =>
            TaskToApm.End<IPHostEntry>(asyncResult ?? throw new ArgumentNullException(nameof(asyncResult)));

        public static IPAddress[] GetHostAddresses(string hostNameOrAddress)
        {
            NameResolutionPal.EnsureSocketsAreInitialized();

            if (hostNameOrAddress is null)
            {
                throw new ArgumentNullException(nameof(hostNameOrAddress));
            }

            // See if it's an IP Address.
            IPAddress[] addresses;
            if (IPAddress.TryParse(hostNameOrAddress, out IPAddress? address))
            {
                if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(address, $"Invalid address '{address}'");
                    throw new ArgumentException(SR.Format(SR.net_invalid_ip_addr, nameof(hostNameOrAddress)));
                }

                addresses = new IPAddress[] { address };
            }
            else
            {
                addresses = GetHostAddressesCore(hostNameOrAddress);
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(hostNameOrAddress, addresses);
            return addresses;
        }

        public static Task<IPAddress[]> GetHostAddressesAsync(string hostNameOrAddress) =>
            (Task<IPAddress[]>)GetHostEntryOrAddressesCoreAsync(hostNameOrAddress, justReturnParsedIp: true, throwOnIIPAny: true, justAddresses: true);

        public static IAsyncResult BeginGetHostAddresses(string hostNameOrAddress, AsyncCallback? requestCallback, object? state) =>
            TaskToApm.Begin(GetHostAddressesAsync(hostNameOrAddress), requestCallback, state);

        public static IPAddress[] EndGetHostAddresses(IAsyncResult asyncResult) =>
            TaskToApm.End<IPAddress[]>(asyncResult ?? throw new ArgumentNullException(nameof(asyncResult)));

        [Obsolete("GetHostByName is obsoleted for this type, please use GetHostEntry instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry GetHostByName(string hostName)
        {
            NameResolutionPal.EnsureSocketsAreInitialized();

            if (hostName is null)
            {
                throw new ArgumentNullException(nameof(hostName));
            }

            if (IPAddress.TryParse(hostName, out IPAddress? address))
            {
                return CreateHostEntryForAddress(address);
            }

            return GetHostEntryCore(hostName);
        }

        [Obsolete("BeginGetHostByName is obsoleted for this type, please use BeginGetHostEntry instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        public static IAsyncResult BeginGetHostByName(string hostName, AsyncCallback? requestCallback, object? stateObject) =>
            TaskToApm.Begin(GetHostEntryCoreAsync(hostName, justReturnParsedIp: true, throwOnIIPAny: true), requestCallback, stateObject);

        [Obsolete("EndGetHostByName is obsoleted for this type, please use EndGetHostEntry instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry EndGetHostByName(IAsyncResult asyncResult) =>
            TaskToApm.End<IPHostEntry>(asyncResult ?? throw new ArgumentNullException(nameof(asyncResult)));

        [Obsolete("GetHostByAddress is obsoleted for this type, please use GetHostEntry instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry GetHostByAddress(string address)
        {
            NameResolutionPal.EnsureSocketsAreInitialized();

            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            IPHostEntry ipHostEntry = GetHostEntryCore(IPAddress.Parse(address));

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(address, ipHostEntry);
            return ipHostEntry;
        }

        [Obsolete("GetHostByAddress is obsoleted for this type, please use GetHostEntry instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry GetHostByAddress(IPAddress address)
        {
            NameResolutionPal.EnsureSocketsAreInitialized();

            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            IPHostEntry ipHostEntry = GetHostEntryCore(address);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(address, ipHostEntry);
            return ipHostEntry;
        }

        [Obsolete("Resolve is obsoleted for this type, please use GetHostEntry instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry Resolve(string hostName)
        {
            NameResolutionPal.EnsureSocketsAreInitialized();

            if (hostName is null)
            {
                throw new ArgumentNullException(nameof(hostName));
            }

            // See if it's an IP Address.
            IPHostEntry ipHostEntry;
            if (IPAddress.TryParse(hostName, out IPAddress? address) &&
                (address.AddressFamily != AddressFamily.InterNetworkV6 || SocketProtocolSupportPal.OSSupportsIPv6))
            {
                try
                {
                    ipHostEntry = GetHostEntryCore(address);
                }
                catch (SocketException ex)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(hostName, ex);
                    ipHostEntry = CreateHostEntryForAddress(address);
                }
            }
            else
            {
                ipHostEntry = GetHostEntryCore(hostName);
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(hostName, ipHostEntry);
            return ipHostEntry;
        }

        [Obsolete("BeginResolve is obsoleted for this type, please use BeginGetHostEntry instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        public static IAsyncResult BeginResolve(string hostName, AsyncCallback? requestCallback, object? stateObject) =>
            TaskToApm.Begin(GetHostEntryCoreAsync(hostName, justReturnParsedIp: false, throwOnIIPAny: false), requestCallback, stateObject);

        [Obsolete("EndResolve is obsoleted for this type, please use EndGetHostEntry instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry EndResolve(IAsyncResult asyncResult)
        {
            IPHostEntry ipHostEntry;

            try
            {
                ipHostEntry = TaskToApm.End<IPHostEntry>(asyncResult);
            }
            catch (SocketException ex)
            {
                IPAddress? address = asyncResult switch
                {
                    Task t => t.AsyncState as IPAddress,
                    TaskToApm.TaskAsyncResult twar => twar._task.AsyncState as IPAddress,
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

        private static IPHostEntry GetHostEntryCore(string hostName) =>
            (IPHostEntry)GetHostEntryOrAddressesCore(hostName, justAddresses: false);

        private static IPAddress[] GetHostAddressesCore(string hostName) =>
            (IPAddress[])GetHostEntryOrAddressesCore(hostName, justAddresses: true);

        private static object GetHostEntryOrAddressesCore(string hostName, bool justAddresses)
        {
            ValidateHostName(hostName);

            ValueStopwatch stopwatch = NameResolutionTelemetry.Log.BeforeResolution(hostName);

            object result;
            try
            {
                SocketError errorCode = NameResolutionPal.TryGetAddrInfo(hostName, justAddresses, out string? newHostName, out string[] aliases, out IPAddress[] addresses, out int nativeErrorCode);

                if (errorCode != SocketError.Success)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(hostName, $"{hostName} DNS lookup failed with {errorCode}");
                    throw SocketExceptionFactory.CreateSocketException(errorCode, nativeErrorCode);
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
            catch when (LogFailure(stopwatch))
            {
                Debug.Fail("LogFailure should return false");
                throw;
            }

            if (NameResolutionTelemetry.Log.IsEnabled())
                NameResolutionTelemetry.Log.AfterResolution(stopwatch, successful: true);

            return result;
        }

        private static IPHostEntry GetHostEntryCore(IPAddress address) =>
            (IPHostEntry)GetHostEntryOrAddressesCore(address, justAddresses: false);

        private static IPAddress[] GetHostAddressesCore(IPAddress address) =>
            (IPAddress[])GetHostEntryOrAddressesCore(address, justAddresses: true);

        // Does internal IPAddress reverse and then forward lookups (for Legacy and current public methods).
        private static object GetHostEntryOrAddressesCore(IPAddress address, bool justAddresses)
        {
            // Try to get the data for the host from its address.
            // We need to call getnameinfo first, because getaddrinfo w/ the ipaddress string
            // will only return that address and not the full list.

            // Do a reverse lookup to get the host name.
            ValueStopwatch stopwatch = NameResolutionTelemetry.Log.BeforeResolution(address);

            SocketError errorCode;
            string? name;
            try
            {
                name = NameResolutionPal.TryGetNameInfo(address, out errorCode, out int nativeErrorCode);
                if (errorCode != SocketError.Success)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(address, $"{address} DNS lookup failed with {errorCode}");
                    throw SocketExceptionFactory.CreateSocketException(errorCode, nativeErrorCode);
                }
                Debug.Assert(name != null);
            }
            catch when (LogFailure(stopwatch))
            {
                Debug.Fail("LogFailure should return false");
                throw;
            }

            if (NameResolutionTelemetry.Log.IsEnabled())
            {
                NameResolutionTelemetry.Log.AfterResolution(stopwatch, successful: true);

                // Do the forward lookup to get the IPs for that host name
                stopwatch = NameResolutionTelemetry.Log.BeforeResolution(name);
            }

            object result;
            try
            {
                errorCode = NameResolutionPal.TryGetAddrInfo(name, justAddresses, out string? hostName, out string[] aliases, out IPAddress[] addresses, out int nativeErrorCode);

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
            catch when (LogFailure(stopwatch))
            {
                Debug.Fail("LogFailure should return false");
                throw;
            }

            if (NameResolutionTelemetry.Log.IsEnabled())
                NameResolutionTelemetry.Log.AfterResolution(stopwatch, successful: true);

            // One of three things happened:
            // 1. Success.
            // 2. There was a ptr record in dns, but not a corollary A/AAA record.
            // 3. The IP was a local (non-loopback) IP that resolved to a connection specific dns suffix.
            //    - Workaround, Check "Use this connection's dns suffix in dns registration" on that network
            //      adapter's advanced dns settings.
            // Return whatever we got.
            return result;
        }

        private static Task<IPHostEntry> GetHostEntryCoreAsync(string hostName, bool justReturnParsedIp, bool throwOnIIPAny) =>
            (Task<IPHostEntry>)GetHostEntryOrAddressesCoreAsync(hostName, justReturnParsedIp, throwOnIIPAny, justAddresses: false);

        // If hostName is an IPString and justReturnParsedIP==true then no reverse lookup will be attempted, but the original address is returned.
        private static Task GetHostEntryOrAddressesCoreAsync(string hostName, bool justReturnParsedIp, bool throwOnIIPAny, bool justAddresses)
        {
            NameResolutionPal.EnsureSocketsAreInitialized();

            if (hostName is null)
            {
                throw new ArgumentNullException(nameof(hostName));
            }

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
                        Task.FromResult(new[] { ipAddress }) :
                        Task.FromResult(CreateHostEntryForAddress(ipAddress));
                }

                return justAddresses ? (Task)
                    RunAsync(s => GetHostAddressesCore((IPAddress)s), ipAddress) :
                    RunAsync(s => GetHostEntryCore((IPAddress)s), ipAddress);
            }

            // If the OS supports it and 'hostName' is not an IP Address, resolve the name asynchronously
            // instead of calling the synchronous version in the ThreadPool.
            if (NameResolutionPal.SupportsGetAddrInfoAsync && ipAddress is null)
            {
                ValidateHostName(hostName);

                if (NameResolutionTelemetry.Log.IsEnabled())
                {
                    ValueStopwatch stopwatch = NameResolutionTelemetry.Log.BeforeResolution(hostName);

                    Task coreTask;
                    try
                    {
                        coreTask = NameResolutionPal.GetAddrInfoAsync(hostName, justAddresses);
                    }
                    catch when (LogFailure(stopwatch))
                    {
                        Debug.Fail("LogFailure should return false");
                        throw;
                    }

                    coreTask.ContinueWith(
                        (task, state) =>
                        {
                            NameResolutionTelemetry.Log.AfterResolution(
                                stopwatch: (ValueStopwatch)state!,
                                successful: task.IsCompletedSuccessfully);
                        },
                        state: stopwatch,
                        cancellationToken: default,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);

                    // coreTask is not actually a base Task, but Task<IPHostEntry> / Task<IPAddress[]>
                    // We have to return it and not the continuation
                    return coreTask;
                }
                else
                {
                    return NameResolutionPal.GetAddrInfoAsync(hostName, justAddresses);
                }
            }

            return justAddresses ? (Task)
                RunAsync(s => GetHostAddressesCore((string)s), hostName) :
                RunAsync(s => GetHostEntryCore((string)s), hostName);
        }

        private static Task<TResult> RunAsync<TResult>(Func<object, TResult> func, object arg) =>
            Task.Factory.StartNew(func!, arg, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

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


        private static bool LogFailure(ValueStopwatch stopwatch)
        {
            if (NameResolutionTelemetry.Log.IsEnabled())
                NameResolutionTelemetry.Log.AfterResolution(stopwatch, successful: false);

            return false;
        }
    }
}
