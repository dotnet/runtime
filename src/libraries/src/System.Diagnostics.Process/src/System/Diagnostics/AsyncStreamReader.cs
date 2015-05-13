// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** Class:  AsyncStreamReader
**
** Purpose: For reading text from streams using a particular 
** encoding in an asynchronous manner used by the process class
**
**
===========================================================*/

using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics
{
    internal sealed class AsyncStreamReader
    {
        private const int DefaultBufferSize = 1024;  // Byte buffer size

        private readonly Stream _stream;
        private readonly Decoder _decoder;
        private readonly byte[] _byteBuffer;
        private readonly char[] _charBuffer;

        // Delegate to call user function.
        private readonly Action<string> _userCallBack;

        private readonly CancellationTokenSource _cts;
        private Task _readToBufferTask;
        private readonly Queue<string> _messageQueue;
        private StringBuilder _sb;
        private bool _bLastCarriageReturn;

        // Cache the last position scanned in sb when searching for lines.
        private int _currentLinePos;

        // Creates a new AsyncStreamReader for the given stream. The
        // character encoding is set by encoding and the buffer size,
        // in number of 16-bit characters, is set by bufferSize.
        internal AsyncStreamReader(Stream stream, Action<string> callback, Encoding encoding)
        {
            Debug.Assert(stream != null && encoding != null && callback != null, "Invalid arguments!");
            Debug.Assert(stream.CanRead, "Stream must be readable!");

            _stream = stream;
            _userCallBack = callback;
            _decoder = encoding.GetDecoder();
            _byteBuffer = new byte[DefaultBufferSize];

            // This is the maximum number of chars we can get from one iteration in loop inside ReadBuffer.
            // Used so ReadBuffer can tell when to copy data into a user's char[] directly, instead of our internal char[].
            int maxCharsPerBuffer = encoding.GetMaxCharCount(DefaultBufferSize);
            _charBuffer = new char[maxCharsPerBuffer];

            _cts = new CancellationTokenSource();
            _messageQueue = new Queue<string>();
        }

        // User calls BeginRead to start the asynchronous read
        internal void BeginReadLine()
        {
            if (_sb == null)
            {
                _sb = new StringBuilder(DefaultBufferSize);
                _readToBufferTask = Task.Factory.StartNew(s => ((AsyncStreamReader)s).ReadBuffer(),
                    this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
            else
            {
                FlushMessageQueue(rethrowInNewThread: false);
            }
        }

        internal void CancelOperation()
        {
            _cts.Cancel();
        }

        // This is the async callback function. Only one thread could/should call this.
        private async Task ReadBuffer()
        {
            while (true)
            {
                try
                {
                    int bytesRead = await _stream.ReadAsync(_byteBuffer, 0, _byteBuffer.Length, _cts.Token).ConfigureAwait(false);
                    if (bytesRead == 0)
                        break;

                    int charLen = _decoder.GetChars(_byteBuffer, 0, bytesRead, _charBuffer, 0);
                    _sb.Append(_charBuffer, 0, charLen);
                    MoveLinesFromStringBuilderToMessageQueue();
                }
                catch (IOException)
                {
                    // We should ideally consume errors from operations getting cancelled
                    // so that we don't crash the unsuspecting parent with an unhandled exc.
                    // This seems to come in 2 forms of exceptions (depending on platform and scenario),
                    // namely OperationCanceledException and IOException (for errorcode that we don't
                    // map explicitly).
                    break; // Treat this as EOF
                }
                catch (OperationCanceledException)
                {
                    // We should consume any OperationCanceledException from child read here
                    // so that we don't crash the parent with an unhandled exc
                    break; // Treat this as EOF
                }

                // If user's delegate throws exception we treat this as EOF and
                // completing without processing current buffer content
                if (FlushMessageQueue(rethrowInNewThread: true))
                {
                    return;
                }
            }

            // We're at EOF, process current buffer content and flush message queue.
            lock (_messageQueue)
            {
                if (_sb.Length != 0)
                {
                    _messageQueue.Enqueue(_sb.ToString());
                    _sb.Length = 0;
                }
                _messageQueue.Enqueue(null);
            }

            FlushMessageQueue(rethrowInNewThread: true);
        }

        // Read lines stored in StringBuilder and the buffer we just read into.
        // A line is defined as a sequence of characters followed by
        // a carriage return ('\r'), a line feed ('\n'), or a carriage return
        // immediately followed by a line feed. The resulting string does not
        // contain the terminating carriage return and/or line feed. The returned
        // value is null if the end of the input stream has been reached.
        private void MoveLinesFromStringBuilderToMessageQueue()
        {
            int currentIndex = _currentLinePos;
            int lineStart = 0;
            int len = _sb.Length;

            // skip a beginning '\n' character of new block if last block ended 
            // with '\r'
            if (_bLastCarriageReturn && (len > 0) && _sb[0] == '\n')
            {
                currentIndex = 1;
                lineStart = 1;
                _bLastCarriageReturn = false;
            }

            while (currentIndex < len)
            {
                char ch = _sb[currentIndex];
                // Note the following common line feed chars:
                // \n - UNIX   \r\n - DOS   \r - Mac
                if (ch == '\r' || ch == '\n')
                {
                    string line = _sb.ToString(lineStart, currentIndex - lineStart);
                    lineStart = currentIndex + 1;
                    // skip the "\n" character following "\r" character
                    if ((ch == '\r') && (lineStart < len) && (_sb[lineStart] == '\n'))
                    {
                        lineStart++;
                        currentIndex++;
                    }

                    lock (_messageQueue)
                    {
                        _messageQueue.Enqueue(line);
                    }
                }
                currentIndex++;
            }
            if (_sb[len - 1] == '\r')
            {
                _bLastCarriageReturn = true;
            }
            // Keep the rest characters which can't form a new line in string builder.
            if (lineStart < len)
            {
                if (lineStart == 0)
                {
                    // we found no breaklines, in this case we cache the position
                    // so next time we don't have to restart from the beginning
                    _currentLinePos = currentIndex;
                }
                else
                {
                    _sb.Remove(0, lineStart);
                    _currentLinePos = 0;
                }
            }
            else
            {
                _sb.Length = 0;
                _currentLinePos = 0;
            }
        }

        private bool FlushMessageQueue(bool rethrowInNewThread)
        {
            while (true)
            {
                // When we call BeginReadLine, we also need to flush the queue
                // So there could be a race between the ReadBuffer and BeginReadLine
                // We need to take lock before DeQueue.
                if (_messageQueue.Count > 0)
                {
                    string line = null;
                    // We need this flag to ensure that line was taken from the queue.
                    // We couldn't just check line on null because ReadBuffer enqueue null in case of EOF.
                    bool isDequeue = false;
                    lock (_messageQueue)
                    {
                        if (_messageQueue.Count > 0)
                        {
                            line = _messageQueue.Dequeue();
                            isDequeue = true;
                        }
                    }

                    // skip if the read is cancelled
                    // this might happen inside UserCallBack
                    // However, continue to drain the queue
                    if (!_cts.Token.IsCancellationRequested && isDequeue)
                    {
                        return CallUserCallback(line, rethrowInNewThread);
                    }
                }
                else
                {
                    break;
                }
            }

            return false;
        }

        // If user's callback throw we re-throw.
        private bool CallUserCallback(string argument, bool rethrowInNewThread)
        {
            try
            {
                _userCallBack(argument);
            }
            catch (Exception e)
            {
                if (rethrowInNewThread)
                {
                    ThreadPool.QueueUserWorkItem(edi => ((ExceptionDispatchInfo)edi).Throw(), ExceptionDispatchInfo.Capture(e));
                    return true;
                }
                else
                {
                    throw;
                }
            }

            return false;
        }

        // Wait until we hit EOF. This is called from Process.WaitForExit
        // We will lose some information if we don't do this.
        internal void WaitUtilEOF()
        {
            if (_readToBufferTask != null)
            {
                _readToBufferTask.GetAwaiter().GetResult();
                _readToBufferTask = null;
            }
        }
    }
}
