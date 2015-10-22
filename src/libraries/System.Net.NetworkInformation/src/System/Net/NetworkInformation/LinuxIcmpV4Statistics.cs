// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace System.Net.NetworkInformation
{
    internal class LinuxIcmpV4Statistics : IcmpV4Statistics
    {
        private int _inMsgs;
        private int _inErrors;
        private int _inCsumErrors;
        private int _inDestUnreachs;
        private int _inTimeExcds;
        private int _inParmProbs;
        private int _inSrcQuenchs;
        private int _inRedirects;
        private int _inEchos;
        private int _inEchoReps;
        private int _inTimestamps;
        private int _inTimeStampReps;
        private int _inAddrMasks;
        private int _inAddrMaskReps;
        private int _outMsgs;
        private int _outErrors;
        private int _outDestUnreachs;
        private int _outTimeExcds;
        private int _outParmProbs;
        private int _outSrcQuenchs;
        private int _outRedirects;
        private int _outEchos;
        private int _outEchoReps;
        private int _outTimestamps;
        private int _outTimestampReps;
        private int _outAddrMasks;
        private int _outAddrMaskReps;

        private LinuxIcmpV4Statistics() { }

        public static IcmpV4Statistics CreateIcmpV4Statistics()
        {
            LinuxIcmpV4Statistics stats = new LinuxIcmpV4Statistics();
            string fileContents = File.ReadAllText(NetworkFiles.SnmpV4StatsFile);
            int firstIpHeader = fileContents.IndexOf("Icmp:");
            int secondIpHeader = fileContents.IndexOf("Icmp:", firstIpHeader + 1);
            int endOfSecondLine = fileContents.IndexOf(Environment.NewLine, secondIpHeader);
            string icmpData = fileContents.Substring(secondIpHeader, endOfSecondLine - secondIpHeader);
            StringParser parser = new StringParser(icmpData, ' ');

            // NOTE: Need to verify that this order is consistent. Otherwise, we need to parse the first-line header
            // to determine the order of information contained in the file.

            parser.MoveNextOrFail(); // Skip Icmp:

            stats._inMsgs = parser.ParseNextInt32();
            stats._inErrors = parser.ParseNextInt32();
            stats._inCsumErrors = parser.ParseNextInt32();
            stats._inDestUnreachs = parser.ParseNextInt32();
            stats._inTimeExcds = parser.ParseNextInt32();
            stats._inParmProbs = parser.ParseNextInt32();
            stats._inSrcQuenchs = parser.ParseNextInt32();
            stats._inRedirects = parser.ParseNextInt32();
            stats._inEchos = parser.ParseNextInt32();
            stats._inEchoReps = parser.ParseNextInt32();
            stats._inTimestamps = parser.ParseNextInt32();
            stats._inTimeStampReps = parser.ParseNextInt32();
            stats._inAddrMasks = parser.ParseNextInt32();
            stats._inAddrMaskReps = parser.ParseNextInt32();
            stats._outMsgs = parser.ParseNextInt32();
            stats._outErrors = parser.ParseNextInt32();
            stats._outDestUnreachs = parser.ParseNextInt32();
            stats._outTimeExcds = parser.ParseNextInt32();
            stats._outParmProbs = parser.ParseNextInt32();
            stats._outSrcQuenchs = parser.ParseNextInt32();
            stats._outRedirects = parser.ParseNextInt32();
            stats._outEchos = parser.ParseNextInt32();
            stats._outEchoReps = parser.ParseNextInt32();
            stats._outTimestamps = parser.ParseNextInt32();
            stats._outTimestampReps = parser.ParseNextInt32();
            stats._outAddrMasks = parser.ParseNextInt32();
            stats._outAddrMaskReps = parser.ParseNextInt32();

            return stats;
        }

        public override long AddressMaskRepliesReceived { get { return _inAddrMaskReps; } }
        public override long AddressMaskRepliesSent { get { return _outAddrMaskReps; } }
        public override long AddressMaskRequestsReceived { get { return _inAddrMasks; } }
        public override long AddressMaskRequestsSent { get { return _outAddrMasks; } }
        public override long DestinationUnreachableMessagesReceived { get { return _inDestUnreachs; } }
        public override long DestinationUnreachableMessagesSent { get { return _outDestUnreachs; } }
        public override long EchoRepliesReceived { get { return _inEchoReps; } }
        public override long EchoRepliesSent { get { return _outEchoReps; } }
        public override long EchoRequestsReceived { get { return _inEchos; } }
        public override long EchoRequestsSent { get { return _outEchos; } }
        public override long ErrorsReceived { get { return _inErrors; } }
        public override long ErrorsSent { get { return _outErrors; } }
        public override long MessagesReceived { get { return _inMsgs; } }
        public override long MessagesSent { get { return _outMsgs; } }
        public override long ParameterProblemsReceived { get { return _inParmProbs; } }
        public override long ParameterProblemsSent { get { return _outParmProbs; } }
        public override long RedirectsReceived { get { return _inRedirects; } }
        public override long RedirectsSent { get { return _outRedirects; } }
        public override long SourceQuenchesReceived { get { return _inSrcQuenchs; } }
        public override long SourceQuenchesSent { get { return _outSrcQuenchs; } }
        public override long TimeExceededMessagesReceived { get { return _inTimeExcds; } }
        public override long TimeExceededMessagesSent { get { return _outTimeExcds; } }
        public override long TimestampRepliesReceived { get { return _inTimeStampReps; } }
        public override long TimestampRepliesSent { get { return _outTimestampReps; } }
        public override long TimestampRequestsReceived { get { return _inTimestamps; } }
        public override long TimestampRequestsSent { get { return _outTimestamps; } }
    }
}
