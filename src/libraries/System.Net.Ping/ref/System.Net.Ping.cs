// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net.NetworkInformation
{
    public enum IPStatus
    {
        Unknown = -1,
        Success = 0,
        DestinationNetworkUnreachable = 11002,
        DestinationHostUnreachable = 11003,
        DestinationProhibited = 11004,
        DestinationProtocolUnreachable = 11004,
        DestinationPortUnreachable = 11005,
        NoResources = 11006,
        BadOption = 11007,
        HardwareError = 11008,
        PacketTooBig = 11009,
        TimedOut = 11010,
        BadRoute = 11012,
        TtlExpired = 11013,
        TtlReassemblyTimeExceeded = 11014,
        ParameterProblem = 11015,
        SourceQuench = 11016,
        BadDestination = 11018,
        DestinationUnreachable = 11040,
        TimeExceeded = 11041,
        BadHeader = 11042,
        UnrecognizedNextHeader = 11043,
        IcmpError = 11044,
        DestinationScopeMismatch = 11045,
    }
    public partial class Ping : System.ComponentModel.Component
    {
        public Ping() { }
        public event System.Net.NetworkInformation.PingCompletedEventHandler? PingCompleted { add { } remove { } }
        protected override void Dispose(bool disposing) { }
        protected void OnPingCompleted(System.Net.NetworkInformation.PingCompletedEventArgs e) { }
        public System.Net.NetworkInformation.PingReply Send(System.Net.IPAddress address) { throw null; }
        public System.Net.NetworkInformation.PingReply Send(System.Net.IPAddress address, int timeout) { throw null; }
        public System.Net.NetworkInformation.PingReply Send(System.Net.IPAddress address, int timeout, byte[] buffer) { throw null; }
        public System.Net.NetworkInformation.PingReply Send(System.Net.IPAddress address, int timeout, byte[] buffer, System.Net.NetworkInformation.PingOptions? options) { throw null; }
        public System.Net.NetworkInformation.PingReply Send(string hostNameOrAddress) { throw null; }
        public System.Net.NetworkInformation.PingReply Send(string hostNameOrAddress, int timeout) { throw null; }
        public System.Net.NetworkInformation.PingReply Send(string hostNameOrAddress, int timeout, byte[] buffer) { throw null; }
        public System.Net.NetworkInformation.PingReply Send(string hostNameOrAddress, int timeout, byte[] buffer, System.Net.NetworkInformation.PingOptions? options) { throw null; }
        public System.Net.NetworkInformation.PingReply Send(System.Net.IPAddress address, System.TimeSpan timeout, byte[]? buffer, System.Net.NetworkInformation.PingOptions? options) { throw null; }
        public System.Net.NetworkInformation.PingReply Send(string hostNameOrAddress, System.TimeSpan timeout, byte[]? buffer, System.Net.NetworkInformation.PingOptions? options) { throw null; }
        public void SendAsync(System.Net.IPAddress address, int timeout, byte[] buffer, System.Net.NetworkInformation.PingOptions? options, object? userToken) { }
        public void SendAsync(System.Net.IPAddress address, int timeout, byte[] buffer, object? userToken) { }
        public void SendAsync(System.Net.IPAddress address, int timeout, object? userToken) { }
        public void SendAsync(System.Net.IPAddress address, object? userToken) { }
        public void SendAsync(string hostNameOrAddress, int timeout, byte[] buffer, System.Net.NetworkInformation.PingOptions? options, object? userToken) { }
        public void SendAsync(string hostNameOrAddress, int timeout, byte[] buffer, object? userToken) { }
        public void SendAsync(string hostNameOrAddress, int timeout, object? userToken) { }
        public void SendAsync(string hostNameOrAddress, object? userToken) { }
        public void SendAsyncCancel() { }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(System.Net.IPAddress address) { throw null; }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(System.Net.IPAddress address, int timeout) { throw null; }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(System.Net.IPAddress address, int timeout, byte[] buffer) { throw null; }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(System.Net.IPAddress address, int timeout, byte[] buffer, System.Net.NetworkInformation.PingOptions? options) { throw null; }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(string hostNameOrAddress) { throw null; }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(string hostNameOrAddress, int timeout) { throw null; }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(string hostNameOrAddress, int timeout, byte[] buffer) { throw null; }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(string hostNameOrAddress, int timeout, byte[] buffer, System.Net.NetworkInformation.PingOptions? options) { throw null; }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(System.Net.IPAddress address, System.TimeSpan timeout, byte[]? buffer, System.Net.NetworkInformation.PingOptions? options, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<System.Net.NetworkInformation.PingReply> SendPingAsync(string hostNameOrAddress, System.TimeSpan timeout, byte[]? buffer, System.Net.NetworkInformation.PingOptions? options, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class PingCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
        internal PingCompletedEventArgs() : base (default(System.Exception), default(bool), default(object)) { }
        public System.Net.NetworkInformation.PingReply? Reply { get { throw null; } }
    }
    public delegate void PingCompletedEventHandler(object sender, System.Net.NetworkInformation.PingCompletedEventArgs e);
    public partial class PingException : System.InvalidOperationException
    {
        protected PingException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public PingException(string? message) { }
        public PingException(string? message, System.Exception? innerException) { }
    }
    public partial class PingOptions
    {
        public PingOptions() { }
        public PingOptions(int ttl, bool dontFragment) { }
        public bool DontFragment { get { throw null; } set { } }
        public int Ttl { get { throw null; } set { } }
    }
    public partial class PingReply
    {
        internal PingReply() { }
        public System.Net.IPAddress Address { get { throw null; } }
        public byte[] Buffer { get { throw null; } }
        public System.Net.NetworkInformation.PingOptions? Options { get { throw null; } }
        public long RoundtripTime { get { throw null; } }
        public System.Net.NetworkInformation.IPStatus Status { get { throw null; } }
    }
}
