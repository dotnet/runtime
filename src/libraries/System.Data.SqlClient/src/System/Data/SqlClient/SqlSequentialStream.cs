// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.SqlClient
{
    sealed internal class SqlSequentialStream : System.IO.Stream
    {
        private SqlDataReader _reader;  // The SqlDataReader that we are reading data from
        private int _columnIndex;       // The index of out column in the table
        private Task _currentTask;      // Holds the current task being processed
        private int _readTimeout;       // Read timeout for this stream in ms (for Stream.ReadTimeout)
        private CancellationTokenSource _disposalTokenSource;    // Used to indicate that a cancellation is requested due to disposal

        internal SqlSequentialStream(SqlDataReader reader, int columnIndex)
        {
            Debug.Assert(reader != null, "Null reader when creating sequential stream");
            Debug.Assert(columnIndex >= 0, "Invalid column index when creating sequential stream");

            _reader = reader;
            _columnIndex = columnIndex;
            _currentTask = null;
            _disposalTokenSource = new CancellationTokenSource();

            // Safely convert the CommandTimeout from seconds to milliseconds
            if ((reader.Command != null) && (reader.Command.CommandTimeout != 0))
            {
                _readTimeout = (int)Math.Min((long)reader.Command.CommandTimeout * 1000L, (long)int.MaxValue);
            }
            else
            {
                _readTimeout = Timeout.Infinite;
            }
        }

        public override bool CanRead
        {
            get { return ((_reader != null) && (!_reader.IsClosed)); }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanTimeout
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        { }

        public override long Length
        {
            get { throw ADP.NotSupported(); }
        }

        public override long Position
        {
            get { throw ADP.NotSupported(); }
            set { throw ADP.NotSupported(); }
        }

        public override int ReadTimeout
        {
            get { return _readTimeout; }
            set
            {
                if ((value > 0) || (value == Timeout.Infinite))
                {
                    _readTimeout = value;
                }
                else
                {
                    throw ADP.ArgumentOutOfRange(nameof(value));
                }
            }
        }

        internal int ColumnIndex
        {
            get { return _columnIndex; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateReadParameters(buffer, offset, count);
            if (!CanRead)
            {
                throw ADP.ObjectDisposed(this);
            }
            if (_currentTask != null)
            {
                throw ADP.AsyncOperationPending();
            }

            try
            {
                return _reader.GetBytesInternalSequential(_columnIndex, buffer, offset, count, _readTimeout);
            }
            catch (SqlException ex)
            {
                // Stream.Read() can't throw a SqlException - so wrap it in an IOException
                throw ADP.ErrorReadingFromStream(ex);
            }
        }


        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateReadParameters(buffer, offset, count);

            TaskCompletionSource<int> completion = new TaskCompletionSource<int>();
            if (!CanRead)
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
                        // Set up a combined cancellation token for both the user's and our disposal tokens
                        CancellationTokenSource combinedTokenSource;
                        if (!cancellationToken.CanBeCanceled)
                        {
                            // Users token is not cancellable - just use ours
                            combinedTokenSource = _disposalTokenSource;
                        }
                        else
                        {
                            // Setup registrations from user and disposal token to cancel the combined token
                            combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposalTokenSource.Token);
                        }

                        int bytesRead = 0;
                        Task<int> getBytesTask = null;
                        var reader = _reader;
                        if ((reader != null) && (!cancellationToken.IsCancellationRequested) && (!_disposalTokenSource.Token.IsCancellationRequested))
                        {
                            getBytesTask = reader.GetBytesAsync(_columnIndex, buffer, offset, count, _readTimeout, combinedTokenSource.Token, out bytesRead);
                        }

                        if (getBytesTask == null)
                        {
                            _currentTask = null;
                            if (cancellationToken.IsCancellationRequested)
                            {
                                completion.SetCanceled();
                            }
                            else if (!CanRead)
                            {
                                completion.SetException(ADP.ExceptionWithStackTrace(ADP.ObjectDisposed(this)));
                            }
                            else
                            {
                                completion.SetResult(bytesRead);
                            }

                            if (combinedTokenSource != _disposalTokenSource)
                            {
                                combinedTokenSource.Dispose();
                            }
                        }
                        else
                        {
                            getBytesTask.ContinueWith((t) =>
                            {
                                _currentTask = null;
                                // If we completed, but _reader is null (i.e. the stream is closed), then report cancellation
                                if ((t.Status == TaskStatus.RanToCompletion) && (CanRead))
                                {
                                    completion.SetResult((int)t.Result);
                                }
                                else if (t.Status == TaskStatus.Faulted)
                                {
                                    if (t.Exception.InnerException is SqlException)
                                    {
                                        // Stream.ReadAsync() can't throw a SqlException - so wrap it in an IOException
                                        completion.SetException(ADP.ExceptionWithStackTrace(ADP.ErrorReadingFromStream(t.Exception.InnerException)));
                                    }
                                    else
                                    {
                                        completion.SetException(t.Exception.InnerException);
                                    }
                                }
                                else if (!CanRead)
                                {
                                    completion.SetException(ADP.ExceptionWithStackTrace(ADP.ObjectDisposed(this)));
                                }
                                else
                                {
                                    completion.SetCanceled();
                                }

                                if (combinedTokenSource != _disposalTokenSource)
                                {
                                    combinedTokenSource.Dispose();
                                }
                            }, TaskScheduler.Default);
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

        public override IAsyncResult BeginRead(byte[] array, int offset, int count, AsyncCallback asyncCallback, object asyncState) =>
            TaskToApm.Begin(ReadAsync(array, offset, count, CancellationToken.None), asyncCallback, asyncState);

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToApm.End<int>(asyncResult);

        public override long Seek(long offset, IO.SeekOrigin origin)
        {
            throw ADP.NotSupported();
        }

        public override void SetLength(long value)
        {
            throw ADP.NotSupported();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw ADP.NotSupported();
        }

        /// <summary>
        /// Forces the stream to act as if it was closed (i.e. CanRead=false and Read() throws)
        /// This does not actually close the stream, read off the rest of the data or dispose this
        /// </summary>
        internal void SetClosed()
        {
            _disposalTokenSource.Cancel();
            _reader = null;

            // Wait for pending task
            var currentTask = _currentTask;
            if (currentTask != null)
            {
                ((IAsyncResult)currentTask).AsyncWaitHandle.WaitOne();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Set the stream as closed
                SetClosed();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Checks the parameters passed into a Read() method are valid
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        internal static void ValidateReadParameters(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw ADP.ArgumentNull(nameof(buffer));
            }
            if (offset < 0)
            {
                throw ADP.ArgumentOutOfRange(nameof(offset));
            }
            if (count < 0)
            {
                throw ADP.ArgumentOutOfRange(nameof(count));
            }
            try
            {
                if (checked(offset + count) > buffer.Length)
                {
                    throw ExceptionBuilder.InvalidOffsetLength();
                }
            }
            catch (OverflowException)
            {
                // If we've overflowed when adding offset and count, then they never would have fit into buffer anyway
                throw ExceptionBuilder.InvalidOffsetLength();
            }
        }
    }
}
