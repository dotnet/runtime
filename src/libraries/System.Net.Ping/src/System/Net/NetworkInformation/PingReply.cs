// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.NetworkInformation
{
    public class PingReply
    {
        internal PingReply(
            IPAddress address,
            PingOptions? options,
            IPStatus ipStatus,
            long rtt,
            byte[] buffer)
        {
            Address = address;
            Options = options;
            Status = ipStatus;
            RoundtripTime = rtt;
            Buffer = buffer;
        }

        public IPStatus Status { get; }

        public IPAddress Address { get; }

        public long RoundtripTime { get; }

        public PingOptions? Options { get; }

        public byte[] Buffer { get; }
    }
}
