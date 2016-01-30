// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    internal class OsxIcmpV4Statistics : IcmpV4Statistics
    {
        private readonly long _addressMaskRepliesReceived;
        private readonly long _addressMaskRepliesSent;
        private readonly long _addressMaskRequestsReceived;
        private readonly long _addressMaskRequestsSent;
        private readonly long _destinationUnreachableMessagesReceived;
        private readonly long _destinationUnreachableMessagesSent;
        private readonly long _echoRepliesReceived;
        private readonly long _echoRepliesSent;
        private readonly long _echoRequestsReceived;
        private readonly long _echoRequestsSent;
        private readonly long _parameterProblemsReceived;
        private readonly long _parameterProblemsSent;
        private readonly long _redirectsReceived;
        private readonly long _redirectsSent;
        private readonly long _sourceQuenchesReceived;
        private readonly long _sourceQuenchesSent;
        private readonly long _timeExceededMessagesReceived;
        private readonly long _timeExceededMessagesSent;
        private readonly long _timestampRepliesReceived;
        private readonly long _timestampRepliesSent;
        private readonly long _timestampRequestsReceived;
        private readonly long _timestampRequestsSent;

        public OsxIcmpV4Statistics()
        {
            Interop.Sys.Icmpv4GlobalStatistics statistics;
            if (Interop.Sys.GetIcmpv4GlobalStatistics(out statistics) != 0)
            {
                throw new NetworkInformationException(SR.net_PInvokeError);
            }

            _addressMaskRepliesReceived = (long)statistics.AddressMaskRepliesReceived;
            _addressMaskRepliesSent = (long)statistics.AddressMaskRepliesSent;
            _addressMaskRequestsReceived = (long)statistics.AddressMaskRequestsReceived;
            _addressMaskRequestsSent = (long)statistics.AddressMaskRequestsSent;
            _destinationUnreachableMessagesReceived = (long)statistics.DestinationUnreachableMessagesReceived;
            _destinationUnreachableMessagesSent = (long)statistics.DestinationUnreachableMessagesSent;
            _echoRepliesReceived = (long)statistics.EchoRepliesReceived;
            _echoRepliesSent = (long)statistics.EchoRepliesSent;
            _echoRequestsReceived = (long)statistics.EchoRequestsReceived;
            _echoRequestsSent = (long)statistics.EchoRequestsSent;
            _parameterProblemsReceived = (long)statistics.ParameterProblemsReceived;
            _parameterProblemsSent = (long)statistics.ParameterProblemsSent;
            _redirectsReceived = (long)statistics.RedirectsReceived;
            _redirectsSent = (long)statistics.RedirectsSent;
            _sourceQuenchesReceived = (long)statistics.SourceQuenchesReceived;
            _sourceQuenchesSent = (long)statistics.SourceQuenchesSent;
            _timeExceededMessagesReceived = (long)statistics.TimeExceededMessagesReceived;
            _timeExceededMessagesSent = (long)statistics.TimeExceededMessagesSent;
            _timestampRepliesReceived = (long)statistics.TimestampRepliesReceived;
            _timestampRepliesSent = (long)statistics.TimestampRepliesSent;
            _timestampRequestsReceived = (long)statistics.TimestampRequestsReceived;
            _timestampRequestsSent = (long)statistics.TimestampRequestsSent;
        }

        public override long AddressMaskRepliesReceived { get { return _addressMaskRepliesReceived; } }

        public override long AddressMaskRepliesSent { get { return _addressMaskRepliesSent; } }

        public override long AddressMaskRequestsReceived { get { return _addressMaskRequestsReceived; } }

        public override long AddressMaskRequestsSent { get { return _addressMaskRequestsSent; } }

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

        public override long SourceQuenchesReceived { get { return _sourceQuenchesReceived; } }

        public override long SourceQuenchesSent { get { return _sourceQuenchesSent; } }

        public override long TimeExceededMessagesReceived { get { return _timeExceededMessagesReceived; } }

        public override long TimeExceededMessagesSent { get { return _timeExceededMessagesSent; } }

        public override long TimestampRepliesReceived { get { return _timestampRepliesReceived; } }

        public override long TimestampRepliesSent { get { return _timestampRepliesSent; } }

        public override long TimestampRequestsReceived { get { return _timestampRequestsReceived; } }

        public override long TimestampRequestsSent { get { return _timestampRequestsSent; } }
    }
}
