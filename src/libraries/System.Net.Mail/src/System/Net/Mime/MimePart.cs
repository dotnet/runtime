// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Net.Mail;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            ArgumentNullException.ThrowIfNull(stream);

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
            ArgumentNullException.ThrowIfNull(stream);

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
            ArgumentNullException.ThrowIfNull(stream);

            _contentType = contentType;
            SetContent(stream);
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

        internal override async Task SendAsync<TIOAdapter>(BaseWriter writer, bool allowUnicode, CancellationToken cancellationToken = default)
        {
            if (Stream != null)
            {
                byte[] buffer = new byte[maxBufferSize];

                PrepareHeaders(allowUnicode);
                writer.WriteHeaders(Headers, allowUnicode);

                Stream outputStream = writer.GetContentStream();
                outputStream = GetEncodedStream(outputStream);

                ResetStream();
                _streamUsedOnce = true;

                int read;
                while ((read = await TIOAdapter.ReadAsync(Stream, buffer.AsMemory(0, maxBufferSize), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await TIOAdapter.WriteAsync(outputStream, buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
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
