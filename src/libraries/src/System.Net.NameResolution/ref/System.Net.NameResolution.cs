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
        public static System.Threading.Tasks.Task<System.Net.IPAddress[]> GetHostAddressesAsync(string hostNameOrAddress) { return default(System.Threading.Tasks.Task<System.Net.IPAddress[]>); }
        public static System.Threading.Tasks.Task<System.Net.IPHostEntry> GetHostEntryAsync(System.Net.IPAddress address) { return default(System.Threading.Tasks.Task<System.Net.IPHostEntry>); }
        public static System.Threading.Tasks.Task<System.Net.IPHostEntry> GetHostEntryAsync(string hostNameOrAddress) { return default(System.Threading.Tasks.Task<System.Net.IPHostEntry>); }
        public static string GetHostName() { return default(string); }
    }
    public partial class IPHostEntry
    {
        public IPHostEntry() { }
        public System.Net.IPAddress[] AddressList { get { return default(System.Net.IPAddress[]); } set { } }
        public string[] Aliases { get { return default(string[]); } set { } }
        public string HostName { get { return default(string); } set { } }
    }
}
