// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO;

namespace Tracing.Tests.Common
{
    // This Stream implementation takes one stream
    // and proxies the Stream API to it while
    // saving any read bytes to an internal stream.
    // Should an error occur, the internal stream
    // is dumped to disk for reproducing the error.
    public class StreamProxy : Stream
    {
        private Stream ProxiedStream { get; }
        private MemoryStream InternalStream => new MemoryStream();
        public override bool CanRead => ProxiedStream.CanRead;

        public override bool CanSeek => ProxiedStream.CanSeek;

        public override bool CanWrite => ProxiedStream.CanWrite;

        public override long Length => ProxiedStream.Length;

        public override long Position { get => ProxiedStream.Position; set => ProxiedStream.Position = value; }

        public StreamProxy(Stream streamToProxy)
        {
            ProxiedStream = streamToProxy;
        }

        public override void Flush() => ProxiedStream.Flush();

        // Read the actual desired amount of bytes into an empty buffer
        // copy those bytes into an internal MemoryStream THEN
        // forward those bytes to the caller. If the caller
        // would have thrown an exception with its Read, copy
        // the bytes to the internal MemoryStream and then throw
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null || offset < 0 || count < 0)
                throw new ArgumentException("Invalid input into Read");

            byte[] localBuffer = ArrayPool<byte>.Shared.Rent(count);
            try
            {
                var readCount = ProxiedStream.Read(localBuffer, 0, count);
                if (readCount == 0)
                    return readCount;

                InternalStream.Write(localBuffer, 0, readCount);

                // copy the data into the caller's buffer
                Array.Copy(localBuffer, 0, buffer, offset, readCount);
                return readCount;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(localBuffer, true);
            }

        }

        public override long Seek(long offset, SeekOrigin origin) => ProxiedStream.Seek(offset, origin);

        public override void SetLength(long value) => ProxiedStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            // This stream is only for "reading" from. No need for this method.
            throw new System.NotImplementedException();
        }

        protected bool disposed = false;
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ProxiedStream.Dispose();
                    InternalStream.Dispose();
                }

                disposed = true;
            }
        }

        public void DumpStreamToDisk()
        {
            var helixWorkItemDirectory = System.Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
            if (helixWorkItemDirectory != null && Directory.Exists(helixWorkItemDirectory))
            {
                Logger.logger.Log($"\t HELIX_WORKITEM_UPLOAD_ROOT = {helixWorkItemDirectory}");
                var filePath = Path.Combine(helixWorkItemDirectory, "streamdump.nettrace");
                using (var streamDumpFile = File.Create(filePath))
                {
                    Logger.logger.Log($"\t Writing stream to {filePath}");
                    Logger.logger.Log($"\t  length of data: {InternalStream.Length} Bytes");
                    InternalStream.WriteTo(streamDumpFile);
                }
            }
        }
    }
}
