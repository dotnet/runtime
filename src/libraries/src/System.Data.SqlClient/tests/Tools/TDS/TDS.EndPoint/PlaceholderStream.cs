// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlServer.TDS.EndPoint
{
    /// <summary>
    /// A simple pass-through implementation of stream that allows dynamically switching the underlying stream
    /// </summary>
    public class PlaceholderStream : Stream
    {
        /// <summary>
        /// Indicates whether inner stream should be closed when TDS stream is closed
        /// </summary>
        private bool _leaveInnerStreamOpen = false;

        /// <summary>
        /// Transport stream used to deliver TDS protocol
        /// </summary>
        public Stream InnerStream { get; set; }

        /// <summary>
        /// Indicates whether stream can be read
        /// </summary>
        public override bool CanRead
        {
            // Delegate to the inner stream
            get { return InnerStream.CanRead; }
        }

        /// <summary>
        /// Indicates whether the stream can be positioned
        /// </summary>
        public override bool CanSeek
        {
            // Delegate to the inner stream
            get { return InnerStream.CanSeek; }
        }

        /// <summary>
        /// Indicates whether the stream can be written
        /// </summary>
        public override bool CanWrite
        {
            // Delegate to the inner stream
            get { return InnerStream.CanWrite; }
        }

        /// <summary>
        /// Return the length of the stream
        /// </summary>
        public override long Length
        {
            // Delegate to the inner stream
            get { return InnerStream.Length; }
        }

        /// <summary>
        /// Return position in the stream
        /// </summary>
        public override long Position
        {
            // Delegate to the inner stream
            get { return InnerStream.Position; }
            set { InnerStream.Position = value; }
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public PlaceholderStream(Stream innerStream) :
            this(innerStream, true)
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public PlaceholderStream(Stream innerStream, bool leaveInnerStreamOpen)
        {
            // Check if inner stream is valid
            if (innerStream == null)
            {
                // We can't proceed without underlying stream
                throw new ArgumentNullException(nameof(innerStream), "Underlying stream is required");
            }

            // Save transport stream
            InnerStream = innerStream;

            // Save whether inner stream is to be closed as well
            _leaveInnerStreamOpen = leaveInnerStreamOpen;
        }

        /// <summary>
        /// Close the stream
        /// </summary>
        public override void Close()
        {
            // Check if inner stream needs to be closed
            if (!_leaveInnerStreamOpen)
            {
                // Close inner stream
                InnerStream.Close();
            }

            // Delegate to the base class
            base.Close();
        }

        /// <summary>
        /// Flush the data into the underlying stream
        /// </summary>
        public override void Flush()
        {
            // Delegate to the inner stream
            InnerStream.Flush();
        }

        /// <summary>
        /// Read the data from the stream
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // Delegate to the underlying stream
            return InnerStream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Seek position in the stream
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            // Delegate to the inner stream
            return InnerStream.Seek(offset, origin);
        }

        /// <summary>
        /// Set stream length
        /// </summary>
        public override void SetLength(long value)
        {
            // Delegate to the inner stream
            InnerStream.SetLength(value);
        }

        /// <summary>
        /// Write data into the stream
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            // Delegate to underlying stream
            InnerStream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return InnerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return InnerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

    }
}
