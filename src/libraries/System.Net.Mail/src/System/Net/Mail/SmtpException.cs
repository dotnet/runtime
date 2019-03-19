// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Net.Mail
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class SmtpException : Exception, ISerializable
    {
        private SmtpStatusCode _statusCode = SmtpStatusCode.GeneralFailure;

        private static string GetMessageForStatus(SmtpStatusCode statusCode, string serverResponse)
        {
            return GetMessageForStatus(statusCode) + " " + SR.Format(SR.MailServerResponse, serverResponse);
        }

        private static string GetMessageForStatus(SmtpStatusCode statusCode)
        {
            switch (statusCode)
            {
                default:
                case SmtpStatusCode.CommandUnrecognized:
                    return SR.SmtpCommandUnrecognized;
                case SmtpStatusCode.SyntaxError:
                    return SR.SmtpSyntaxError;
                case SmtpStatusCode.CommandNotImplemented:
                    return SR.SmtpCommandNotImplemented;
                case SmtpStatusCode.BadCommandSequence:
                    return SR.SmtpBadCommandSequence;
                case SmtpStatusCode.CommandParameterNotImplemented:
                    return SR.SmtpCommandParameterNotImplemented;
                case SmtpStatusCode.SystemStatus:
                    return SR.SmtpSystemStatus;
                case SmtpStatusCode.HelpMessage:
                    return SR.SmtpHelpMessage;
                case SmtpStatusCode.ServiceReady:
                    return SR.SmtpServiceReady;
                case SmtpStatusCode.ServiceClosingTransmissionChannel:
                    return SR.SmtpServiceClosingTransmissionChannel;
                case SmtpStatusCode.ServiceNotAvailable:
                    return SR.SmtpServiceNotAvailable;
                case SmtpStatusCode.Ok:
                    return SR.SmtpOK;
                case SmtpStatusCode.UserNotLocalWillForward:
                    return SR.SmtpUserNotLocalWillForward;
                case SmtpStatusCode.MailboxBusy:
                    return SR.SmtpMailboxBusy;
                case SmtpStatusCode.MailboxUnavailable:
                    return SR.SmtpMailboxUnavailable;
                case SmtpStatusCode.LocalErrorInProcessing:
                    return SR.SmtpLocalErrorInProcessing;
                case SmtpStatusCode.UserNotLocalTryAlternatePath:
                    return SR.SmtpUserNotLocalTryAlternatePath;
                case SmtpStatusCode.InsufficientStorage:
                    return SR.SmtpInsufficientStorage;
                case SmtpStatusCode.ExceededStorageAllocation:
                    return SR.SmtpExceededStorageAllocation;
                case SmtpStatusCode.MailboxNameNotAllowed:
                    return SR.SmtpMailboxNameNotAllowed;
                case SmtpStatusCode.StartMailInput:
                    return SR.SmtpStartMailInput;
                case SmtpStatusCode.TransactionFailed:
                    return SR.SmtpTransactionFailed;
                case SmtpStatusCode.ClientNotPermitted:
                    return SR.SmtpClientNotPermitted;
                case SmtpStatusCode.MustIssueStartTlsFirst:
                    return SR.SmtpMustIssueStartTlsFirst;
            }
        }

        public SmtpException(SmtpStatusCode statusCode) : base(GetMessageForStatus(statusCode))
        {
            _statusCode = statusCode;
        }

        public SmtpException(SmtpStatusCode statusCode, string message) : base(message)
        {
            _statusCode = statusCode;
        }

        public SmtpException() : this(SmtpStatusCode.GeneralFailure)
        {
        }

        public SmtpException(string message) : base(message)
        {
        }

        public SmtpException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SmtpException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
            _statusCode = (SmtpStatusCode)serializationInfo.GetInt32("Status");
        }

        internal SmtpException(SmtpStatusCode statusCode, string serverMessage, bool serverResponse) : base(GetMessageForStatus(statusCode, serverMessage))
        {
            _statusCode = statusCode;
        }

        internal SmtpException(string message, string serverResponse) : base(message + " " + SR.Format(SR.MailServerResponse, serverResponse))
        {
        }

        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            GetObjectData(serializationInfo, streamingContext);
        }

        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
            serializationInfo.AddValue("Status", (int)_statusCode, typeof(int));
        }

        public SmtpStatusCode StatusCode
        {
            get
            {
                return _statusCode;
            }
            set
            {
                _statusCode = value;
            }
        }
    }
}
