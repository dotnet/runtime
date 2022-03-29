// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Net.Mail;
using System.Runtime.ExceptionServices;

namespace System.Net.Mime
{
    /// <summary>
    /// Summary description for MimePart.
    /// </summary>
    internal sealed class MimePart : MimeBasePart, IDisposable
    {
        private Stream? _stream;
        private bool _streamSet;
        private bool _streamUsedOnce;
        private AsyncCallback? _readCallback;
        private AsyncCallback? _writeCallback;
        private const int maxBufferSize = 0x4400;  //seems optimal for send based on perf analysis

        internal MimePart() { }

        public void Dispose()
        {
            _stream?.Close();
        }

        internal Stream? Stream => _stream;

        internal ContentDisposition? ContentDisposition
        {
            get { return _contentDisposition; }
            set
            {
                _contentDisposition = value;
                if (value == null)
                {
                    ((HeaderCollection)Headers).InternalRemove(MailHeaderInfo.GetString(MailHeaderID.ContentDisposition)!);
                }
                else
                {
                    _contentDisposition!.PersistIfNeeded((HeaderCollection)Headers, true);
                }
            }
        }

        internal TransferEncoding TransferEncoding
        {
            get
            {
                string value = Headers[MailHeaderInfo.GetString(MailHeaderID.ContentTransferEncoding)!]!;
                if (value.Equals("base64", StringComparison.OrdinalIgnoreCase))
                {
                    return TransferEncoding.Base64;
                }
                else if (value.Equals("quoted-printable", StringComparison.OrdinalIgnoreCase))
                {
                    return TransferEncoding.QuotedPrintable;
                }
                else if (value.Equals("7bit", StringComparison.OrdinalIgnoreCase))
                {
                    return TransferEncoding.SevenBit;
                }
                else if (value.Equals("8bit", StringComparison.OrdinalIgnoreCase))
                {
                    return TransferEncoding.EightBit;
                }
                else
                {
                    return TransferEncoding.Unknown;
                }
            }
            set
            {
                //QFE 4554
                if (value == TransferEncoding.Base64)
                {
                    Headers[MailHeaderInfo.GetString(MailHeaderID.ContentTransferEncoding)] = "base64";
                }
                else if (value == TransferEncoding.QuotedPrintable)
                {
                    Headers[MailHeaderInfo.GetString(MailHeaderID.ContentTransferEncoding)] = "quoted-printable";
                }
                else if (value == TransferEncoding.SevenBit)
                {
                    Headers[MailHeaderInfo.GetString(MailHeaderID.ContentTransferEncoding)] = "7bit";
                }
                else if (value == TransferEncoding.EightBit)
                {
                    Headers[MailHeaderInfo.GetString(MailHeaderID.ContentTransferEncoding)] = "8bit";
                }
                else
                {
                    throw new NotSupportedException(SR.Format(SR.MimeTransferEncodingNotSupported, value));
                }
            }
        }

        internal void SetContent(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (_streamSet)
            {
                _stream!.Close();
            }

            _stream = stream;
            _streamSet = true;
            _streamUsedOnce = false;
            TransferEncoding = TransferEncoding.Base64;
        }

        internal void SetContent(Stream stream, string? name, string? mimeType)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (mimeType != null && mimeType != string.Empty)
            {
                _contentType = new ContentType(mimeType);
            }
            if (name != null && name != string.Empty)
            {
                ContentType.Name = name;
            }
            SetContent(stream);
        }

        internal void SetContent(Stream stream, ContentType? contentType)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            _contentType = contentType;
            SetContent(stream);
        }

        internal void Complete(IAsyncResult result, Exception? e)
        {
            //if we already completed and we got called again,
            //it mean's that there was an exception in the callback and we
            //should just rethrow it.

            MimePartContext context = (MimePartContext)result.AsyncState!;
            if (context._completed)
            {
                ExceptionDispatchInfo.Throw(e!);
            }

            try
            {
                if (context._outputStream != null)
                {
                    context._outputStream.Close();
                }
            }
            catch (Exception ex)
            {
                if (e == null)
                {
                    e = ex;
                }
            }
            context._completed = true;
            context._result.InvokeCallback(e);
        }


        internal void ReadCallback(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            ((MimePartContext)result.AsyncState!)._completedSynchronously = false;

            try
            {
                ReadCallbackHandler(result);
            }
            catch (Exception e)
            {
                Complete(result, e);
            }
        }

        internal void ReadCallbackHandler(IAsyncResult result)
        {
            MimePartContext context = (MimePartContext)result.AsyncState!;
            context._bytesLeft = Stream!.EndRead(result);
            if (context._bytesLeft > 0)
            {
                IAsyncResult writeResult = context._outputStream!.BeginWrite(context._buffer, 0, context._bytesLeft, _writeCallback, context);
                if (writeResult.CompletedSynchronously)
                {
                    WriteCallbackHandler(writeResult);
                }
            }
            else
            {
                Complete(result, null);
            }
        }

        internal void WriteCallback(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            ((MimePartContext)result.AsyncState!)._completedSynchronously = false;

            try
            {
                WriteCallbackHandler(result);
            }
            catch (Exception e)
            {
                Complete(result, e);
            }
        }

        internal void WriteCallbackHandler(IAsyncResult result)
        {
            MimePartContext context = (MimePartContext)result.AsyncState!;
            context._outputStream!.EndWrite(result);
            IAsyncResult readResult = Stream!.BeginRead(context._buffer, 0, context._buffer.Length, _readCallback, context);
            if (readResult.CompletedSynchronously)
            {
                ReadCallbackHandler(readResult);
            }
        }

        internal Stream GetEncodedStream(Stream stream)
        {
            Stream outputStream = stream;

            if (TransferEncoding == TransferEncoding.Base64)
            {
                outputStream = new Base64Stream(outputStream, new Base64WriteStateInfo());
            }
            else if (TransferEncoding == TransferEncoding.QuotedPrintable)
            {
                outputStream = new QuotedPrintableStream(outputStream, true);
            }
            else if (TransferEncoding == TransferEncoding.SevenBit || TransferEncoding == TransferEncoding.EightBit)
            {
                outputStream = new EightBitStream(outputStream);
            }

            return outputStream;
        }

        internal void ContentStreamCallbackHandler(IAsyncResult result)
        {
            MimePartContext context = (MimePartContext)result.AsyncState!;
            Stream outputStream = context._writer.EndGetContentStream(result);
            context._outputStream = GetEncodedStream(outputStream);

            _readCallback = new AsyncCallback(ReadCallback);
            _writeCallback = new AsyncCallback(WriteCallback);
            IAsyncResult readResult = Stream!.BeginRead(context._buffer, 0, context._buffer.Length, _readCallback, context);
            if (readResult.CompletedSynchronously)
            {
                ReadCallbackHandler(readResult);
            }
        }

        internal void ContentStreamCallback(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            ((MimePartContext)result.AsyncState!)._completedSynchronously = false;

            try
            {
                ContentStreamCallbackHandler(result);
            }
            catch (Exception e)
            {
                Complete(result, e);
            }
        }

        internal sealed class MimePartContext
        {
            internal MimePartContext(BaseWriter writer, LazyAsyncResult result)
            {
                _writer = writer;
                _result = result;
                _buffer = new byte[maxBufferSize];
            }

            internal Stream? _outputStream;
            internal LazyAsyncResult _result;
            internal int _bytesLeft;
            internal BaseWriter _writer;
            internal byte[] _buffer;
            internal bool _completed;
            internal bool _completedSynchronously = true;
        }

        internal override IAsyncResult BeginSend(BaseWriter writer, AsyncCallback? callback, bool allowUnicode, object? state)
        {
            PrepareHeaders(allowUnicode);
            writer.WriteHeaders(Headers, allowUnicode);
            MimePartAsyncResult result = new MimePartAsyncResult(this, state, callback);
            MimePartContext context = new MimePartContext(writer, result);

            ResetStream();
            _streamUsedOnce = true;
            IAsyncResult contentResult = writer.BeginGetContentStream(new AsyncCallback(ContentStreamCallback), context);
            if (contentResult.CompletedSynchronously)
            {
                ContentStreamCallbackHandler(contentResult);
            }
            return result;
        }

        internal override void Send(BaseWriter writer, bool allowUnicode)
        {
            if (Stream != null)
            {
                byte[] buffer = new byte[maxBufferSize];

                PrepareHeaders(allowUnicode);
                writer.WriteHeaders(Headers, allowUnicode);

                Stream outputStream = writer.GetContentStream();
                outputStream = GetEncodedStream(outputStream);

                int read;

                ResetStream();
                _streamUsedOnce = true;

                while ((read = Stream.Read(buffer, 0, maxBufferSize)) > 0)
                {
                    outputStream.Write(buffer, 0, read);
                }
                outputStream.Close();
            }
        }

        //Ensures that if we've used the stream once, we will either reset it to the origin, or throw.
        internal void ResetStream()
        {
            if (_streamUsedOnce)
            {
                if (Stream!.CanSeek)
                {
                    Stream.Seek(0, SeekOrigin.Begin);
                    _streamUsedOnce = false;
                }
                else
                {
                    throw new InvalidOperationException(SR.MimePartCantResetStream);
                }
            }
        }
    }
}
