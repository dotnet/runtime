// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Globalization;

namespace System.Net
{
    internal class StreamFramer
    {
        private Stream _transport;

        private bool _eof;

        private FrameHeader _writeHeader = new FrameHeader();
        private FrameHeader _curReadHeader = new FrameHeader();
        private FrameHeader _readVerifier = new FrameHeader(
                                                    FrameHeader.IgnoreValue,
                                                    FrameHeader.IgnoreValue,
                                                    FrameHeader.IgnoreValue);

        private byte[] _readHeaderBuffer;
        private byte[] _writeHeaderBuffer;

        private readonly AsyncCallback _readFrameCallback;
        private readonly AsyncCallback _beginWriteCallback;

        public StreamFramer(Stream Transport)
        {
            if (Transport == null || Transport == Stream.Null)
            {
                throw new ArgumentNullException(nameof(Transport));
            }

            _transport = Transport;
            _readHeaderBuffer = new byte[_curReadHeader.Size];
            _writeHeaderBuffer = new byte[_writeHeader.Size];

            _readFrameCallback = new AsyncCallback(ReadFrameCallback);
            _beginWriteCallback = new AsyncCallback(BeginWriteCallback);
        }

        public FrameHeader ReadHeader
        {
            get
            {
                return _curReadHeader;
            }
        }

        public FrameHeader WriteHeader
        {
            get
            {
                return _writeHeader;
            }
        }

        public Stream Transport
        {
            get
            {
                return _transport;
            }
        }

        public byte[] ReadMessage()
        {
            if (_eof)
            {
                return null;
            }

            int offset = 0;
            byte[] buffer = _readHeaderBuffer;

            int bytesRead;
            while (offset < buffer.Length)
            {
                bytesRead = Transport.Read(buffer, offset, buffer.Length - offset);
                if (bytesRead == 0)
                {
                    if (offset == 0)
                    {
                        // m_Eof, return null
                        _eof = true;
                        return null;
                    }
                    else
                    {
                        throw new IOException(SR.Format(SR.net_io_readfailure, SR.Format(SR.net_io_connectionclosed)));
                    }
                }

                offset += bytesRead;
            }

            _curReadHeader.CopyFrom(buffer, 0, _readVerifier);
            if (_curReadHeader.PayloadSize > _curReadHeader.MaxMessageSize)
            {
                throw new InvalidOperationException(SR.Format(SR.net_frame_size,
                                                               _curReadHeader.MaxMessageSize.ToString(NumberFormatInfo.InvariantInfo),
                                                               _curReadHeader.PayloadSize.ToString(NumberFormatInfo.InvariantInfo)));
            }

            buffer = new byte[_curReadHeader.PayloadSize];

            offset = 0;
            while (offset < buffer.Length)
            {
                bytesRead = Transport.Read(buffer, offset, buffer.Length - offset);
                if (bytesRead == 0)
                {
                    throw new IOException(SR.Format(SR.net_io_readfailure, SR.Format(SR.net_io_connectionclosed)));
                }

                offset += bytesRead;
            }
            return buffer;
        }

        public IAsyncResult BeginReadMessage(AsyncCallback asyncCallback, object stateObject)
        {
            WorkerAsyncResult workerResult;

            if (_eof)
            {
                workerResult = new WorkerAsyncResult(this, stateObject, asyncCallback, null, 0, 0);
                workerResult.InvokeCallback(-1);
                return workerResult;
            }

            workerResult = new WorkerAsyncResult(this, stateObject, asyncCallback,
                                                                   _readHeaderBuffer, 0,
                                                                   _readHeaderBuffer.Length);

            IAsyncResult result = _transport.BeginRead(_readHeaderBuffer, 0, _readHeaderBuffer.Length,
                _readFrameCallback, workerResult);

            if (result.CompletedSynchronously)
            {
                ReadFrameComplete(result);
            }

            return workerResult;
        }

        private void ReadFrameCallback(IAsyncResult transportResult)
        {
            if (!(transportResult.AsyncState is WorkerAsyncResult))
            {
                NetEventSource.Fail(this, $"The state expected to be WorkerAsyncResult, received {transportResult}.");
            }

            if (transportResult.CompletedSynchronously)
            {
                return;
            }

            WorkerAsyncResult workerResult = (WorkerAsyncResult)transportResult.AsyncState;

            try
            {
                ReadFrameComplete(transportResult);
            }
            catch (Exception e)
            {
                if (e is OutOfMemoryException)
                {
                    throw;
                }

                if (!(e is IOException))
                {
                    e = new System.IO.IOException(SR.Format(SR.net_io_readfailure, e.Message), e);
                }

                workerResult.InvokeCallback(e);
            }
        }

        // IO COMPLETION CALLBACK
        //
        // This callback is responsible for getting the complete protocol frame.
        // 1. it reads the header.
        // 2. it determines the frame size.
        // 3. loops while not all frame received or an error.
        //
        private void ReadFrameComplete(IAsyncResult transportResult)
        {
            do
            {
                if (!(transportResult.AsyncState is WorkerAsyncResult))
                {
                    NetEventSource.Fail(this, $"The state expected to be WorkerAsyncResult, received {transportResult}.");
                }

                WorkerAsyncResult workerResult = (WorkerAsyncResult)transportResult.AsyncState;

                int bytesRead = _transport.EndRead(transportResult);
                workerResult.Offset += bytesRead;

                if (!(workerResult.Offset <= workerResult.End))
                {
                    NetEventSource.Fail(this, $"WRONG: offset - end = {workerResult.Offset - workerResult.End}");
                }

                if (bytesRead <= 0)
                {
                    // (by design) This indicates the stream has receives EOF
                    // If we are in the middle of a Frame - fail, otherwise - produce EOF
                    object result = null;
                    if (!workerResult.HeaderDone && workerResult.Offset == 0)
                    {
                        result = (object)-1;
                    }
                    else
                    {
                        result = new System.IO.IOException(SR.net_frame_read_io);
                    }

                    workerResult.InvokeCallback(result);
                    return;
                }

                if (workerResult.Offset >= workerResult.End)
                {
                    if (!workerResult.HeaderDone)
                    {
                        workerResult.HeaderDone = true;
                        // This indicates the header has been read successfully
                        _curReadHeader.CopyFrom(workerResult.Buffer, 0, _readVerifier);
                        int payloadSize = _curReadHeader.PayloadSize;
                        if (payloadSize < 0)
                        {
                            // Let's call user callback and he call us back and we will throw
                            workerResult.InvokeCallback(new System.IO.IOException(SR.Format(SR.net_frame_read_size)));
                        }

                        if (payloadSize == 0)
                        {
                            // report empty frame (NOT eof!) to the caller, he might be interested in
                            workerResult.InvokeCallback(0);
                            return;
                        }

                        if (payloadSize > _curReadHeader.MaxMessageSize)
                        {
                            throw new InvalidOperationException(SR.Format(SR.net_frame_size,
                                                                            _curReadHeader.MaxMessageSize.ToString(NumberFormatInfo.InvariantInfo),
                                                                            payloadSize.ToString(NumberFormatInfo.InvariantInfo)));
                        }

                        // Start reading the remaining frame data (note header does not count).
                        byte[] frame = new byte[payloadSize];
                        // Save the ref of the data block
                        workerResult.Buffer = frame;
                        workerResult.End = frame.Length;
                        workerResult.Offset = 0;

                        // Transport.BeginRead below will pickup those changes.
                    }
                    else
                    {
                        workerResult.HeaderDone = false; // Reset for optional object reuse.
                        workerResult.InvokeCallback(workerResult.End);
                        return;
                    }
                }

                // This means we need more data to complete the data block.
                transportResult = _transport.BeginRead(workerResult.Buffer, workerResult.Offset, workerResult.End - workerResult.Offset,
                                            _readFrameCallback, workerResult);
            } while (transportResult.CompletedSynchronously);
        }

        //
        // User code will call this when workerResult gets signaled.
        //
        // On BeginRead, the user always gets back our WorkerAsyncResult.
        // The Result property represents either a number of bytes read or an
        // exception put by our async state machine.
        //
        public byte[] EndReadMessage(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }
            WorkerAsyncResult workerResult = asyncResult as WorkerAsyncResult;

            if (workerResult == null)
            {
                throw new ArgumentException(SR.Format(SR.net_io_async_result, typeof(WorkerAsyncResult).FullName), nameof(asyncResult));
            }

            if (!workerResult.InternalPeekCompleted)
            {
                workerResult.InternalWaitForCompletion();
            }

            if (workerResult.Result is Exception)
            {
                throw (Exception)(workerResult.Result);
            }

            int size = (int)workerResult.Result;
            if (size == -1)
            {
                _eof = true;
                return null;
            }
            else if (size == 0)
            {
                // Empty frame.
                return Array.Empty<byte>();
            }

            return workerResult.Buffer;
        }

        public void WriteMessage(byte[] message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            _writeHeader.PayloadSize = message.Length;
            _writeHeader.CopyTo(_writeHeaderBuffer, 0);

            Transport.Write(_writeHeaderBuffer, 0, _writeHeaderBuffer.Length);
            if (message.Length == 0)
            {
                return;
            }

            Transport.Write(message, 0, message.Length);
        }

        public IAsyncResult BeginWriteMessage(byte[] message, AsyncCallback asyncCallback, object stateObject)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            _writeHeader.PayloadSize = message.Length;
            _writeHeader.CopyTo(_writeHeaderBuffer, 0);

            if (message.Length == 0)
            {
                return _transport.BeginWrite(_writeHeaderBuffer, 0, _writeHeaderBuffer.Length,
                                                   asyncCallback, stateObject);
            }

            // Will need two async writes. Prepare the second:
            WorkerAsyncResult workerResult = new WorkerAsyncResult(this, stateObject, asyncCallback,
                                                                   message, 0, message.Length);
            
            // Charge the first:
            IAsyncResult result = _transport.BeginWrite(_writeHeaderBuffer, 0, _writeHeaderBuffer.Length,
                                 _beginWriteCallback, workerResult);

            if (result.CompletedSynchronously)
            {
                BeginWriteComplete(result);
            }

            return workerResult;
        }

        private void BeginWriteCallback(IAsyncResult transportResult)
        {
            if (!(transportResult.AsyncState is WorkerAsyncResult))
            {
                NetEventSource.Fail(this, $"The state expected to be WorkerAsyncResult, received {transportResult}.");
            }

            if (transportResult.CompletedSynchronously)
            {
                return;
            }

            var workerResult = (WorkerAsyncResult)transportResult.AsyncState;

            try
            {
                BeginWriteComplete(transportResult);
            }
            catch (Exception e)
            {
                if (e is OutOfMemoryException)
                {
                    throw;
                }

                workerResult.InvokeCallback(e);
            }
        }

        // IO COMPLETION CALLBACK
        //
        // Called when user IO request was wrapped to do several underlined IO.
        //
        private void BeginWriteComplete(IAsyncResult transportResult)
        {
            do
            {
                WorkerAsyncResult workerResult = (WorkerAsyncResult)transportResult.AsyncState;

                // First, complete the previous portion write.
                _transport.EndWrite(transportResult);

                // Check on exit criterion.
                if (workerResult.Offset == workerResult.End)
                {
                    workerResult.InvokeCallback();
                    return;
                }
                
                // Setup exit criterion.
                workerResult.Offset = workerResult.End;

                // Write next portion (frame body) using Async IO.
                transportResult = _transport.BeginWrite(workerResult.Buffer, 0, workerResult.End,
                                            _beginWriteCallback, workerResult);
            }
            while (transportResult.CompletedSynchronously);
        }

        public void EndWriteMessage(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            WorkerAsyncResult workerResult = asyncResult as WorkerAsyncResult;

            if (workerResult != null)
            {
                if (!workerResult.InternalPeekCompleted)
                {
                    workerResult.InternalWaitForCompletion();
                }

                if (workerResult.Result is Exception)
                {
                    throw (Exception)(workerResult.Result);
                }
            }
            else
            {
                _transport.EndWrite(asyncResult);
            }
        }
    }

    //
    // This class wraps an Async IO request. It is based on our internal LazyAsyncResult helper.
    // - If ParentResult is not null then the base class (LazyAsyncResult) methods must not be used.
    // - If ParentResult == null, then real user IO request is wrapped.
    //

    internal class WorkerAsyncResult : LazyAsyncResult
    {
        public byte[] Buffer;
        public int Offset;
        public int End;
        public bool HeaderDone; // This might be reworked so we read both header and frame in one chunk.

        public WorkerAsyncResult(object asyncObject, object asyncState,
                                   AsyncCallback savedAsyncCallback,
                                   byte[] buffer, int offset, int end)
            : base(asyncObject, asyncState, savedAsyncCallback)
        {
            Buffer = buffer;
            Offset = offset;
            End = end;
        }
    }

    // Describes the header used in framing of the stream data.
    internal class FrameHeader
    {
        public const int IgnoreValue = -1;
        public const int HandshakeDoneId = 20;
        public const int HandshakeErrId = 21;
        public const int HandshakeId = 22;
        public const int DefaultMajorV = 1;
        public const int DefaultMinorV = 0;

        private int _MessageId;
        private int _MajorV;
        private int _MinorV;
        private int _PayloadSize;

        public FrameHeader()
        {
            _MessageId = HandshakeId;
            _MajorV = DefaultMajorV;
            _MinorV = DefaultMinorV;
            _PayloadSize = -1;
        }

        public FrameHeader(int messageId, int majorV, int minorV)
        {
            _MessageId = messageId;
            _MajorV = majorV;
            _MinorV = minorV;
            _PayloadSize = -1;
        }

        public int Size
        {
            get
            {
                return 5;
            }
        }

        public int MaxMessageSize
        {
            get
            {
                return 0xFFFF;
            }
        }

        public int MessageId
        {
            get
            {
                return _MessageId;
            }
            set
            {
                _MessageId = value;
            }
        }

        public int MajorV
        {
            get
            {
                return _MajorV;
            }
        }

        public int MinorV
        {
            get
            {
                return _MinorV;
            }
        }

        public int PayloadSize
        {
            get
            {
                return _PayloadSize;
            }
            set
            {
                if (value > MaxMessageSize)
                {
                    throw new ArgumentException(SR.Format(SR.net_frame_max_size,
                        MaxMessageSize.ToString(NumberFormatInfo.InvariantInfo),
                        value.ToString(NumberFormatInfo.InvariantInfo)), "PayloadSize");
                }

                _PayloadSize = value;
            }
        }

        public void CopyTo(byte[] dest, int start)
        {
            dest[start++] = (byte)_MessageId;
            dest[start++] = (byte)_MajorV;
            dest[start++] = (byte)_MinorV;
            dest[start++] = (byte)((_PayloadSize >> 8) & 0xFF);
            dest[start] = (byte)(_PayloadSize & 0xFF);
        }

        public void CopyFrom(byte[] bytes, int start, FrameHeader verifier)
        {
            _MessageId = bytes[start++];
            _MajorV = bytes[start++];
            _MinorV = bytes[start++];
            _PayloadSize = (int)((bytes[start++] << 8) | bytes[start]);

            if (verifier.MessageId != FrameHeader.IgnoreValue && MessageId != verifier.MessageId)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_header_id, "MessageId", MessageId, verifier.MessageId));
            }

            if (verifier.MajorV != FrameHeader.IgnoreValue && MajorV != verifier.MajorV)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_header_id, "MajorV", MajorV, verifier.MajorV));
            }

            if (verifier.MinorV != FrameHeader.IgnoreValue && MinorV != verifier.MinorV)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_header_id, "MinorV", MinorV, verifier.MinorV));
            }
        }
    }
}
