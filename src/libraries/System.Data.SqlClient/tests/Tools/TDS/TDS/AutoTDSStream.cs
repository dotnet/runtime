// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;

namespace Microsoft.SqlServer.TDS
{
    /// <summary>
    /// Stream that wraps TDS stream with automatic dispatch
    /// </summary>
    public class AutoTDSStream : Stream
    {
        /// <summary>
        /// Indicates whether inner stream should be closed when TDS stream is closed
        /// </summary>
        private bool _closeInnerStream = false;

        /// <summary>
        /// Inner TDS stream
        /// </summary>
        public TDSStream InnerTDSStream { get; set; }

        /// <summary>
        /// Type of the message being sent to the other party
        /// </summary>
        public TDSMessageType OutgoingMessageType { get; set; }

        /// <summary>
        /// Indicates whether stream can be read
        /// </summary>
        public override bool CanRead
        {
            // Delegate to the inner stream
            get { return InnerTDSStream.CanRead; }
        }

        /// <summary>
        /// Indicates whether the stream can be positioned
        /// </summary>
        public override bool CanSeek
        {
            // Delegate to the inner stream
            get { return InnerTDSStream.CanSeek; }
        }

        /// <summary>
        /// Indicates whether the stream can be written
        /// </summary>
        public override bool CanWrite
        {
            // Delegate to the inner stream
            get { return InnerTDSStream.CanWrite; }
        }

        /// <summary>
        /// Return the length of the stream
        /// </summary>
        public override long Length
        {
            // Delegate to the inner stream
            get { return InnerTDSStream.Length; }
        }

        /// <summary>
        /// Return position in the stream
        /// </summary>
        public override long Position
        {
            // Delegate to the inner stream
            get { return InnerTDSStream.Position; }
            set { InnerTDSStream.Position = value; }
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public AutoTDSStream(TDSStream innerTDSStream) :
            this(innerTDSStream, true)
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public AutoTDSStream(TDSStream innerTDSStream, bool closeInnerStream)
        {
            // Check if inner stream is valid
            if (innerTDSStream == null)
            {
                // We can't proceed without underlying stream
                throw new ArgumentNullException(nameof(innerTDSStream), "Underlying TDS stream is required");
            }

            // Save transport stream
            InnerTDSStream = innerTDSStream;

            // Save whether inner stream is to be closed as well
            _closeInnerStream = closeInnerStream;
        }

        /// <summary>
        /// Close the stream
        /// </summary>
        public override void Close()
        {
            // Check if inner stream needs to be closed
            if (_closeInnerStream)
            {
                // Close inner stream
                InnerTDSStream.Close();
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
            InnerTDSStream.Flush();
        }

        /// <summary>
        /// Read the data from the stream
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // Delegate to the underlying stream
            return InnerTDSStream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Seek position in the stream
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            // Delegate to the inner stream
            return InnerTDSStream.Seek(offset, origin);
        }

        /// <summary>
        /// Set stream length
        /// </summary>
        public override void SetLength(long value)
        {
            // Delegate to the inner stream
            InnerTDSStream.SetLength(value);
        }

        /// <summary>
        /// Write data into the stream
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            // Start a new TDS message
            InnerTDSStream.StartMessage(OutgoingMessageType);

            // Delegate to the inner TDS stream
            InnerTDSStream.Write(buffer, offset, count);

            // Complete this message
            InnerTDSStream.EndMessage();
        }
    }
}
