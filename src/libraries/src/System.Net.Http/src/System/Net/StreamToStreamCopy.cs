// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;

namespace System.Net.Http
{
    // This helper class is used to copy the content of a source stream to a destination stream.
    // The type verifies if the source and/or destination stream are MemoryStreams (or derived types). If so, sync 
    // read/write is used on the MemoryStream to avoid context switches.
    internal class StreamToStreamCopy
    {
        private byte[] _buffer;
        private int _bufferSize;
        private Stream _source;
        private Stream _destination;
        private bool _disposeSource;
        private bool _sourceIsMemoryStream;
        private bool _destinationIsMemoryStream;

        public StreamToStreamCopy(Stream source, Stream destination, int bufferSize, bool disposeSource)
        {
            Contract.Requires(source != null);
            Contract.Requires(destination != null);
            Contract.Requires(bufferSize > 0);

            _buffer = new byte[bufferSize];
            _source = source;
            _destination = destination;
            _bufferSize = bufferSize;
            _disposeSource = disposeSource;
            _sourceIsMemoryStream = source is MemoryStream;
            _destinationIsMemoryStream = destination is MemoryStream;
        }

        public async Task StartAsync()
        {
            // If both streams are MemoryStreams, just copy the whole content at once to avoid context switches.
            // This will not block since it will just result in a memcopy.
            if (_sourceIsMemoryStream && _destinationIsMemoryStream)
            {
                for (; ;)
                {
                    int bytesRead = _source.Read(_buffer, 0, _bufferSize);
                    if (bytesRead == 0)
                        break;
                    _destination.Write(_buffer, 0, bytesRead);
                }
            }
            else
            {
                for (; ;)
                {
                    int bytesRead = await _source.ReadAsync(_buffer, 0, _bufferSize).ConfigureAwait(false);
                    if (bytesRead == 0)
                        break;
                    await _destination.WriteAsync(_buffer, 0, bytesRead).ConfigureAwait(false);
                }
            }

            try
            {
                if (_disposeSource)
                {
                    _source.Dispose();
                }
            }
            catch (Exception e)
            {
                // Dispose() should never throw, but since we're on an async codepath, make sure to catch the exception.
                if (Logging.On) Logging.Exception(Logging.Http, this, "SetCompleted", e);
            }
        }
    }
}
