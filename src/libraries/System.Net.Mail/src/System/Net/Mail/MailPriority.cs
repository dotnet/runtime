// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Mime;
using System.Runtime.ExceptionServices;
using System.Text;

namespace System.Net.Mail
{
    public enum MailPriority
    {
        Normal = 0,
        Low = 1,
        High = 2
    }

    internal class Message
    {
        #region Fields

        private MailAddress _from;
        private MailAddress _sender;
        private MailAddressCollection _replyToList;
        private MailAddress _replyTo;
        private MailAddressCollection _to;
        private MailAddressCollection _cc;
        private MailAddressCollection _bcc;
        private MimeBasePart _content;
        private HeaderCollection _headers;
        private HeaderCollection _envelopeHeaders;
        private string _subject;
        private Encoding _subjectEncoding;
        private Encoding _headersEncoding;
        private MailPriority _priority = (MailPriority)(-1);

        #endregion Fields

        #region Constructors

        internal Message()
        {
        }

        internal Message(string from, string to) : this()
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            if (to == null)
                throw new ArgumentNullException(nameof(to));

            if (from == string.Empty)
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(from)), nameof(from));

            if (to == string.Empty)
                throw new ArgumentException(SR.Format(SR.net_emptystringcall, nameof(to)), nameof(to));

            _from = new MailAddress(from);
            MailAddressCollection collection = new MailAddressCollection();
            collection.Add(to);
            _to = collection;
        }


        internal Message(MailAddress from, MailAddress to) : this()
        {
            _from = from;
            To.Add(to);
        }

        #endregion Constructors

        #region Properties

        public MailPriority Priority
        {
            get
            {
                return (((int)_priority == -1) ? MailPriority.Normal : _priority);
            }
            set
            {
                _priority = value;
            }
        }

        internal MailAddress From
        {
            get
            {
                return _from;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _from = value;
            }
        }


        internal MailAddress Sender
        {
            get
            {
                return _sender;
            }
            set
            {
                _sender = value;
            }
        }


        internal MailAddress ReplyTo
        {
            get
            {
                return _replyTo;
            }
            set
            {
                _replyTo = value;
            }
        }

        internal MailAddressCollection ReplyToList
        {
            get
            {
                if (_replyToList == null)
                    _replyToList = new MailAddressCollection();

                return _replyToList;
            }
        }

        internal MailAddressCollection To
        {
            get
            {
                if (_to == null)
                    _to = new MailAddressCollection();

                return _to;
            }
        }

        internal MailAddressCollection Bcc
        {
            get
            {
                if (_bcc == null)
                    _bcc = new MailAddressCollection();

                return _bcc;
            }
        }

        internal MailAddressCollection CC
        {
            get
            {
                if (_cc == null)
                    _cc = new MailAddressCollection();

                return _cc;
            }
        }


        internal string Subject
        {
            get
            {
                return _subject;
            }
            set
            {
                Encoding inputEncoding = null;
                try
                {
                    // extract the encoding from =?encoding?BorQ?blablalba?=
                    inputEncoding = MimeBasePart.DecodeEncoding(value);
                }
                catch (ArgumentException) { };

                if (inputEncoding != null && value != null)
                {
                    try
                    {
                        // Store the decoded value, we'll re-encode before sending
                        value = MimeBasePart.DecodeHeaderValue(value);
                        _subjectEncoding = _subjectEncoding ?? inputEncoding;
                    }
                    // Failed to decode, just pass it through as ascii (legacy)
                    catch (FormatException) { }
                }

                if (value != null && MailBnfHelper.HasCROrLF(value))
                {
                    throw new ArgumentException(SR.MailSubjectInvalidFormat);
                }
                _subject = value;

                if (_subject != null)
                {
                    _subject = _subject.Normalize(NormalizationForm.FormC);
                    if (_subjectEncoding == null && !MimeBasePart.IsAscii(_subject, false))
                    {
                        _subjectEncoding = Encoding.GetEncoding(MimeBasePart.DefaultCharSet);
                    }
                }
            }
        }

        internal Encoding SubjectEncoding
        {
            get
            {
                return _subjectEncoding;
            }
            set
            {
                _subjectEncoding = value;
            }
        }

        internal HeaderCollection Headers
        {
            get
            {
                if (_headers == null)
                {
                    _headers = new HeaderCollection();
                    if (NetEventSource.IsEnabled) NetEventSource.Associate(this, _headers);
                }

                return _headers;
            }
        }

        internal Encoding HeadersEncoding
        {
            get
            {
                return _headersEncoding;
            }
            set
            {
                _headersEncoding = value;
            }
        }

        internal HeaderCollection EnvelopeHeaders
        {
            get
            {
                if (_envelopeHeaders == null)
                {
                    _envelopeHeaders = new HeaderCollection();
                    if (NetEventSource.IsEnabled) NetEventSource.Associate(this, _envelopeHeaders);
                }

                return _envelopeHeaders;
            }
        }


        internal virtual MimeBasePart Content
        {
            get
            {
                return _content;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _content = value;
            }
        }

        #endregion Properties

        #region Sending

        internal void EmptySendCallback(IAsyncResult result)
        {
            Exception e = null;

            if (result.CompletedSynchronously)
            {
                return;
            }

            EmptySendContext context = (EmptySendContext)result.AsyncState;
            try
            {
                context._writer.EndGetContentStream(result).Close();
            }
            catch (Exception ex)
            {
                e = ex;
            }
            context._result.InvokeCallback(e);
        }

        internal class EmptySendContext
        {
            internal EmptySendContext(BaseWriter writer, LazyAsyncResult result)
            {
                _writer = writer;
                _result = result;
            }

            internal LazyAsyncResult _result;
            internal BaseWriter _writer;
        }

        internal virtual IAsyncResult BeginSend(BaseWriter writer, bool sendEnvelope, bool allowUnicode,
            AsyncCallback callback, object state)
        {
            PrepareHeaders(sendEnvelope, allowUnicode);
            writer.WriteHeaders(Headers, allowUnicode);

            if (Content != null)
            {
                return Content.BeginSend(writer, callback, allowUnicode, state);
            }
            else
            {
                LazyAsyncResult result = new LazyAsyncResult(this, state, callback);
                IAsyncResult newResult = writer.BeginGetContentStream(EmptySendCallback, new EmptySendContext(writer, result));
                if (newResult.CompletedSynchronously)
                {
                    writer.EndGetContentStream(newResult).Close();
                }
                return result;
            }
        }

        internal virtual void EndSend(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            if (Content != null)
            {
                Content.EndSend(asyncResult);
            }
            else
            {
                LazyAsyncResult castedAsyncResult = asyncResult as LazyAsyncResult;

                if (castedAsyncResult == null || castedAsyncResult.AsyncObject != this)
                {
                    throw new ArgumentException(SR.net_io_invalidasyncresult);
                }

                if (castedAsyncResult.EndCalled)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, nameof(EndSend)));
                }

                castedAsyncResult.InternalWaitForCompletion();
                castedAsyncResult.EndCalled = true;
                if (castedAsyncResult.Result is Exception e)
                {
                    ExceptionDispatchInfo.Throw(e);
                }
            }
        }

        internal virtual void Send(BaseWriter writer, bool sendEnvelope, bool allowUnicode)
        {
            if (sendEnvelope)
            {
                PrepareEnvelopeHeaders(sendEnvelope, allowUnicode);
                writer.WriteHeaders(EnvelopeHeaders, allowUnicode);
            }

            PrepareHeaders(sendEnvelope, allowUnicode);
            writer.WriteHeaders(Headers, allowUnicode);

            if (Content != null)
            {
                Content.Send(writer, allowUnicode);
            }
            else
            {
                writer.GetContentStream().Close();
            }
        }

        internal void PrepareEnvelopeHeaders(bool sendEnvelope, bool allowUnicode)
        {
            if (_headersEncoding == null)
            {
                _headersEncoding = Encoding.GetEncoding(MimeBasePart.DefaultCharSet);
            }

            EncodeHeaders(EnvelopeHeaders, allowUnicode);

            // Only add X-Sender header if it wasn't already set by the user
            string xSenderHeader = MailHeaderInfo.GetString(MailHeaderID.XSender);
            if (!IsHeaderSet(xSenderHeader))
            {
                MailAddress sender = Sender ?? From;
                EnvelopeHeaders.InternalSet(xSenderHeader, sender.Encode(xSenderHeader.Length, allowUnicode));
            }

            string headerName = MailHeaderInfo.GetString(MailHeaderID.XReceiver);
            EnvelopeHeaders.Remove(headerName);

            foreach (MailAddress address in To)
            {
                EnvelopeHeaders.InternalAdd(headerName, address.Encode(headerName.Length, allowUnicode));
            }
            foreach (MailAddress address in CC)
            {
                EnvelopeHeaders.InternalAdd(headerName, address.Encode(headerName.Length, allowUnicode));
            }
            foreach (MailAddress address in Bcc)
            {
                EnvelopeHeaders.InternalAdd(headerName, address.Encode(headerName.Length, allowUnicode));
            }
        }

        internal void PrepareHeaders(bool sendEnvelope, bool allowUnicode)
        {
            string headerName;

            if (_headersEncoding == null)
            {
                _headersEncoding = Encoding.GetEncoding(MimeBasePart.DefaultCharSet);
            }

            //ContentType is written directly to the stream so remove potential user duplicate
            Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.ContentType));

            Headers[MailHeaderInfo.GetString(MailHeaderID.MimeVersion)] = "1.0";

            // add sender to headers first so that it is written first to allow the IIS smtp svc to
            // send MAIL FROM with the sender if both sender and from are present
            headerName = MailHeaderInfo.GetString(MailHeaderID.Sender);
            if (Sender != null)
            {
                Headers.InternalAdd(headerName, Sender.Encode(headerName.Length, allowUnicode));
            }
            else
            {
                Headers.Remove(headerName);
            }

            headerName = MailHeaderInfo.GetString(MailHeaderID.From);
            Headers.InternalAdd(headerName, From.Encode(headerName.Length, allowUnicode));

            headerName = MailHeaderInfo.GetString(MailHeaderID.To);
            if (To.Count > 0)
            {
                Headers.InternalAdd(headerName, To.Encode(headerName.Length, allowUnicode));
            }
            else
            {
                Headers.Remove(headerName);
            }

            headerName = MailHeaderInfo.GetString(MailHeaderID.Cc);
            if (CC.Count > 0)
            {
                Headers.InternalAdd(headerName, CC.Encode(headerName.Length, allowUnicode));
            }
            else
            {
                Headers.Remove(headerName);
            }

            headerName = MailHeaderInfo.GetString(MailHeaderID.ReplyTo);
            if (ReplyTo != null)
            {
                Headers.InternalAdd(headerName, ReplyTo.Encode(headerName.Length, allowUnicode));
            }
            else if (ReplyToList.Count > 0)
            {
                Headers.InternalAdd(headerName, ReplyToList.Encode(headerName.Length, allowUnicode));
            }
            else
            {
                Headers.Remove(headerName);
            }

            Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.Bcc));

            if (_priority == MailPriority.High)
            {
                Headers[MailHeaderInfo.GetString(MailHeaderID.XPriority)] = "1";
                Headers[MailHeaderInfo.GetString(MailHeaderID.Priority)] = "urgent";
                Headers[MailHeaderInfo.GetString(MailHeaderID.Importance)] = "high";
            }
            else if (_priority == MailPriority.Low)
            {
                Headers[MailHeaderInfo.GetString(MailHeaderID.XPriority)] = "5";
                Headers[MailHeaderInfo.GetString(MailHeaderID.Priority)] = "non-urgent";
                Headers[MailHeaderInfo.GetString(MailHeaderID.Importance)] = "low";
            }
            //if the priority was never set, allow the app to set the headers directly.
            else if (((int)_priority) != -1)
            {
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.XPriority));
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.Priority));
                Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.Importance));
            }

            Headers.InternalAdd(MailHeaderInfo.GetString(MailHeaderID.Date),
                MailBnfHelper.GetDateTimeString(DateTime.Now, null));

            headerName = MailHeaderInfo.GetString(MailHeaderID.Subject);
            if (!string.IsNullOrEmpty(_subject))
            {
                if (allowUnicode)
                {
                    Headers.InternalAdd(headerName, _subject);
                }
                else
                {
                    Headers.InternalAdd(headerName,
                        MimeBasePart.EncodeHeaderValue(_subject, _subjectEncoding,
                        MimeBasePart.ShouldUseBase64Encoding(_subjectEncoding),
                        headerName.Length));
                }
            }
            else
            {
                Headers.Remove(headerName);
            }

            EncodeHeaders(_headers, allowUnicode);
        }

        internal void EncodeHeaders(HeaderCollection headers, bool allowUnicode)
        {
            if (_headersEncoding == null)
            {
                _headersEncoding = Encoding.GetEncoding(MimeBasePart.DefaultCharSet);
            }

            System.Diagnostics.Debug.Assert(_headersEncoding != null);

            for (int i = 0; i < headers.Count; i++)
            {
                string headerName = headers.GetKey(i);

                //certain well-known values are encoded by PrepareHeaders and PrepareEnvelopeHeaders
                //so we can ignore them because either we encoded them already or there is no 
                //way for the user to have set them.  If a header is well known and user settable then
                //we should encode it here, otherwise we have already encoded it if necessary
                if (!MailHeaderInfo.IsUserSettable(headerName))
                {
                    continue;
                }

                string[] values = headers.GetValues(headerName);
                string encodedValue = string.Empty;
                for (int j = 0; j < values.Length; j++)
                {
                    //encode if we need to
                    if (MimeBasePart.IsAscii(values[j], false)
                         || (allowUnicode && MailHeaderInfo.AllowsUnicode(headerName) // EAI
                            && !MailBnfHelper.HasCROrLF(values[j])))
                    {
                        encodedValue = values[j];
                    }
                    else
                    {
                        encodedValue = MimeBasePart.EncodeHeaderValue(values[j],
                                                        _headersEncoding,
                                                        MimeBasePart.ShouldUseBase64Encoding(_headersEncoding),
                                                        headerName.Length);
                    }

                    //potentially there are multiple values per key
                    if (j == 0)
                    {
                        //if it's the first or only value, set will overwrite all the values assigned to that key
                        //which is fine since we have them stored in values[]
                        headers.Set(headerName, encodedValue);
                    }
                    else
                    {
                        //this is a subsequent key, so we must Add it since the first key will have overwritten the
                        //other values
                        headers.Add(headerName, encodedValue);
                    }
                }
            }
        }

        private bool IsHeaderSet(string headerName)
        {
            for (int i = 0; i < Headers.Count; i++)
            {
                if (string.Equals(Headers.GetKey(i), headerName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion Sending
    }
}
