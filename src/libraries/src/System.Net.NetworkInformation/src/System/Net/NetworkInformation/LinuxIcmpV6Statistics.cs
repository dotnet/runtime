// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    internal class LinuxIcmpV6Statistics : IcmpV6Statistics
    {
        private readonly Icmpv6StatisticsTable _table;

        public LinuxIcmpV6Statistics()
        {
            _table = StringParsingHelpers.ParseIcmpv6FromSnmp6File(NetworkFiles.SnmpV6StatsFile);
        }

        public override long DestinationUnreachableMessagesReceived { get { return _table.InDestUnreachs; } }

        public override long DestinationUnreachableMessagesSent { get { return _table.OutDestUnreachs; } }

        public override long EchoRepliesReceived { get { return _table.InEchoReplies; } }

        public override long EchoRepliesSent { get { return _table.OutEchoReplies; } }

        public override long EchoRequestsReceived { get { return _table.InEchos; } }

        public override long EchoRequestsSent { get { return _table.OutEchos; } }

        public override long ErrorsReceived { get { return _table.InErrors; } }

        public override long ErrorsSent { get { return _table.OutErrors; } }

        public override long MembershipQueriesReceived { get { return _table.InGroupMembQueries; } }

        public override long MembershipQueriesSent { get { return _table.OutInGroupMembQueries; } }

        public override long MembershipReductionsReceived { get { return _table.InGroupMembReductions; } }

        public override long MembershipReductionsSent { get { return _table.OutGroupMembReductions; } }

        public override long MembershipReportsReceived { get { return _table.InGroupMembResponses; } }

        public override long MembershipReportsSent { get { return _table.OutGroupMembResponses; } }

        public override long MessagesReceived { get { return _table.InMsgs; } }

        public override long MessagesSent { get { return _table.OutMsgs; } }

        public override long NeighborAdvertisementsReceived { get { return _table.InNeighborAdvertisements; } }

        public override long NeighborAdvertisementsSent { get { return _table.OutNeighborAdvertisements; } }

        public override long NeighborSolicitsReceived { get { return _table.InNeighborSolicits; } }

        public override long NeighborSolicitsSent { get { return _table.OutNeighborSolicits; } }

        public override long PacketTooBigMessagesReceived { get { return _table.InPktTooBigs; } }

        public override long PacketTooBigMessagesSent { get { return _table.OutPktTooBigs; } }

        public override long ParameterProblemsReceived { get { return _table.InParmProblems; } }

        public override long ParameterProblemsSent { get { return _table.OutParmProblems; } }

        public override long RedirectsReceived { get { return _table.InRedirects; } }

        public override long RedirectsSent { get { return _table.OutRedirects; } }

        public override long RouterAdvertisementsReceived { get { return _table.InRouterAdvertisements; } }

        public override long RouterAdvertisementsSent { get { return _table.OutRouterAdvertisements; } }

        public override long RouterSolicitsReceived { get { return _table.InRouterSolicits; } }

        public override long RouterSolicitsSent { get { return _table.OutRouterSolicits; } }

        public override long TimeExceededMessagesReceived { get { return _table.InTimeExcds; } }

        public override long TimeExceededMessagesSent { get { return _table.OutTimeExcds; } }
    }
}
