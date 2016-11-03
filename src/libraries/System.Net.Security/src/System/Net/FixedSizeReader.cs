// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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

        private readonly Stream _transport;
        private AsyncProtocolRequest _request;
        private int _totalRead;

        public FixedSizeReader(Stream transport)
        {
            _transport = transport;
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
                IAsyncResult ar = _transport.BeginRead(_request.Buffer, _request.Offset + _totalRead, _request.Count - _totalRead, s_readCallback, this);
                if (!ar.CompletedSynchronously)
                {
#if DEBUG
                    _request._DebugAsyncChain = ar;
#endif
                    break;
                }

                int bytes = _transport.EndRead(ar);

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

            if (_totalRead + bytes > _request.Count)
            {
                NetEventSource.Fail(this, $"State got out of range. Total:{_totalRead + bytes} Count:{_request.Count}");
            }

            if ((_totalRead += bytes) == _request.Count)
            {
                _request.CompleteRequest(_request.Count);
                return true;
            }

            return false;
        }

        private static void ReadCallback(IAsyncResult transportResult)
        {
            if (!(transportResult.AsyncState is FixedSizeReader))
            {
                NetEventSource.Fail(null, "State type is wrong, expected FixedSizeReader.");
            }

            if (transportResult.CompletedSynchronously)
            {
                return;
            }

            FixedSizeReader reader = (FixedSizeReader)transportResult.AsyncState;
            AsyncProtocolRequest request = reader._request;

            // Async completion.
            try
            {
                int bytes = reader._transport.EndRead(transportResult);

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

                request.CompleteUserWithError(e);
            }
        }
    }
}
