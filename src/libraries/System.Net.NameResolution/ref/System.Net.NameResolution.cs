// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Net
{
    public static partial class Dns
    {
        public static IAsyncResult BeginGetHostAddresses(string hostNameOrAddress, AsyncCallback requestCallback, object state) { throw null; }
        [Obsolete("BeginGetHostByName is obsoleted for this type, please use BeginGetHostEntry instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static IAsyncResult BeginGetHostByName(string hostName, AsyncCallback requestCallback, object stateObject) { throw null; }
        public static IAsyncResult BeginGetHostEntry(string hostNameOrAddress, AsyncCallback requestCallback, object stateObject) { throw null; }
        public static IAsyncResult BeginGetHostEntry(IPAddress address, AsyncCallback requestCallback, object stateObject) { throw null; }
        [Obsolete("BeginResolve is obsoleted for this type, please use BeginGetHostEntry instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static IAsyncResult BeginResolve(string hostName, AsyncCallback requestCallback, object stateObject) { throw null; }
        public static IPAddress[] EndGetHostAddresses(IAsyncResult asyncResult) { throw null; }
        [Obsolete("EndGetHostByName is obsoleted for this type, please use EndGetHostEntry instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry EndGetHostByName(IAsyncResult asyncResult) { throw null; }
        public static IPHostEntry EndGetHostEntry(IAsyncResult asyncResult) { throw null; }
        [Obsolete("EndResolve is obsoleted for this type, please use EndGetHostEntry instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry EndResolve(IAsyncResult asyncResult) { throw null; }
        public static IPAddress[] GetHostAddresses(string hostNameOrAddress) { throw null; }
        [Obsolete("GetHostByAddress is obsoleted for this type, please use GetHostEntry instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry GetHostByAddress(IPAddress address) { throw null; }
        [Obsolete("GetHostByAddress is obsoleted for this type, please use GetHostEntry instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry GetHostByAddress(string address) { throw null; }
        [Obsolete("GetHostByName is obsoleted for this type, please use GetHostEntry instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry GetHostByName(string hostName) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.IPAddress[]> GetHostAddressesAsync(string hostNameOrAddress) { throw null; }
        public static IPHostEntry GetHostEntry(string hostNameOrAddress) { throw null; }
        public static IPHostEntry GetHostEntry(IPAddress address) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.IPHostEntry> GetHostEntryAsync(System.Net.IPAddress address) { throw null; }
        public static System.Threading.Tasks.Task<System.Net.IPHostEntry> GetHostEntryAsync(string hostNameOrAddress) { throw null; }
        public static string GetHostName() { throw null; }
        [Obsolete("Resolve is obsoleted for this type, please use GetHostEntry instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static IPHostEntry Resolve(string hostName) { throw null; }
    }
    public partial class IPHostEntry
    {
        public IPHostEntry() { }
        public System.Net.IPAddress[] AddressList { get { throw null; } set { } }
        public string[] Aliases { get { throw null; } set { } }
        public string HostName { get { throw null; } set { } }
    }
}
