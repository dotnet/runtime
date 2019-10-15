// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    // ICMP statistics for IPv4.
    internal class SystemIcmpV4Statistics : IcmpV4Statistics
    {
        private readonly Interop.IpHlpApi.MibIcmpInfo _stats;

        internal SystemIcmpV4Statistics()
        {
            uint result = Interop.IpHlpApi.GetIcmpStatistics(out _stats);

            if (result != Interop.IpHlpApi.ERROR_SUCCESS)
            {
                throw new NetworkInformationException((int)result);
            }
        }

        public override long MessagesSent { get { return _stats.outStats.messages; } }

        public override long MessagesReceived { get { return _stats.inStats.messages; } }

        public override long ErrorsSent { get { return _stats.outStats.errors; } }

        public override long ErrorsReceived { get { return _stats.inStats.errors; } }

        public override long DestinationUnreachableMessagesSent { get { return _stats.outStats.destinationUnreachables; } }

        public override long DestinationUnreachableMessagesReceived { get { return _stats.inStats.destinationUnreachables; } }

        public override long TimeExceededMessagesSent { get { return _stats.outStats.timeExceeds; } }

        public override long TimeExceededMessagesReceived { get { return _stats.inStats.timeExceeds; } }

        public override long ParameterProblemsSent { get { return _stats.outStats.parameterProblems; } }

        public override long ParameterProblemsReceived { get { return _stats.inStats.parameterProblems; } }

        public override long SourceQuenchesSent { get { return _stats.outStats.sourceQuenches; } }

        public override long SourceQuenchesReceived { get { return _stats.inStats.sourceQuenches; } }

        public override long RedirectsSent { get { return _stats.outStats.redirects; } }

        public override long RedirectsReceived { get { return _stats.inStats.redirects; } }

        public override long EchoRequestsSent { get { return _stats.outStats.echoRequests; } }

        public override long EchoRequestsReceived { get { return _stats.inStats.echoRequests; } }

        public override long EchoRepliesSent { get { return _stats.outStats.echoReplies; } }

        public override long EchoRepliesReceived { get { return _stats.inStats.echoReplies; } }

        public override long TimestampRequestsSent { get { return _stats.outStats.timestampRequests; } }

        public override long TimestampRequestsReceived { get { return _stats.inStats.timestampRequests; } }

        public override long TimestampRepliesSent { get { return _stats.outStats.timestampReplies; } }

        public override long TimestampRepliesReceived { get { return _stats.inStats.timestampReplies; } }

        public override long AddressMaskRequestsSent { get { return _stats.outStats.addressMaskRequests; } }

        public override long AddressMaskRequestsReceived { get { return _stats.inStats.addressMaskRequests; } }

        public override long AddressMaskRepliesSent { get { return _stats.outStats.addressMaskReplies; } }

        public override long AddressMaskRepliesReceived { get { return _stats.inStats.addressMaskReplies; } }
    }
}
