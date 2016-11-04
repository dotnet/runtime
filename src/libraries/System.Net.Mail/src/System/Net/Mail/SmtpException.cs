// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Net.Mail
{
    [Serializable]
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
                    return SR.Format(SR.SmtpCommandUnrecognized);
                case SmtpStatusCode.SyntaxError:
                    return SR.Format(SR.SmtpSyntaxError);
                case SmtpStatusCode.CommandNotImplemented:
                    return SR.Format(SR.SmtpCommandNotImplemented);
                case SmtpStatusCode.BadCommandSequence:
                    return SR.Format(SR.SmtpBadCommandSequence);
                case SmtpStatusCode.CommandParameterNotImplemented:
                    return SR.Format(SR.SmtpCommandParameterNotImplemented);
                case SmtpStatusCode.SystemStatus:
                    return SR.Format(SR.SmtpSystemStatus);
                case SmtpStatusCode.HelpMessage:
                    return SR.Format(SR.SmtpHelpMessage);
                case SmtpStatusCode.ServiceReady:
                    return SR.Format(SR.SmtpServiceReady);
                case SmtpStatusCode.ServiceClosingTransmissionChannel:
                    return SR.Format(SR.SmtpServiceClosingTransmissionChannel);
                case SmtpStatusCode.ServiceNotAvailable:
                    return SR.Format(SR.SmtpServiceNotAvailable);
                case SmtpStatusCode.Ok:
                    return SR.Format(SR.SmtpOK);
                case SmtpStatusCode.UserNotLocalWillForward:
                    return SR.Format(SR.SmtpUserNotLocalWillForward);
                case SmtpStatusCode.MailboxBusy:
                    return SR.Format(SR.SmtpMailboxBusy);
                case SmtpStatusCode.MailboxUnavailable:
                    return SR.Format(SR.SmtpMailboxUnavailable);
                case SmtpStatusCode.LocalErrorInProcessing:
                    return SR.Format(SR.SmtpLocalErrorInProcessing);
                case SmtpStatusCode.UserNotLocalTryAlternatePath:
                    return SR.Format(SR.SmtpUserNotLocalTryAlternatePath);
                case SmtpStatusCode.InsufficientStorage:
                    return SR.Format(SR.SmtpInsufficientStorage);
                case SmtpStatusCode.ExceededStorageAllocation:
                    return SR.Format(SR.SmtpExceededStorageAllocation);
                case SmtpStatusCode.MailboxNameNotAllowed:
                    return SR.Format(SR.SmtpMailboxNameNotAllowed);
                case SmtpStatusCode.StartMailInput:
                    return SR.Format(SR.SmtpStartMailInput);
                case SmtpStatusCode.TransactionFailed:
                    return SR.Format(SR.SmtpTransactionFailed);
                case SmtpStatusCode.ClientNotPermitted:
                    return SR.Format(SR.SmtpClientNotPermitted);
                case SmtpStatusCode.MustIssueStartTlsFirst:
                    return SR.Format(SR.SmtpMustIssueStartTlsFirst);
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

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            GetObjectData(serializationInfo, streamingContext);
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
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
