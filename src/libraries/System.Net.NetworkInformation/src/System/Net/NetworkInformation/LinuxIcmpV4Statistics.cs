// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    internal class LinuxIcmpV4Statistics : IcmpV4Statistics
    {
        private readonly Icmpv4StatisticsTable _table;

        // The table is a fairly large struct (108 bytes), pass it by reference
        public LinuxIcmpV4Statistics()
        {
            _table = StringParsingHelpers.ParseIcmpv4FromSnmpFile(NetworkFiles.SnmpV4StatsFile);
        }

        public override long AddressMaskRepliesReceived { get { return _table.InAddrMaskReps; } }

        public override long AddressMaskRepliesSent { get { return _table.OutAddrMaskReps; } }

        public override long AddressMaskRequestsReceived { get { return _table.InAddrMasks; } }

        public override long AddressMaskRequestsSent { get { return _table.OutAddrMasks; } }

        public override long DestinationUnreachableMessagesReceived { get { return _table.InDestUnreachs; } }

        public override long DestinationUnreachableMessagesSent { get { return _table.OutDestUnreachs; } }

        public override long EchoRepliesReceived { get { return _table.InEchoReps; } }

        public override long EchoRepliesSent { get { return _table.OutEchoReps; } }

        public override long EchoRequestsReceived { get { return _table.InEchos; } }

        public override long EchoRequestsSent { get { return _table.OutEchos; } }

        public override long ErrorsReceived { get { return _table.InErrors; } }

        public override long ErrorsSent { get { return _table.OutErrors; } }

        public override long MessagesReceived { get { return _table.InMsgs; } }

        public override long MessagesSent { get { return _table.OutMsgs; } }

        public override long ParameterProblemsReceived { get { return _table.InParmProbs; } }

        public override long ParameterProblemsSent { get { return _table.OutParmProbs; } }

        public override long RedirectsReceived { get { return _table.InRedirects; } }

        public override long RedirectsSent { get { return _table.OutRedirects; } }

        public override long SourceQuenchesReceived { get { return _table.InSrcQuenchs; } }

        public override long SourceQuenchesSent { get { return _table.OutSrcQuenchs; } }

        public override long TimeExceededMessagesReceived { get { return _table.InTimeExcds; } }

        public override long TimeExceededMessagesSent { get { return _table.OutTimeExcds; } }

        public override long TimestampRepliesReceived { get { return _table.InTimeStampReps; } }

        public override long TimestampRepliesSent { get { return _table.OutTimestampReps; } }

        public override long TimestampRequestsReceived { get { return _table.InTimestamps; } }

        public override long TimestampRequestsSent { get { return _table.OutTimestamps; } }
    }
}
