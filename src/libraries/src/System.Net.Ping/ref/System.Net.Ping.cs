// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Net.NetworkInformation
{
    public enum IPStatus
    {
        BadDestination = 11018,
        BadHeader = 11042,
        BadOption = 11007,
        BadRoute = 11012,
        DestinationHostUnreachable = 11003,
        DestinationNetworkUnreachable = 11002,
        DestinationPortUnreachable = 11005,
        DestinationProhibited = 11004,
        DestinationProtocolUnreachable = 11004,
        DestinationScopeMismatch = 11045,
        DestinationUnreachable = 11040,
        HardwareError = 11008,
        IcmpError = 11044,
        NoResources = 11006,
        PacketTooBig = 11009,
        ParameterProblem = 11015,
        SourceQuench = 11016,
        Success = 0,
        TimedOut = 11010,
        TimeExceeded = 11041,
        TtlExpired = 11013,
        TtlReassemblyTimeExceeded = 11014,
        Unknown = -1,
        UnrecognizedNextHeader = 11043,
    }
    public partial class Ping
    {
        public Ping() { }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(System.Net.IPAddress address) { return default(System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply>); }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(System.Net.IPAddress address, int timeout) { return default(System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply>); }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(System.Net.IPAddress address, int timeout, byte[] buffer) { return default(System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply>); }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(System.Net.IPAddress address, int timeout, byte[] buffer, System.Net.NetworkInformation.PingOptions options) { return default(System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply>); }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(string hostNameOrAddress) { return default(System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply>); }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(string hostNameOrAddress, int timeout) { return default(System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply>); }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(string hostNameOrAddress, int timeout, byte[] buffer) { return default(System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply>); }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(string hostNameOrAddress, int timeout, byte[] buffer, System.Net.NetworkInformation.PingOptions options) { return default(System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply>); }
    }
    public partial class PingException : System.InvalidOperationException
    {
        public PingException(string message) { }
        public PingException(string message, System.Exception innerException) { }
    }
    public partial class PingOptions
    {
        public PingOptions() { }
        public PingOptions(int ttl, bool dontFragment) { }
        public bool DontFragment { get { return default(bool); } set { } }
        public int Ttl { get { return default(int); } set { } }
    }
    public partial class PingReply
    {
        internal PingReply() { }
        public System.Net.IPAddress Address { get { return default(System.Net.IPAddress); } }
        public byte[] Buffer { get { return default(byte[]); } }
        public System.Net.NetworkInformation.PingOptions Options { get { return default(System.Net.NetworkInformation.PingOptions); } }
        public long RoundtripTime { get { return default(long); } }
        public System.Net.NetworkInformation.IPStatus Status { get { return default(System.Net.NetworkInformation.IPStatus); } }
    }
}
