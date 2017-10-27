// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace System.Net.Mail
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class SmtpFailedRecipientException : SmtpException, ISerializable
    {
        private string _failedRecipient;
        internal bool fatal;

        public SmtpFailedRecipientException() : base() { }

        public SmtpFailedRecipientException(string message) : base(message) { }

        public SmtpFailedRecipientException(string message, Exception innerException) : base(message, innerException) { }

        protected SmtpFailedRecipientException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _failedRecipient = info.GetString("failedRecipient");
        }

        public SmtpFailedRecipientException(SmtpStatusCode statusCode, string failedRecipient) : base(statusCode)
        {
            _failedRecipient = failedRecipient;
        }

        public SmtpFailedRecipientException(SmtpStatusCode statusCode, string failedRecipient, string serverResponse) : base(statusCode, serverResponse, true)
        {
            _failedRecipient = failedRecipient;
        }

        public SmtpFailedRecipientException(string message, string failedRecipient, Exception innerException) : base(message, innerException)
        {
            _failedRecipient = failedRecipient;
        }

        public string FailedRecipient
        {
            get
            {
                return _failedRecipient;
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Justification = "System.dll is still using pre-v4 security model and needs this demand")]
        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            GetObjectData(serializationInfo, streamingContext);
        }

        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
            serializationInfo.AddValue("failedRecipient", _failedRecipient, typeof(string));
        }
    }
}

