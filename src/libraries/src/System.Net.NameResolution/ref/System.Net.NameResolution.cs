// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
