// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    public class PingReply
    {
        private readonly IPAddress _address;
        private readonly PingOptions _options;
        private readonly IPStatus _ipStatus;
        private readonly long _rtt;
        private readonly byte[] _buffer;

        internal PingReply(
            IPAddress address, 
            PingOptions options, 
            IPStatus ipStatus, 
            long rtt, 
            byte[] buffer)
        {
            _address = address;
            _options = options;
            _ipStatus = ipStatus;
            _rtt = rtt;
            _buffer = buffer;
        }

        public IPStatus Status { get { return _ipStatus; } }

        public IPAddress Address { get { return _address; } }

        public long RoundtripTime { get { return _rtt; } }

        public PingOptions Options { get { return _options; } }

        public byte[] Buffer { get { return _buffer; } }
    }
}
