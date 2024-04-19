// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Mime;
using System.Text;

namespace System.Net.Mail
{
    [Flags]
    public enum DeliveryNotificationOptions
    {
        None = 0,
        OnSuccess = 1,
        OnFailure = 2,
        Delay = 4,
        Never = (int)0x08000000
    }

    public class MailMessage : IDisposable
    {
        private AlternateViewCollection? _views;
        private AttachmentCollection? _attachments;
        private AlternateView? _bodyView;
        private string? _body = string.Empty;
        private Encoding? _bodyEncoding;
        private TransferEncoding _bodyTransferEncoding = TransferEncoding.Unknown;
        private bool _isBodyHtml;
        private bool _disposed;
        private readonly Message _message;
        private DeliveryNotificationOptions _deliveryStatusNotification = DeliveryNotificationOptions.None;

        public MailMessage()
        {
            _message = new Message();
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Associate(this, _message);
        }

        public MailMessage(string from, string to)
        {
            ArgumentException.ThrowIfNullOrEmpty(from);
            ArgumentException.ThrowIfNullOrEmpty(to);

            _message = new Message(from, to);
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Associate(this, _message);
        }


        public MailMessage(string from, string to, string? subject, string? body) : this(from, to)
        {
            Subject = subject;
            Body = body;
        }


        public MailMessage(MailAddress from, MailAddress to)
        {
            ArgumentNullException.ThrowIfNull(from);
            ArgumentNullException.ThrowIfNull(to);

            _message = new Message(from, to);
        }

        [DisallowNull]
        public MailAddress? From
        {
            get
            {
                return _message.From;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _message.From = value;
            }
        }

        [DisallowNull]
        public MailAddress? Sender
        {
            get
            {
                return _message.Sender;
            }
            set
            {
                _message.Sender = value;
            }
        }

        [Obsolete("ReplyTo has been deprecated. Use ReplyToList instead, which can accept multiple addresses.")]
        public MailAddress? ReplyTo
        {
            get
            {
                return _message.ReplyTo;
            }
            set
            {
                _message.ReplyTo = value;
            }
        }

        public MailAddressCollection ReplyToList
        {
            get
            {
                return _message.ReplyToList;
            }
        }

        public MailAddressCollection To
        {
            get
            {
                return _message.To;
            }
        }

        public MailAddressCollection Bcc
        {
            get
            {
                return _message.Bcc;
            }
        }

        public MailAddressCollection CC
        {
            get
            {
                return _message.CC;
            }
        }

        public MailPriority Priority
        {
            get
            {
                return _message.Priority;
            }
            set
            {
                _message.Priority = value;
            }
        }

        public DeliveryNotificationOptions DeliveryNotificationOptions
        {
            get
            {
                return _deliveryStatusNotification;
            }
            set
            {
                if (7 < (uint)value && value != DeliveryNotificationOptions.Never)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _deliveryStatusNotification = value;
            }
        }

        [AllowNull]
        public string Subject
        {
            get
            {
                return _message.Subject ?? string.Empty;
            }
            set
            {
                _message.Subject = value;
            }
        }

        public Encoding? SubjectEncoding
        {
            get
            {
                return _message.SubjectEncoding;
            }
            set
            {
                _message.SubjectEncoding = value;
            }
        }

        public NameValueCollection Headers
        {
            get
            {
                return _message.Headers;
            }
        }

        public Encoding? HeadersEncoding
        {
            get
            {
                return _message.HeadersEncoding;
            }
            set
            {
                _message.HeadersEncoding = value;
            }
        }

        [AllowNull]
        public string Body
        {
            get
            {
                return _body ?? string.Empty;
            }

            set
            {
                _body = value;

                if (_bodyEncoding == null && _body != null)
                {
                    if (MimeBasePart.IsAscii(_body, true))
                    {
                        _bodyEncoding = Text.Encoding.ASCII;
                    }
                    else
                    {
                        _bodyEncoding = Text.Encoding.GetEncoding(MimeBasePart.DefaultCharSet);
                    }
                }
            }
        }

        public Encoding? BodyEncoding
        {
            get
            {
                return _bodyEncoding;
            }
            set
            {
                _bodyEncoding = value;
            }
        }

        public TransferEncoding BodyTransferEncoding
        {
            get
            {
                return _bodyTransferEncoding;
            }
            set
            {
                _bodyTransferEncoding = value;
            }
        }


        public bool IsBodyHtml
        {
            get
            {
                return _isBodyHtml;
            }
            set
            {
                _isBodyHtml = value;
            }
        }


        public AttachmentCollection Attachments
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                return _attachments ??= new AttachmentCollection();
            }
        }
        public AlternateViewCollection AlternateViews
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                return _views ??= new AlternateViewCollection();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                _views?.Dispose();
                _attachments?.Dispose();
                _bodyView?.Dispose();
            }
        }


        private void SetContent(bool allowUnicode)
        {
            //the attachments may have changed, so we need to reset the message
            if (_bodyView != null)
            {
                _bodyView.Dispose();
                _bodyView = null;
            }

            if (AlternateViews.Count == 0 && Attachments.Count == 0)
            {
                if (!string.IsNullOrEmpty(_body))
                {
                    _bodyView = AlternateView.CreateAlternateViewFromString(_body, _bodyEncoding, (_isBodyHtml ? MediaTypeNames.Text.Html : null));
                    _message.Content = _bodyView.MimePart;
                }
            }
            else if (AlternateViews.Count == 0 && Attachments.Count > 0)
            {
                MimeMultiPart part = new MimeMultiPart(MimeMultiPartType.Mixed);

                if (!string.IsNullOrEmpty(_body))
                {
                    _bodyView = AlternateView.CreateAlternateViewFromString(_body, _bodyEncoding, (_isBodyHtml ? MediaTypeNames.Text.Html : null));
                }
                else
                {
                    _bodyView = AlternateView.CreateAlternateViewFromString(string.Empty);
                }

                part.Parts.Add(_bodyView.MimePart);

                foreach (Attachment attachment in Attachments)
                {
                    if (attachment != null)
                    {
                        //ensure we can read from the stream.
                        attachment.PrepareForSending(allowUnicode);
                        part.Parts.Add(attachment.MimePart);
                    }
                }
                _message.Content = part;
            }
            else
            {
                // we should not unnecessarily use Multipart/Mixed
                // When there is no attachement and all the alternative views are of "Alternative" types.
                MimeMultiPart? part;
                MimeMultiPart viewsPart = new MimeMultiPart(MimeMultiPartType.Alternative);

                if (!string.IsNullOrEmpty(_body))
                {
                    _bodyView = AlternateView.CreateAlternateViewFromString(_body, _bodyEncoding, null);
                    viewsPart.Parts.Add(_bodyView.MimePart);
                }

                foreach (AlternateView view in AlternateViews)
                {
                    //ensure we can read from the stream.
                    if (view != null)
                    {
                        view.PrepareForSending(allowUnicode);
                        if (view.LinkedResources.Count > 0)
                        {
                            MimeMultiPart wholeView = new MimeMultiPart(MimeMultiPartType.Related);
                            wholeView.ContentType.Parameters["type"] = view.ContentType.MediaType;
                            wholeView.ContentLocation = view.MimePart.ContentLocation;
                            wholeView.Parts.Add(view.MimePart);

                            foreach (LinkedResource resource in view.LinkedResources)
                            {
                                //ensure we can read from the stream.
                                resource.PrepareForSending(allowUnicode);

                                wholeView.Parts.Add(resource.MimePart);
                            }
                            viewsPart.Parts.Add(wholeView);
                        }
                        else
                        {
                            viewsPart.Parts.Add(view.MimePart);
                        }
                    }
                }

                if (Attachments.Count > 0)
                {
                    part = new MimeMultiPart(MimeMultiPartType.Mixed);
                    part.Parts.Add(viewsPart);

                    foreach (Attachment attachment in Attachments)
                    {
                        if (attachment != null)
                        {
                            //ensure we can read from the stream.
                            attachment.PrepareForSending(allowUnicode);
                            part.Parts.Add(attachment.MimePart);
                        }
                    }
                    _message.Content = part;
                }
                // If there is no Attachement, AND only "1" Alternate View AND !!no body!!
                // then in fact, this is NOT a multipart region.
                else if (viewsPart.Parts.Count == 1 && string.IsNullOrEmpty(_body))
                {
                    _message.Content = viewsPart.Parts[0];
                }
                else
                {
                    _message.Content = viewsPart;
                }
            }

            if (_bodyView != null && _bodyTransferEncoding != TransferEncoding.Unknown)
            {
                _bodyView.TransferEncoding = _bodyTransferEncoding;
            }
        }

        internal void Send(BaseWriter writer, bool sendEnvelope, bool allowUnicode)
        {
            SetContent(allowUnicode);
            _message.Send(writer, sendEnvelope, allowUnicode);
        }

        internal IAsyncResult BeginSend(BaseWriter writer, bool allowUnicode,
            AsyncCallback? callback, object? state)
        {
            SetContent(allowUnicode);
            return _message.BeginSend(writer, allowUnicode, callback, state);
        }

        internal void EndSend(IAsyncResult asyncResult)
        {
            _message.EndSend(asyncResult);
        }

        internal string BuildDeliveryStatusNotificationString()
        {
            if (_deliveryStatusNotification != DeliveryNotificationOptions.None)
            {
                StringBuilder s = new StringBuilder(" NOTIFY=");

                bool oneSet = false;

                //none
                if (_deliveryStatusNotification == DeliveryNotificationOptions.Never)
                {
                    s.Append("NEVER");
                    return s.ToString();
                }

                if ((((int)_deliveryStatusNotification) & (int)DeliveryNotificationOptions.OnSuccess) > 0)
                {
                    s.Append("SUCCESS");
                    oneSet = true;
                }
                if ((((int)_deliveryStatusNotification) & (int)DeliveryNotificationOptions.OnFailure) > 0)
                {
                    if (oneSet)
                    {
                        s.Append(',');
                    }
                    s.Append("FAILURE");
                    oneSet = true;
                }
                if ((((int)_deliveryStatusNotification) & (int)DeliveryNotificationOptions.Delay) > 0)
                {
                    if (oneSet)
                    {
                        s.Append(',');
                    }
                    s.Append("DELAY");
                }
                return s.ToString();
            }
            return string.Empty;
        }
    }
}
