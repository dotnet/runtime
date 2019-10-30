// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    internal class BsdIcmpV6Statistics : IcmpV6Statistics
    {
        private readonly long _destinationUnreachableMessagesReceived;
        private readonly long _destinationUnreachableMessagesSent;
        private readonly long _echoRepliesReceived;
        private readonly long _echoRepliesSent;
        private readonly long _echoRequestsReceived;
        private readonly long _echoRequestsSent;
        private readonly long _membershipQueriesReceived;
        private readonly long _membershipQueriesSent;
        private readonly long _membershipReductionsReceived;
        private readonly long _membershipReductionsSent;
        private readonly long _membershipReportsReceived;
        private readonly long _membershipReportsSent;
        private readonly long _neighborAdvertisementsReceived;
        private readonly long _neighborAdvertisementsSent;
        private readonly long _neighborSolicitsReceived;
        private readonly long _neighborSolicitsSent;
        private readonly long _packetTooBigMessagesReceived;
        private readonly long _packetTooBigMessagesSent;
        private readonly long _parameterProblemsReceived;
        private readonly long _parameterProblemsSent;
        private readonly long _redirectsReceived;
        private readonly long _redirectsSent;
        private readonly long _routerAdvertisementsReceived;
        private readonly long _routerAdvertisementsSent;
        private readonly long _routerSolicitsReceived;
        private readonly long _routerSolicitsSent;
        private readonly long _timeExceededMessagesReceived;
        private readonly long _timeExceededMessagesSent;

        public BsdIcmpV6Statistics()
        {
            Interop.Sys.Icmpv6GlobalStatistics statistics;
            if (Interop.Sys.GetIcmpv6GlobalStatistics(out statistics) != 0)
            {
                throw new NetworkInformationException(SR.net_PInvokeError);
            }

            _destinationUnreachableMessagesReceived = (long)statistics.DestinationUnreachableMessagesReceived;
            _destinationUnreachableMessagesSent = (long)statistics.DestinationUnreachableMessagesSent;
            _echoRepliesReceived = (long)statistics.EchoRepliesReceived;
            _echoRepliesSent = (long)statistics.EchoRepliesSent;
            _echoRequestsReceived = (long)statistics.EchoRequestsReceived;
            _echoRequestsSent = (long)statistics.EchoRequestsSent;
            _membershipQueriesReceived = (long)statistics.MembershipQueriesReceived;
            _membershipQueriesSent = (long)statistics.MembershipQueriesSent;
            _membershipReductionsReceived = (long)statistics.MembershipReductionsReceived;
            _membershipReductionsSent = (long)statistics.MembershipReductionsSent;
            _membershipReportsReceived = (long)statistics.MembershipReportsReceived;
            _membershipReportsSent = (long)statistics.MembershipReportsSent;
            _neighborAdvertisementsReceived = (long)statistics.NeighborAdvertisementsReceived;
            _neighborAdvertisementsSent = (long)statistics.NeighborAdvertisementsSent;
            _neighborSolicitsReceived = (long)statistics.NeighborSolicitsReceived;
            _neighborSolicitsSent = (long)statistics.NeighborSolicitsSent;
            _packetTooBigMessagesReceived = (long)statistics.PacketTooBigMessagesReceived;
            _packetTooBigMessagesSent = (long)statistics.PacketTooBigMessagesSent;
            _parameterProblemsReceived = (long)statistics.ParameterProblemsReceived;
            _parameterProblemsSent = (long)statistics.ParameterProblemsSent;
            _redirectsReceived = (long)statistics.RedirectsReceived;
            _redirectsSent = (long)statistics.RedirectsSent;
            _routerAdvertisementsReceived = (long)statistics.RouterAdvertisementsReceived;
            _routerAdvertisementsSent = (long)statistics.RouterAdvertisementsSent;
            _routerSolicitsReceived = (long)statistics.RouterAdvertisementsReceived;
            _routerSolicitsSent = (long)statistics.RouterAdvertisementsSent;
            _timeExceededMessagesReceived = (long)statistics.TimeExceededMessagesReceived;
            _timeExceededMessagesSent = (long)statistics.TimeExceededMessagesSent;
        }

        public override long DestinationUnreachableMessagesReceived { get { return _destinationUnreachableMessagesReceived; } }

        public override long DestinationUnreachableMessagesSent { get { return _destinationUnreachableMessagesSent; } }

        public override long EchoRepliesReceived { get { return _echoRepliesReceived; } }

        public override long EchoRepliesSent { get { return _echoRepliesSent; } }

        public override long EchoRequestsReceived { get { return _echoRequestsReceived; } }

        public override long EchoRequestsSent { get { return _echoRequestsSent; } }

        public override long ErrorsReceived { get { throw new PlatformNotSupportedException(); } }

        public override long ErrorsSent { get { throw new PlatformNotSupportedException(); } }

        public override long MessagesReceived { get { throw new PlatformNotSupportedException(); } }

        public override long MessagesSent { get { throw new PlatformNotSupportedException(); } }

        public override long ParameterProblemsReceived { get { return _parameterProblemsReceived; } }

        public override long ParameterProblemsSent { get { return _parameterProblemsSent; } }

        public override long RedirectsReceived { get { return _redirectsReceived; } }

        public override long RedirectsSent { get { return _redirectsSent; } }

        public override long TimeExceededMessagesReceived { get { return _timeExceededMessagesReceived; } }

        public override long TimeExceededMessagesSent { get { return _timeExceededMessagesSent; } }

        public override long MembershipQueriesReceived { get { return _membershipQueriesReceived; } }

        public override long MembershipQueriesSent { get { return _membershipQueriesSent; } }

        public override long MembershipReductionsReceived { get { return _membershipReductionsReceived; } }

        public override long MembershipReductionsSent { get { return _membershipReductionsSent; } }

        public override long MembershipReportsReceived { get { return _membershipReportsReceived; } }

        public override long MembershipReportsSent { get { return _membershipReportsSent; } }

        public override long NeighborAdvertisementsReceived { get { return _neighborAdvertisementsReceived; } }

        public override long NeighborAdvertisementsSent { get { return _neighborAdvertisementsSent; } }

        public override long NeighborSolicitsReceived { get { return _neighborSolicitsReceived; } }

        public override long NeighborSolicitsSent { get { return _neighborSolicitsSent; } }

        public override long PacketTooBigMessagesReceived { get { return _packetTooBigMessagesReceived; } }

        public override long PacketTooBigMessagesSent { get { return _packetTooBigMessagesSent; } }

        public override long RouterAdvertisementsReceived { get { return _routerAdvertisementsReceived; } }

        public override long RouterAdvertisementsSent { get { return _routerAdvertisementsSent; } }

        public override long RouterSolicitsReceived { get { return _routerSolicitsReceived; } }

        public override long RouterSolicitsSent { get { return _routerSolicitsSent; } }
    }
}
