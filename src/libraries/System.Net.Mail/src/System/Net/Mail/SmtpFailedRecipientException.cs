// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace System.Net.Mail
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class SmtpFailedRecipientException : SmtpException
    {
        private readonly string? _failedRecipient;

#pragma warning disable CS0649      // Browser - never assigned to
        internal bool fatal;
#pragma warning restore CS0649

        public SmtpFailedRecipientException() : base() { }

        public SmtpFailedRecipientException(string? message) : base(message) { }

        public SmtpFailedRecipientException(string? message, Exception? innerException) : base(message, innerException) { }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected SmtpFailedRecipientException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _failedRecipient = info.GetString("failedRecipient");
        }

        public SmtpFailedRecipientException(SmtpStatusCode statusCode, string? failedRecipient) : base(statusCode)
        {
            _failedRecipient = failedRecipient;
        }

        public SmtpFailedRecipientException(SmtpStatusCode statusCode, string? failedRecipient, string? serverResponse) : base(statusCode, serverResponse, true)
        {
            _failedRecipient = failedRecipient;
        }

        public SmtpFailedRecipientException(string? message, string? failedRecipient, Exception? innerException) : base(message, innerException)
        {
            _failedRecipient = failedRecipient;
        }

        public string? FailedRecipient
        {
            get
            {
                return _failedRecipient;
            }
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
            serializationInfo.AddValue("failedRecipient", _failedRecipient, typeof(string));
        }
    }
}
