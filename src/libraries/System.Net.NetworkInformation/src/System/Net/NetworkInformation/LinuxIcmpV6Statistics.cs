// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace System.Net.NetworkInformation
{
    internal class LinuxIcmpV6Statistics : IcmpV6Statistics
    {
        private int _inDestUnreachs;
        private int _outDestUnreachs;
        private int _inEchoReplies;
        private int _inEchos;
        private int _outEchoReplies;
        private int _outEchos;
        private int _inErrors;
        private int _outErrors;
        private int _inGroupMembQueries;
        private int _outInGroupMembQueries;
        private int _inGroupMembReductions;
        private int _outGroupMembReductions;
        private int _inGroupMembResponses;
        private int _outGroupMembResponses;
        private int _inMsgs;
        private int _outMsgs;
        private int _inNeighborAdvertisements;
        private int _outNeighborAdvertisements;
        private int _inNeighborSolicits;
        private int _outNeighborSolicits;
        private int _inPktTooBigs;
        private int _outPktTooBigs;
        private int _inParmProblems;
        private int _outParmProblems;
        private int _inRedirects;
        private int _outRedirects;
        private int _inRouterSolicits;
        private int _outRouterSolicits;
        private int _inRouterAdvertisements;
        private int _outRouterAdvertisements;
        private int _inTimeExcds;
        private int _outTimeExcds;

        public static IcmpV6Statistics CreateIcmpV6Statistics()
        {
            LinuxIcmpV6Statistics stats = new LinuxIcmpV6Statistics();

            string fileContents = File.ReadAllText(NetworkFiles.SnmpV6StatsFile);

            RowConfigReader reader = new RowConfigReader(fileContents);

            stats._inMsgs = reader.GetNextValueAsInt32("Icmp6InMsgs");
            stats._inErrors = reader.GetNextValueAsInt32("Icmp6InErrors");
            stats._outMsgs = reader.GetNextValueAsInt32("Icmp6OutMsgs");
            stats._outErrors = reader.GetNextValueAsInt32("Icmp6OutErrors");
            stats._inDestUnreachs = reader.GetNextValueAsInt32("Icmp6InDestUnreachs");
            stats._inPktTooBigs = reader.GetNextValueAsInt32("Icmp6InPktTooBigs");
            stats._inTimeExcds = reader.GetNextValueAsInt32("Icmp6InTimeExcds");
            stats._inParmProblems = reader.GetNextValueAsInt32("Icmp6InParmProblems");
            stats._inEchos = reader.GetNextValueAsInt32("Icmp6InEchos");
            stats._inEchoReplies = reader.GetNextValueAsInt32("Icmp6InEchoReplies");
            stats._inGroupMembQueries = reader.GetNextValueAsInt32("Icmp6InGroupMembQueries");
            stats._inGroupMembResponses = reader.GetNextValueAsInt32("Icmp6InGroupMembResponses");
            stats._inGroupMembReductions = reader.GetNextValueAsInt32("Icmp6InGroupMembReductions");
            stats._inRouterSolicits = reader.GetNextValueAsInt32("Icmp6InRouterSolicits");
            stats._inRouterAdvertisements = reader.GetNextValueAsInt32("Icmp6InRouterAdvertisements");
            stats._inNeighborSolicits = reader.GetNextValueAsInt32("Icmp6InNeighborSolicits");
            stats._inNeighborAdvertisements = reader.GetNextValueAsInt32("Icmp6InNeighborAdvertisements");
            stats._inRedirects = reader.GetNextValueAsInt32("Icmp6InRedirects");
            stats._outDestUnreachs = reader.GetNextValueAsInt32("Icmp6OutDestUnreachs");
            stats._outPktTooBigs = reader.GetNextValueAsInt32("Icmp6OutPktTooBigs");
            stats._outTimeExcds = reader.GetNextValueAsInt32("Icmp6OutTimeExcds");
            stats._outParmProblems = reader.GetNextValueAsInt32("Icmp6OutParmProblems");
            stats._outEchos = reader.GetNextValueAsInt32("Icmp6OutEchos");
            stats._outEchoReplies = reader.GetNextValueAsInt32("Icmp6OutEchoReplies");
            stats._outInGroupMembQueries = reader.GetNextValueAsInt32("Icmp6OutGroupMembQueries");
            stats._outGroupMembResponses = reader.GetNextValueAsInt32("Icmp6OutGroupMembResponses");
            stats._outGroupMembReductions = reader.GetNextValueAsInt32("Icmp6OutGroupMembReductions");
            stats._outRouterSolicits = reader.GetNextValueAsInt32("Icmp6OutRouterSolicits");
            stats._outRouterAdvertisements = reader.GetNextValueAsInt32("Icmp6OutRouterAdvertisements");
            stats._outNeighborSolicits = reader.GetNextValueAsInt32("Icmp6OutNeighborSolicits");
            stats._outNeighborAdvertisements = reader.GetNextValueAsInt32("Icmp6OutNeighborAdvertisements");
            stats._outRedirects = reader.GetNextValueAsInt32("Icmp6OutRedirects");

            return stats;
        }

        public override long DestinationUnreachableMessagesReceived { get { return _inDestUnreachs; } }
        public override long DestinationUnreachableMessagesSent { get { return _outDestUnreachs; } }
        public override long EchoRepliesReceived { get { return _inEchoReplies; } }
        public override long EchoRepliesSent { get { return _outEchoReplies; } }
        public override long EchoRequestsReceived { get { return _inEchos; } }
        public override long EchoRequestsSent { get { return _outEchos; } }
        public override long ErrorsReceived { get { return _inErrors; } }
        public override long ErrorsSent { get { return _outErrors; } }
        public override long MembershipQueriesReceived { get { return _inGroupMembQueries; } }
        public override long MembershipQueriesSent { get { return _outInGroupMembQueries; } }
        public override long MembershipReductionsReceived { get { return _inGroupMembReductions; } }
        public override long MembershipReductionsSent { get { return _outGroupMembReductions; } }
        public override long MembershipReportsReceived { get { return _inGroupMembResponses; } }
        public override long MembershipReportsSent { get { return _outGroupMembResponses; } }
        public override long MessagesReceived { get { return _inMsgs; } }
        public override long MessagesSent { get { return _outMsgs; } }
        public override long NeighborAdvertisementsReceived { get { return _inNeighborAdvertisements; } }
        public override long NeighborAdvertisementsSent { get { return _outNeighborAdvertisements; } }
        public override long NeighborSolicitsReceived { get { return _inNeighborSolicits; } }
        public override long NeighborSolicitsSent { get { return _outNeighborSolicits; } }
        public override long PacketTooBigMessagesReceived { get { return _inPktTooBigs; } }
        public override long PacketTooBigMessagesSent { get { return _outPktTooBigs; } }
        public override long ParameterProblemsReceived { get { return _inParmProblems; } }
        public override long ParameterProblemsSent { get { return _outParmProblems; } }
        public override long RedirectsReceived { get { return _inRedirects; } }
        public override long RedirectsSent { get { return _outRedirects; } }
        public override long RouterAdvertisementsReceived { get { return _inRouterAdvertisements; } }
        public override long RouterAdvertisementsSent { get { return _outRouterAdvertisements; } }
        public override long RouterSolicitsReceived { get { return _inRouterSolicits; } }
        public override long RouterSolicitsSent { get { return _outRouterSolicits; } }
        public override long TimeExceededMessagesReceived { get { return _inTimeExcds; } }
        public override long TimeExceededMessagesSent { get { return _outTimeExcds; } }
    }
}
