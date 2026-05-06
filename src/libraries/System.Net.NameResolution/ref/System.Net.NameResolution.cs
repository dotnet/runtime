// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net
{
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
    }
    public partial class IPHostEntry
    {
        public IPHostEntry() { }
        public System.Net.IPAddress[] AddressList { get { throw null; } set { } }
        public string[] Aliases { get { throw null; } set { } }
        public string HostName { get { throw null; } set { } }
    }
}
