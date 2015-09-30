// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace System.Net.NetworkInformation
{
    public class PingReply
    {
        private IPAddress _address;
        private PingOptions _options;
        // The status code returned by icmpsendecho, or the ICMP status field on the raw socket.
        private IPStatus _ipStatus;
        // The round trip time.
        private long _rtt;
        private byte[] _buffer;

        internal PingReply()
        {
        }

        internal PingReply(IPStatus ipStatus)
        {
            _ipStatus = ipStatus;
            _buffer = Array.Empty<byte>();
        }

        internal PingReply(Interop.IpHlpApi.IcmpEchoReply reply)
        {
            _address = new IPAddress(reply.address);
            // The icmpsendecho IP status codes.
            _ipStatus = (IPStatus)reply.status;

            // Only copy the data if we succeed w/ the ping operation.
            if (_ipStatus == IPStatus.Success)
            {
                _rtt = (long)reply.roundTripTime;
                _buffer = new byte[reply.dataSize];
                Marshal.Copy(reply.data, _buffer, 0, reply.dataSize);
                _options = new PingOptions(reply.options);
            }
            else
            {
                _buffer = Array.Empty<byte>();
            }
        }

        internal PingReply(Interop.IpHlpApi.Icmp6EchoReply reply, IntPtr dataPtr, int sendSize)
        {
            _address = new IPAddress(reply.Address.Address, reply.Address.ScopeID);
            // The icmpsendecho IP status codes.
            _ipStatus = (IPStatus)reply.Status;

            // Only copy the data if we succeed w/ the ping operation.
            if (_ipStatus == IPStatus.Success)
            {
                _rtt = (long)reply.RoundTripTime;
                _buffer = new byte[sendSize];
                Marshal.Copy(IntPtrHelper.Add(dataPtr, 36), _buffer, 0, sendSize);
            }
            else
            {
                _buffer = Array.Empty<byte>();
            }
        }

        // Translates the relevant icmpsendecho codes to a IPStatus code.
        private IPStatus GetIPStatus(Interop.IpHlpApi.IcmpV4Type type, Interop.IpHlpApi.IcmpV4Code code)
        {
            switch (type)
            {
                case Interop.IpHlpApi.IcmpV4Type.ICMP4_ECHO_REPLY:
                    return IPStatus.Success;
                case Interop.IpHlpApi.IcmpV4Type.ICMP4_SOURCE_QUENCH:
                    return IPStatus.SourceQuench;
                case Interop.IpHlpApi.IcmpV4Type.ICMP4_PARAM_PROB:
                    return IPStatus.ParameterProblem;
                case Interop.IpHlpApi.IcmpV4Type.ICMP4_TIME_EXCEEDED:
                    return IPStatus.TtlExpired;
                case Interop.IpHlpApi.IcmpV4Type.ICMP4_DST_UNREACH:
                    {
                        switch (code)
                        {
                            case Interop.IpHlpApi.IcmpV4Code.ICMP4_UNREACH_NET:
                                return IPStatus.DestinationNetworkUnreachable;
                            case Interop.IpHlpApi.IcmpV4Code.ICMP4_UNREACH_HOST:
                                return IPStatus.DestinationHostUnreachable;
                            case Interop.IpHlpApi.IcmpV4Code.ICMP4_UNREACH_PROTOCOL:
                                return IPStatus.DestinationProtocolUnreachable;
                            case Interop.IpHlpApi.IcmpV4Code.ICMP4_UNREACH_PORT:
                                return IPStatus.DestinationPortUnreachable;
                            case Interop.IpHlpApi.IcmpV4Code.ICMP4_UNREACH_FRAG_NEEDED:
                                return IPStatus.PacketTooBig;
                            default:
                                return IPStatus.DestinationUnreachable;
                        }
                    }
            }

            return IPStatus.Unknown;
        }

        public IPStatus Status { get { return _ipStatus; } }

        public IPAddress Address { get { return _address; } }

        public long RoundtripTime { get { return _rtt; } }

        public PingOptions Options
        {
            get
            {
                return _options;
            }
        }

        public byte[] Buffer { get { return _buffer; } }
    }
}
