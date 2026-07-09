// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net
{
    public readonly partial struct AddressRecord
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Net.IPAddress Address { get { throw null; } }
        public System.TimeSpan Ttl { get { throw null; } }
    }
    public readonly partial struct CNameRecord
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public string CanonicalName { get { throw null; } }
        public System.TimeSpan Ttl { get { throw null; } }
    }
    public static partial class Dns
    {
        public static System.IAsyncResult BeginGetHostAddresses(string hostNameOrAddress, System.AsyncCallback? requestCallback, object? state) { throw null; }
        [System.ObsoleteAttribute("BeginGetHostByName has been deprecated. Use BeginGetHostEntry instead.")]
        public static System.IAsyncResult BeginGetHostByName(string hostName, System.AsyncCallback? requestCallback, object? stateObject) { throw null; }
        public static System.IAsyncResult BeginGetHostEntry(System.Net.IPAddress address, System.AsyncCallback? requestCallback, object? stateObject) { throw null; }
        public static System.IAsyncResult BeginGetHostEntry(string hostNameOrAddress, System.AsyncCallback? requestCallback, object? stateObject) { throw null; }
        [System.ObsoleteAttribute("BeginResolve has been deprecated. Use BeginGetHostEntry instead.")]
        public static System.IAsyncResult BeginResolve(string hostName, System.AsyncCallback? requestCallback, object? stateObject) { throw null; }
        public static System.Net.IPAddress[] EndGetHostAddresses(System.IAsyncResult asyncResult) { throw null; }
        [System.ObsoleteAttribute("EndGetHostByName has been deprecated. Use EndGetHostEntry instead.")]
        public static System.Net.IPHostEntry EndGetHostByName(System.IAsyncResult asyncResult) { throw null; }
        public static System.Net.IPHostEntry EndGetHostEntry(System.IAsyncResult asyncResult) { throw null; }
        [System.ObsoleteAttribute("EndResolve has been deprecated. Use EndGetHostEntry instead.")]
        public static System.Net.IPHostEntry EndResolve(System.IAsyncResult asyncResult) { throw null; }
        public static System.Net.IPAddress[] GetHostAddresses(string hostNameOrAddress) { throw null; }
        public static System.Net.IPAddress[] GetHostAddresses(string hostNameOrAddress, System.Net.Sockets.AddressFamily family) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.IPAddress[]> GetHostAddressesAsync(string hostNameOrAddress) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.IPAddress[]> GetHostAddressesAsync(string hostNameOrAddress, System.Net.Sockets.AddressFamily family, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.IPAddress[]> GetHostAddressesAsync(string hostNameOrAddress, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.ObsoleteAttribute("GetHostByAddress has been deprecated. Use GetHostEntry instead.")]
        public static System.Net.IPHostEntry GetHostByAddress(System.Net.IPAddress address) { throw null; }
        [System.ObsoleteAttribute("GetHostByAddress has been deprecated. Use GetHostEntry instead.")]
        public static System.Net.IPHostEntry GetHostByAddress(string address) { throw null; }
        [System.ObsoleteAttribute("GetHostByName has been deprecated. Use GetHostEntry instead.")]
        public static System.Net.IPHostEntry GetHostByName(string hostName) { throw null; }
        public static System.Net.IPHostEntry GetHostEntry(System.Net.IPAddress address) { throw null; }
        public static System.Net.IPHostEntry GetHostEntry(string hostNameOrAddress) { throw null; }
        public static System.Net.IPHostEntry GetHostEntry(string hostNameOrAddress, System.Net.Sockets.AddressFamily family) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.IPHostEntry> GetHostEntryAsync(System.Net.IPAddress address) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.IPHostEntry> GetHostEntryAsync(string hostNameOrAddress) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.IPHostEntry> GetHostEntryAsync(string hostNameOrAddress, System.Net.Sockets.AddressFamily family, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.IPHostEntry> GetHostEntryAsync(string hostNameOrAddress, System.Threading.CancellationToken cancellationToken) { throw null; }
        public static string GetHostName() { throw null; }
        [System.ObsoleteAttribute("Resolve has been deprecated. Use GetHostEntry instead.")]
        public static System.Net.IPHostEntry Resolve(string hostName) { throw null; }
        public static System.Net.DnsResult<System.Net.AddressRecord> ResolveAddresses(string name) { throw null; }
        public static System.Net.DnsResult<System.Net.AddressRecord> ResolveAddresses(string name, System.Net.Sockets.AddressFamily addressFamily) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.AddressRecord>> ResolveAddressesAsync(string name, System.Net.Sockets.AddressFamily addressFamily, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.AddressRecord>> ResolveAddressesAsync(string name, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Net.DnsResult<System.Net.CNameRecord> ResolveCName(string name) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.CNameRecord>> ResolveCNameAsync(string name, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Net.DnsResult<System.Net.MxRecord> ResolveMx(string name) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.MxRecord>> ResolveMxAsync(string name, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Net.DnsResult<System.Net.NsRecord> ResolveNs(string name) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.NsRecord>> ResolveNsAsync(string name, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Net.DnsResult<System.Net.PtrRecord> ResolvePtr(System.Net.IPAddress address) { throw null; }
        public static System.Net.DnsResult<System.Net.PtrRecord> ResolvePtr(string name) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.PtrRecord>> ResolvePtrAsync(System.Net.IPAddress address, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.PtrRecord>> ResolvePtrAsync(string name, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Net.DnsResult<System.Net.SrvRecord> ResolveSrv(string name) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.SrvRecord>> ResolveSrvAsync(string name, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Net.DnsResult<System.Net.TxtRecord> ResolveTxt(string name) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.TxtRecord>> ResolveTxtAsync(string name, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public sealed partial class DnsResolver : System.IAsyncDisposable, System.IDisposable
    {
        public DnsResolver() { }
        public DnsResolver(System.Net.DnsResolverOptions options) { }
        public void Dispose() { }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public System.Net.DnsResult<System.Net.AddressRecord> ResolveAddresses(string name) { throw null; }
        public System.Net.DnsResult<System.Net.AddressRecord> ResolveAddresses(string name, System.Net.Sockets.AddressFamily addressFamily) { throw null; }
        public System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.AddressRecord>> ResolveAddressesAsync(string name, System.Net.Sockets.AddressFamily addressFamily, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.AddressRecord>> ResolveAddressesAsync(string name, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Net.DnsResult<System.Net.CNameRecord> ResolveCName(string name) { throw null; }
        public System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.CNameRecord>> ResolveCNameAsync(string name, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Net.DnsResult<System.Net.MxRecord> ResolveMx(string name) { throw null; }
        public System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.MxRecord>> ResolveMxAsync(string name, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Net.DnsResult<System.Net.NsRecord> ResolveNs(string name) { throw null; }
        public System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.NsRecord>> ResolveNsAsync(string name, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Net.DnsResult<System.Net.PtrRecord> ResolvePtr(System.Net.IPAddress address) { throw null; }
        public System.Net.DnsResult<System.Net.PtrRecord> ResolvePtr(string name) { throw null; }
        public System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.PtrRecord>> ResolvePtrAsync(System.Net.IPAddress address, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.PtrRecord>> ResolvePtrAsync(string name, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Net.DnsResult<System.Net.SrvRecord> ResolveSrv(string name) { throw null; }
        public System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.SrvRecord>> ResolveSrvAsync(string name, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Net.DnsResult<System.Net.TxtRecord> ResolveTxt(string name) { throw null; }
        public System.Threading.Tasks.Task<System.Net.DnsResult<System.Net.TxtRecord>> ResolveTxtAsync(string name, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public sealed partial class DnsResolverOptions
    {
        public DnsResolverOptions() { }
        public System.Collections.Generic.IList<System.Net.IPEndPoint> Servers { get { throw null; } set { } }
    }
    [System.CLSCompliantAttribute(false)]
    public enum DnsResponseCode : ushort
    {
        NoError = (ushort)0,
        FormatError = (ushort)1,
        ServerFailure = (ushort)2,
        NxDomain = (ushort)3,
        NotImplemented = (ushort)4,
        Refused = (ushort)5,
    }
    public readonly partial struct DnsResult<T>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.TimeSpan NegativeCacheTtl { get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<T> Records { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public System.Net.DnsResponseCode ResponseCode { get { throw null; } }
    }
    public partial class IPHostEntry
    {
        public IPHostEntry() { }
        public System.Net.IPAddress[] AddressList { get { throw null; } set { } }
        public string[] Aliases { get { throw null; } set { } }
        public string HostName { get { throw null; } set { } }
    }
    public readonly partial struct MxRecord
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public string Exchange { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public ushort Preference { get { throw null; } }
        public System.TimeSpan Ttl { get { throw null; } }
    }
    public readonly partial struct NsRecord
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public string Name { get { throw null; } }
        public System.TimeSpan Ttl { get { throw null; } }
    }
    public readonly partial struct PtrRecord
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public string Name { get { throw null; } }
        public System.TimeSpan Ttl { get { throw null; } }
    }
    public readonly partial struct SrvRecord
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Collections.Generic.IReadOnlyList<System.Net.AddressRecord> Addresses { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public ushort Port { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public ushort Priority { get { throw null; } }
        public string Target { get { throw null; } }
        public System.TimeSpan Ttl { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public ushort Weight { get { throw null; } }
    }
    public readonly partial struct TxtRecord
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.TimeSpan Ttl { get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<string> Values { get { throw null; } }
    }
}
