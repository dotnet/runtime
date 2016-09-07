// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.SqlClient
{
    sealed internal class SqlSequentialTextReader : System.IO.TextReader
    {
        private SqlDataReader _reader;  // The SqlDataReader that we are reading data from
        private int _columnIndex;       // The index of out column in the table
        private Encoding _encoding;     // Encoding for this character stream
        private Decoder _decoder;       // Decoder based on the encoding (NOTE: Decoders are stateful as they are designed to process streams of data)
        private byte[] _leftOverBytes;  // Bytes leftover from the last Read() operation - this can be null if there were no bytes leftover (Possible optimization: re-use the same array?)
        private int _peekedChar;        // The last character that we peeked at (or -1 if we haven't peeked at anything)
        private Task _currentTask;      // The current async task
        private CancellationTokenSource _disposalTokenSource;    // Used to indicate that a cancellation is requested due to disposal

        internal SqlSequentialTextReader(SqlDataReader reader, int columnIndex, Encoding encoding)
        {
            Debug.Assert(reader != null, "Null reader when creating sequential textreader");
            Debug.Assert(columnIndex >= 0, "Invalid column index when creating sequential textreader");
            Debug.Assert(encoding != null, "Null encoding when creating sequential textreader");

            _reader = reader;
            _columnIndex = columnIndex;
            _encoding = encoding;
            _decoder = encoding.GetDecoder();
            _leftOverBytes = null;
            _peekedChar = -1;
            _currentTask = null;
            _disposalTokenSource = new CancellationTokenSource();
        }

        internal int ColumnIndex
        {
            get { return _columnIndex; }
        }

        public override int Peek()
        {
            if (_currentTask != null)
            {
                throw ADP.AsyncOperationPending();
            }
            if (IsClosed)
            {
                throw ADP.ObjectDisposed(this);
            }

            if (!HasPeekedChar)
            {
                _peekedChar = Read();
            }

            Debug.Assert(_peekedChar == -1 || ((_peekedChar >= char.MinValue) && (_peekedChar <= char.MaxValue)), string.Format("Bad peeked character: {0}", _peekedChar));
            return _peekedChar;
        }

        public override int Read()
        {
            if (_currentTask != null)
            {
                throw ADP.AsyncOperationPending();
            }
            if (IsClosed)
            {
                throw ADP.ObjectDisposed(this);
            }

            int readChar = -1;

            // If there is already a peeked char, then return it
            if (HasPeekedChar)
            {
                readChar = _peekedChar;
                _peekedChar = -1;
            }
            // If there is data available try to read a char
            else
            {
                char[] tempBuffer = new char[1];
                int charsRead = InternalRead(tempBuffer, 0, 1);
                if (charsRead == 1)
                {
                    readChar = tempBuffer[0];
                }
            }

            Debug.Assert(readChar == -1 || ((readChar >= char.MinValue) && (readChar <= char.MaxValue)), string.Format("Bad read character: {0}", readChar));
            return readChar;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            ValidateReadParameters(buffer, index, count);

            if (IsClosed)
            {
                throw ADP.ObjectDisposed(this);
            }
            if (_currentTask != null)
            {
                throw ADP.AsyncOperationPending();
            }

            int charsRead = 0;
            int charsNeeded = count;
            // Load in peeked char
            if ((charsNeeded > 0) && (HasPeekedChar))
            {
                Debug.Assert((_peekedChar >= char.MinValue) && (_peekedChar <= char.MaxValue), string.Format("Bad peeked character: {0}", _peekedChar));
                buffer[index + charsRead] = (char)_peekedChar;
                charsRead++;
                charsNeeded--;
                _peekedChar = -1;
            }

            // If we need more data and there is data available, read
            charsRead += InternalRead(buffer, index + charsRead, charsNeeded);

            return charsRead;
        }

        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            ValidateReadParameters(buffer, index, count);
            TaskCompletionSource<int> completion = new TaskCompletionSource<int>();

            if (IsClosed)
            {
                completion.SetException(ADP.ExceptionWithStackTrace(ADP.ObjectDisposed(this)));
            }
            else
            {
                try
                {
                    Task original = Interlocked.CompareExchange<Task>(ref _currentTask, completion.Task, null);
                    if (original != null)
                    {
                        completion.SetException(ADP.ExceptionWithStackTrace(ADP.AsyncOperationPending()));
                    }
                    else
                    {
                        bool completedSynchronously = true;
                        int charsRead = 0;
                        int adjustedIndex = index;
                        int charsNeeded = count;

                        // Load in peeked char
                        if ((HasPeekedChar) && (charsNeeded > 0))
                        {
                            // Take a copy of _peekedChar in case it is cleared during close
                            int peekedChar = _peekedChar;
                            if (peekedChar >= char.MinValue)
                            {
                                Debug.Assert((_peekedChar >= char.MinValue) && (_peekedChar <= char.MaxValue), string.Format("Bad peeked character: {0}", _peekedChar));
                                buffer[adjustedIndex] = (char)peekedChar;
                                adjustedIndex++;
                                charsRead++;
                                charsNeeded--;
                                _peekedChar = -1;
                            }
                        }

                        int byteBufferUsed;
                        byte[] byteBuffer = PrepareByteBuffer(charsNeeded, out byteBufferUsed);

                        // Permit a 0 byte read in order to advance the reader to the correct column
                        if ((byteBufferUsed < byteBuffer.Length) || (byteBuffer.Length == 0))
                        {
                            int bytesRead;
                            var reader = _reader;
                            if (reader != null)
                            {
                                Task<int> getBytesTask = reader.GetBytesAsync(_columnIndex, byteBuffer, byteBufferUsed, byteBuffer.Length - byteBufferUsed, Timeout.Infinite, _disposalTokenSource.Token, out bytesRead);
                                if (getBytesTask == null)
                                {
                                    byteBufferUsed += bytesRead;
                                }
                                else
                                {
                                    // We need more data - setup the callback, and mark this as not completed sync
                                    completedSynchronously = false;
                                    getBytesTask.ContinueWith((t) =>
                                    {
                                        _currentTask = null;
                                        // If we completed but the textreader is closed, then report cancellation
                                        if ((t.Status == TaskStatus.RanToCompletion) && (!IsClosed))
                                        {
                                            try
                                            {
                                                int bytesReadFromStream = t.Result;
                                                byteBufferUsed += bytesReadFromStream;
                                                if (byteBufferUsed > 0)
                                                {
                                                    charsRead += DecodeBytesToChars(byteBuffer, byteBufferUsed, buffer, adjustedIndex, charsNeeded);
                                                }
                                                completion.SetResult(charsRead);
                                            }
                                            catch (Exception ex)
                                            {
                                                completion.SetException(ex);
                                            }
                                        }
                                        else if (IsClosed)
                                        {
                                            completion.SetException(ADP.ExceptionWithStackTrace(ADP.ObjectDisposed(this)));
                                        }
                                        else if (t.Status == TaskStatus.Faulted)
                                        {
                                            if (t.Exception.InnerException is SqlException)
                                            {
                                                // ReadAsync can't throw a SqlException, so wrap it in an IOException
                                                completion.SetException(ADP.ExceptionWithStackTrace(ADP.ErrorReadingFromStream(t.Exception.InnerException)));
                                            }
                                            else
                                            {
                                                completion.SetException(t.Exception.InnerException);
                                            }
                                        }
                                        else
                                        {
                                            completion.SetCanceled();
                                        }
                                    }, TaskScheduler.Default);
                                }


                                if ((completedSynchronously) && (byteBufferUsed > 0))
                                {
                                    // No more data needed, decode what we have
                                    charsRead += DecodeBytesToChars(byteBuffer, byteBufferUsed, buffer, adjustedIndex, charsNeeded);
                                }
                            }
                            else
                            {
                                // Reader is null, close must of happened in the middle of this read
                                completion.SetException(ADP.ExceptionWithStackTrace(ADP.ObjectDisposed(this)));
                            }
                        }


                        if (completedSynchronously)
                        {
                            _currentTask = null;
                            if (IsClosed)
                            {
                                completion.SetCanceled();
                            }
                            else
                            {
                                completion.SetResult(charsRead);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // In case of any errors, ensure that the completion is completed and the task is set back to null if we switched it
                    completion.TrySetException(ex);
                    Interlocked.CompareExchange(ref _currentTask, null, completion.Task);
                    throw;
                }
            }

            return completion.Task;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Set the textreader as closed
                SetClosed();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Forces the TextReader to act as if it was closed
        /// This does not actually close the stream, read off the rest of the data or dispose this
        /// </summary>
        internal void SetClosed()
        {
            _disposalTokenSource.Cancel();
            _reader = null;
            _peekedChar = -1;

            // Wait for pending task
            var currentTask = _currentTask;
            if (currentTask != null)
            {
                ((IAsyncResult)currentTask).AsyncWaitHandle.WaitOne();
            }
        }

        /// <summary>
        /// Performs the actual reading and converting
        /// NOTE: This assumes that buffer, index and count are all valid, we're not closed (!IsClosed) and that there is data left (IsDataLeft())
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private int InternalRead(char[] buffer, int index, int count)
        {
            Debug.Assert(buffer != null, "Null output buffer");
            Debug.Assert((index >= 0) && (count >= 0) && (index + count <= buffer.Length), string.Format("Bad count: {0} or index: {1}", count, index));
            Debug.Assert(!IsClosed, "Can't read while textreader is closed");

            try
            {
                int byteBufferUsed;
                byte[] byteBuffer = PrepareByteBuffer(count, out byteBufferUsed);
                byteBufferUsed += _reader.GetBytesInternalSequential(_columnIndex, byteBuffer, byteBufferUsed, byteBuffer.Length - byteBufferUsed);

                if (byteBufferUsed > 0)
                {
                    return DecodeBytesToChars(byteBuffer, byteBufferUsed, buffer, index, count);
                }
                else
                {
                    // Nothing to read, or nothing read
                    return 0;
                }
            }
            catch (SqlException ex)
            {
                // Read can't throw a SqlException - so wrap it in an IOException
                throw ADP.ErrorReadingFromStream(ex);
            }
        }

        /// <summary>
        /// Creates a byte array large enough to store all bytes for the characters in the current encoding, then fills it with any leftover bytes
        /// </summary>
        /// <param name="numberOfChars">Number of characters that are to be read</param>
        /// <param name="byteBufferUsed">Number of bytes pre-filled by the leftover bytes</param>
        /// <returns>A byte array of the correct size, pre-filled with leftover bytes</returns>
        private byte[] PrepareByteBuffer(int numberOfChars, out int byteBufferUsed)
        {
            Debug.Assert(numberOfChars >= 0, "Can't prepare a byte buffer for negative characters");

            byte[] byteBuffer;

            if (numberOfChars == 0)
            {
                byteBuffer = Array.Empty<byte>();
                byteBufferUsed = 0;
            }
            else
            {
                int byteBufferSize = _encoding.GetMaxByteCount(numberOfChars);

                if (_leftOverBytes != null)
                {
                    // If we have more leftover bytes than we need for this conversion, then just re-use the leftover buffer
                    if (_leftOverBytes.Length > byteBufferSize)
                    {
                        byteBuffer = _leftOverBytes;
                        byteBufferUsed = byteBuffer.Length;
                    }
                    else
                    {
                        // Otherwise, copy over the leftover buffer
                        byteBuffer = new byte[byteBufferSize];
                        Buffer.BlockCopy(_leftOverBytes, 0, byteBuffer, 0, _leftOverBytes.Length);
                        byteBufferUsed = _leftOverBytes.Length;
                    }
                }
                else
                {
                    byteBuffer = new byte[byteBufferSize];
                    byteBufferUsed = 0;
                }
            }

            return byteBuffer;
        }

        /// <summary>
        /// Decodes the given bytes into characters, and stores the leftover bytes for later use
        /// </summary>
        /// <param name="inBuffer">Buffer of bytes to decode</param>
        /// <param name="inBufferCount">Number of bytes to decode from the inBuffer</param>
        /// <param name="outBuffer">Buffer to write the characters to</param>
        /// <param name="outBufferOffset">Offset to start writing to outBuffer at</param>
        /// <param name="outBufferCount">Maximum number of characters to decode</param>
        /// <returns>The actual number of characters decoded</returns>
        private int DecodeBytesToChars(byte[] inBuffer, int inBufferCount, char[] outBuffer, int outBufferOffset, int outBufferCount)
        {
            Debug.Assert(inBuffer != null, "Null input buffer");
            Debug.Assert((inBufferCount > 0) && (inBufferCount <= inBuffer.Length), string.Format("Bad inBufferCount: {0}", inBufferCount));
            Debug.Assert(outBuffer != null, "Null output buffer");
            Debug.Assert((outBufferOffset >= 0) && (outBufferCount > 0) && (outBufferOffset + outBufferCount <= outBuffer.Length), string.Format("Bad outBufferCount: {0} or outBufferOffset: {1}", outBufferCount, outBufferOffset));

            int charsRead;
            int bytesUsed;
            bool completed;
            _decoder.Convert(inBuffer, 0, inBufferCount, outBuffer, outBufferOffset, outBufferCount, false, out bytesUsed, out charsRead, out completed);

            // completed may be false and there is no spare bytes if the Decoder has stored bytes to use later
            if ((!completed) && (bytesUsed < inBufferCount))
            {
                _leftOverBytes = new byte[inBufferCount - bytesUsed];
                Buffer.BlockCopy(inBuffer, bytesUsed, _leftOverBytes, 0, _leftOverBytes.Length);
            }
            else
            {
                // If Convert() sets completed to true, then it must have used all of the bytes we gave it
                Debug.Assert(bytesUsed >= inBufferCount, "Converted completed, but not all bytes were used");
                _leftOverBytes = null;
            }

            Debug.Assert(((_reader == null) || (_reader.ColumnDataBytesRemaining() > 0) || (!completed) || (_leftOverBytes == null)), "Stream has run out of data and the decoder finished, but there are leftover bytes");
            Debug.Assert(charsRead > 0, "Converted no chars. Bad encoding?");

            return charsRead;
        }

        /// <summary>
        /// True if this TextReader is supposed to be closed
        /// </summary>
        private bool IsClosed
        {
            get { return (_reader == null); }
        }

        /// <summary>
        /// True if there is a peeked character available
        /// </summary>
        private bool HasPeekedChar
        {
            get { return (_peekedChar >= char.MinValue); }
        }

        /// <summary>
        /// Checks the parameters passed into a Read() method are valid
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        internal static void ValidateReadParameters(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw ADP.ArgumentNull(nameof(buffer));
            }
            if (index < 0)
            {
                throw ADP.ArgumentOutOfRange(nameof(index));
            }
            if (count < 0)
            {
                throw ADP.ArgumentOutOfRange(nameof(count));
            }
            try
            {
                if (checked(index + count) > buffer.Length)
                {
                    throw ExceptionBuilder.InvalidOffsetLength();
                }
            }
            catch (OverflowException)
            {
                // If we've overflowed when adding index and count, then they never would have fit into buffer anyway
                throw ExceptionBuilder.InvalidOffsetLength();
            }
        }
    }

    sealed internal class SqlUnicodeEncoding : UnicodeEncoding
    {
        private static SqlUnicodeEncoding s_singletonEncoding = new SqlUnicodeEncoding();

        private SqlUnicodeEncoding() : base(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: false)
        { }
        
        public override Decoder GetDecoder()
        {
            return new SqlUnicodeDecoder();
        }

        public override int GetMaxByteCount(int charCount)
        {
            // SQL Server never sends a BOM, so we can assume that its 2 bytes per char
            return charCount * 2;
        }

        public static Encoding SqlUnicodeEncodingInstance
        {
            get { return s_singletonEncoding; }
        }

        sealed private class SqlUnicodeDecoder : Decoder
        {
            public override int GetCharCount(byte[] bytes, int index, int count)
            {
                // SQL Server never sends a BOM, so we can assume that its 2 bytes per char
                return count / 2;
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            {
                // This method is required - simply call Convert()
                int bytesUsed;
                int charsUsed;
                bool completed;
                Convert(bytes, byteIndex, byteCount, chars, charIndex, chars.Length - charIndex, true, out bytesUsed, out charsUsed, out completed);
                return charsUsed;
            }

            public override void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
            {
                // Assume 2 bytes per char and no BOM
                charsUsed = Math.Min(charCount, byteCount / 2);
                bytesUsed = charsUsed * 2;
                completed = (bytesUsed == byteCount);

                // BlockCopy uses offsets\length measured in bytes, not the actual array index
                Buffer.BlockCopy(bytes, byteIndex, chars, charIndex * 2, bytesUsed);
            }
        }
    }
}
