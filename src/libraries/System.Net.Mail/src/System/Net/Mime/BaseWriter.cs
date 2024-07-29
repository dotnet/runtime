// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.IO;
using System.Net.Mail;
using System.Runtime.ExceptionServices;

namespace System.Net.Mime
{
    internal abstract class BaseWriter
    {
        // This is the maximum default line length that can actually be written.  When encoding
        // headers, the line length is more conservative to account for things like folding.
        // In MailWriter, all encoding has already been done so this will only fold lines
        // that are NOT encoded already, which means being less conservative is ok.
        private const int DefaultLineLength = 76;
        private static readonly AsyncCallback s_onWrite = OnWrite;

        protected readonly BufferBuilder _bufferBuilder;
        protected readonly Stream _stream;
        private readonly EventHandler _onCloseHandler;
        private readonly bool _shouldEncodeLeadingDots;
        private readonly int _lineLength;
        protected Stream _contentStream = null!; // set to null on dispose
        protected bool _isInContent;

        protected BaseWriter(Stream stream, bool shouldEncodeLeadingDots)
        {
            ArgumentNullException.ThrowIfNull(stream);

            _stream = stream;
            _shouldEncodeLeadingDots = shouldEncodeLeadingDots;
            _onCloseHandler = new EventHandler(OnClose);
            _bufferBuilder = new BufferBuilder();
            _lineLength = DefaultLineLength;
        }

        #region Headers

        internal abstract void WriteHeaders(NameValueCollection headers, bool allowUnicode);

        internal void WriteHeader(string name, string value, bool allowUnicode)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(value);

            if (_isInContent)
            {
                throw new InvalidOperationException(SR.MailWriterIsInContent);
            }

            CheckBoundary();
            _bufferBuilder.Append(name);
            _bufferBuilder.Append(": ");
            WriteAndFold(value, name.Length + 2, allowUnicode);
            _bufferBuilder.Append("\r\n"u8);
        }

        private void WriteAndFold(string value, int charsAlreadyOnLine, bool allowUnicode)
        {
            int lastSpace = 0, startOfLine = 0;
            for (int index = 0; index < value.Length; index++)
            {
                // When we find a FWS (CRLF) copy it as is.
                if (MailBnfHelper.IsFWSAt(value, index)) // At the first char of "\r\n " or "\r\n\t"
                {
                    index += 2; // Skip the FWS
                    _bufferBuilder.Append(value, startOfLine, index - startOfLine, allowUnicode);
                    // Reset for the next line
                    startOfLine = index;
                    lastSpace = index;
                    charsAlreadyOnLine = 0;
                }
                // When we pass the line length limit, and know where there was a space to fold at, fold there
                else if (((index - startOfLine) > (_lineLength - charsAlreadyOnLine)) && lastSpace != startOfLine)
                {
                    _bufferBuilder.Append(value, startOfLine, lastSpace - startOfLine, allowUnicode);
                    _bufferBuilder.Append("\r\n"u8);
                    startOfLine = lastSpace;
                    charsAlreadyOnLine = 0;
                }
                // Mark a foldable space.  If we go over the line length limit, fold here.
                else if (value[index] == MailBnfHelper.Space || value[index] == MailBnfHelper.Tab)
                {
                    lastSpace = index;
                }
            }
            // Write any remaining data to the buffer.
            if (value.Length - startOfLine > 0)
            {
                _bufferBuilder.Append(value, startOfLine, value.Length - startOfLine, allowUnicode);
            }
        }

        #endregion Headers

        #region Content

        internal Stream GetContentStream() => GetContentStream(null);

        private ClosableStream GetContentStream(MultiAsyncResult? multiResult)
        {
            if (_isInContent)
            {
                throw new InvalidOperationException(SR.MailWriterIsInContent);
            }

            _isInContent = true;

            CheckBoundary();

            _bufferBuilder.Append("\r\n"u8);
            Flush(multiResult);

            ClosableStream cs = new ClosableStream(new EightBitStream(_stream, _shouldEncodeLeadingDots), _onCloseHandler);
            _contentStream = cs;
            return cs;
        }

        internal IAsyncResult BeginGetContentStream(AsyncCallback? callback, object? state)
        {
            MultiAsyncResult multiResult = new MultiAsyncResult(this, callback, state);

            Stream s = GetContentStream(multiResult);

            if (!(multiResult.Result is Exception))
            {
                multiResult.Result = s;
            }

            multiResult.CompleteSequence();

            return multiResult;
        }

        internal static Stream EndGetContentStream(IAsyncResult result)
        {
            object o = MultiAsyncResult.End(result)!;
            if (o is Exception e)
            {
                ExceptionDispatchInfo.Throw(e);
            }
            return (Stream)o;
        }

        #endregion Content

        #region Cleanup

        protected void Flush(MultiAsyncResult? multiResult)
        {
            if (_bufferBuilder.Length > 0)
            {
                if (multiResult != null)
                {
                    multiResult.Enter();
                    IAsyncResult result = _stream.BeginWrite(_bufferBuilder.GetBuffer(), 0,
                        _bufferBuilder.Length, s_onWrite, multiResult);
                    if (result.CompletedSynchronously)
                    {
                        _stream.EndWrite(result);
                        multiResult.Leave();
                    }
                }
                else
                {
                    _stream.Write(_bufferBuilder.GetBuffer(), 0, _bufferBuilder.Length);
                }
                _bufferBuilder.Reset();
            }
        }

        protected static void OnWrite(IAsyncResult result)
        {
            if (!result.CompletedSynchronously)
            {
                MultiAsyncResult multiResult = (MultiAsyncResult)result.AsyncState!;
                BaseWriter thisPtr = (BaseWriter)multiResult.Context;
                try
                {
                    thisPtr._stream.EndWrite(result);
                    multiResult.Leave();
                }
                catch (Exception e)
                {
                    multiResult.Leave(e);
                }
            }
        }

        internal abstract void Close();

        protected abstract void OnClose(object? sender, EventArgs args);

        #endregion Cleanup

        protected virtual void CheckBoundary() { }
    }
}
