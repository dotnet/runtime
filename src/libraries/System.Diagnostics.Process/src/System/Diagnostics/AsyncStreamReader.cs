// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** Class:  AsyncStreamReader
**
** Purpose: For reading text from streams using a particular 
** encoding in an asychronous manner used by the process class
**
**
===========================================================*/

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics
{
    internal sealed class AsyncStreamReader : IDisposable
    {
        internal const int DefaultBufferSize = 1024;  // Byte buffer size
        private const int MinBufferSize = 128;

        private Stream _stream;
        private Encoding _encoding;
        private Decoder _decoder;
        private byte[] _byteBuffer;
        private char[] _charBuffer;
        // Record the number of valid bytes in the byteBuffer, for a few checks.

        // This is the maximum number of chars we can get from one call to 
        // ReadBuffer.  Used so ReadBuffer can tell when to copy data into
        // a user's char[] directly, instead of our internal char[].
        private int _maxCharsPerBuffer;

        // Store a backpointer to the process class, to check for user callbacks
        private readonly Process _process;

        // Delegate to call user function.
        private Action<string> _userCallBack;

        // Internal Cancel operation
        private bool _cancelOperation;
        private ManualResetEvent _eofEvent;
        private Queue<string> _messageQueue;
        private StringBuilder _sb;
        private bool _bLastCarriageReturn;

        // Cache the last position scanned in sb when searching for lines.
        private int _currentLinePos;

        internal AsyncStreamReader(Process process, Stream stream, Action<string> callback, Encoding encoding)
            : this(process, stream, callback, encoding, DefaultBufferSize)
        {
        }


        // Creates a new AsyncStreamReader for the given stream.  The 
        // character encoding is set by encoding and the buffer size, 
        // in number of 16-bit characters, is set by bufferSize.  
        // 
        internal AsyncStreamReader(Process process, Stream stream, Action<string> callback, Encoding encoding, int bufferSize)
        {
            Debug.Assert(process != null && stream != null && encoding != null && callback != null, "Invalid arguments!");
            Debug.Assert(stream.CanRead, "Stream must be readable!");
            Debug.Assert(bufferSize > 0, "Invalid buffer size!");

            _process = process;
            _stream = stream;
            _userCallBack = callback;
            _encoding = encoding;
            _decoder = encoding.GetDecoder();

            if (bufferSize < MinBufferSize) bufferSize = MinBufferSize;
            _byteBuffer = new byte[bufferSize];
            _maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
            _charBuffer = new char[_maxCharsPerBuffer];

            _eofEvent = new ManualResetEvent(false);
            _messageQueue = new Queue<string>();
        }

        public void Close()
        {
            Dispose(true);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_stream != null)
                    _stream.Dispose();
            }
            if (_stream != null)
            {
                _stream = null;
                _encoding = null;
                _decoder = null;
                _byteBuffer = null;
                _charBuffer = null;
            }

            if (_eofEvent != null)
            {
                _eofEvent.Dispose();
                _eofEvent = null;
            }
        }

        public Encoding CurrentEncoding
        {
            get { return _encoding; }
        }

        public Stream BaseStream
        {
            get { return _stream; }
        }

        // User calls BeginRead to start the asynchronous read
        internal void BeginReadLine()
        {
            if (_cancelOperation)
            {
                _cancelOperation = false;
            }

            if (_sb == null)
            {
                _sb = new StringBuilder(DefaultBufferSize);
                _stream.ReadAsync(_byteBuffer, 0, _byteBuffer.Length).ContinueWith(ReadBuffer, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            }
            else
            {
                FlushMessageQueue();
            }
        }

        internal void CancelOperation()
        {
            _cancelOperation = true;
        }

        // This is the async callback function. Only one thread could/should call this.
        private void ReadBuffer(Task<int> t)
        {
            int byteLen;

            try
            {
                byteLen = t.GetAwaiter().GetResult();
            }
            catch (IOException)
            {
                // We should ideally consume errors from operations getting cancelled
                // so that we don't crash the unsuspecting parent with an unhandled exc. 
                // This seems to come in 2 forms of exceptions (depending on platform and scenario), 
                // namely OperationCanceledException and IOException (for errorcode that we don't 
                // map explicitly).   
                byteLen = 0; // Treat this as EOF
            }
            catch (OperationCanceledException)
            {
                // We should consume any OperationCanceledException from child read here  
                // so that we don't crash the parent with an unhandled exc
                byteLen = 0; // Treat this as EOF
            }

            if (byteLen == 0)
            {
                // We're at EOF, we won't call this function again from here on.
                lock (_messageQueue)
                {
                    if (_sb.Length != 0)
                    {
                        _messageQueue.Enqueue(_sb.ToString());
                        _sb.Length = 0;
                    }
                    _messageQueue.Enqueue(null);
                }

                try
                {
                    // UserCallback could throw, we should still set the eofEvent 
                    FlushMessageQueue();
                }
                finally
                {
                    _eofEvent.Set();
                }
            }
            else
            {
                int charLen = _decoder.GetChars(_byteBuffer, 0, byteLen, _charBuffer, 0);
                _sb.Append(_charBuffer, 0, charLen);
                GetLinesFromStringBuilder();
                _stream.ReadAsync(_byteBuffer, 0, _byteBuffer.Length).ContinueWith(ReadBuffer, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            }
        }


        // Read lines stored in StringBuilder and the buffer we just read into. 
        // A line is defined as a sequence of characters followed by
        // a carriage return ('\r'), a line feed ('\n'), or a carriage return
        // immediately followed by a line feed. The resulting string does not
        // contain the terminating carriage return and/or line feed. The returned
        // value is null if the end of the input stream has been reached.
        //

        private void GetLinesFromStringBuilder()
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
                    string s = _sb.ToString(lineStart, currentIndex - lineStart);
                    lineStart = currentIndex + 1;
                    // skip the "\n" character following "\r" character
                    if ((ch == '\r') && (lineStart < len) && (_sb[lineStart] == '\n'))
                    {
                        lineStart++;
                        currentIndex++;
                    }

                    lock (_messageQueue)
                    {
                        _messageQueue.Enqueue(s);
                    }
                }
                currentIndex++;
            }
            if (_sb[len - 1] == '\r')
            {
                _bLastCarriageReturn = true;
            }
            // Keep the rest characaters which can't form a new line in string builder.
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

            FlushMessageQueue();
        }

        private void FlushMessageQueue()
        {
            while (true)
            {
                // When we call BeginReadLine, we also need to flush the queue
                // So there could be a race between the ReadBuffer and BeginReadLine
                // We need to take lock before DeQueue.
                if (_messageQueue.Count > 0)
                {
                    lock (_messageQueue)
                    {
                        if (_messageQueue.Count > 0)
                        {
                            string s = (string)_messageQueue.Dequeue();
                            // skip if the read is the read is cancelled
                            // this might happen inside UserCallBack
                            // However, continue to drain the queue
                            if (!_cancelOperation)
                            {
                                _userCallBack(s);
                            }
                        }
                    }
                }
                else
                {
                    break;
                }
            }
        }

        // Wait until we hit EOF. This is called from Process.WaitForExit
        // We will lose some information if we don't do this.
        internal void WaitUtilEOF()
        {
            if (_eofEvent != null)
            {
                _eofEvent.WaitOne();
                _eofEvent.Dispose();
                _eofEvent = null;
            }
        }
    }
}
