// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace System.Net
{
    //
    // The class is a simple wrapper on top of a read stream. It will read the exact number of bytes requested.
    // It will throw if EOF is reached before the expected number of bytes is returned.
    //
    internal class FixedSizeReader
    {
        private static readonly AsyncCallback s_readCallback = new AsyncCallback(ReadCallback);

        // TODO (Issue #3114): Implement this using TPL instead of APM.
        private readonly StreamAsyncHelper _transportAPM;
        private readonly Stream _transport;
        private AsyncProtocolRequest _request;
        private int _totalRead;

        public FixedSizeReader(Stream transport)
        {
            _transport = transport;
            _transportAPM = new StreamAsyncHelper(transport);
        }

        //
        // Returns 0 on legitimate EOF or if 0 bytes were requested, otherwise reads as directed or throws.
        // Returns count on success.
        //
        public int ReadPacket(byte[] buffer, int offset, int count)
        {
            int tempCount = count;
            do
            {
                int bytes = _transport.Read(buffer, offset, tempCount);

                if (bytes == 0)
                {
                    if (tempCount != count)
                    {
                        throw new IOException(SR.net_io_eof);
                    }

                    return 0;
                }

                tempCount -= bytes;
                offset += bytes;
            } while (tempCount != 0);

            return count;
        }

        //
        // Completes "_Request" with 0 if 0 bytes was requested or legitimate EOF received.
        // Otherwise, reads as directed or completes "_Request" with an Exception or throws.
        //
        public void AsyncReadPacket(AsyncProtocolRequest request)
        {
            _request = request;
            _totalRead = 0;
            StartReading();
        }
        //
        // Loops while subsequent completions are sync.
        //
        private void StartReading()
        {
            while (true)
            {
                IAsyncResult ar = _transportAPM.BeginRead(_request.Buffer, _request.Offset + _totalRead, _request.Count - _totalRead, s_readCallback, this);
                if (!ar.CompletedSynchronously)
                {
#if DEBUG
                    _request._DebugAsyncChain = ar;
#endif
                    break;
                }

                int bytes = _transportAPM.EndRead(ar);

                if (CheckCompletionBeforeNextRead(bytes))
                {
                    break;
                }
            }
        }

        private bool CheckCompletionBeforeNextRead(int bytes)
        {
            if (bytes == 0)
            {
                // 0 bytes was requested or EOF in the beginning of a frame, the caller should decide whether it's OK.
                if (_totalRead == 0)
                {
                    _request.CompleteRequest(0);
                    return true;
                }

                // EOF in the middle of a frame.
                throw new IOException(SR.net_io_eof);
            }

            GlobalLog.Assert(_totalRead + bytes <= _request.Count, "FixedSizeReader::CheckCompletion()|State got out of range. Total:{0} Count:{1}", _totalRead + bytes, _request.Count);

            if ((_totalRead += bytes) == _request.Count)
            {
                _request.CompleteRequest(_request.Count);
                return true;
            }

            return false;
        }

        private static void ReadCallback(IAsyncResult transportResult)
        {
            GlobalLog.Assert(transportResult.AsyncState is FixedSizeReader, "ReadCallback|State type is wrong, expected FixedSizeReader.");
            if (transportResult.CompletedSynchronously)
            {
                return;
            }

            FixedSizeReader reader = (FixedSizeReader)transportResult.AsyncState;
            AsyncProtocolRequest request = reader._request;

            // Async completion.
            try
            {
                int bytes = reader._transportAPM.EndRead(transportResult);

                if (reader.CheckCompletionBeforeNextRead(bytes))
                {
                    return;
                }

                reader.StartReading();
            }
            catch (Exception e)
            {
                if (request.IsUserCompleted)
                {
                    throw;
                }

                request.CompleteWithError(e);
            }
        }
    }
}
