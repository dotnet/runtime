// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Tests
{
    public class CallTrackingStream : Stream
    {
        private readonly Dictionary<string, int> _callCounts; // maps names of methods -> how many times they were called

        public CallTrackingStream(Stream inner)
        {
            Debug.Assert(inner != null);

            Inner = inner;
            _callCounts = new Dictionary<string, int>();
        }

        public Stream Inner { get; }

        // Overridden Stream properties

        public override bool CanRead => Read(Inner.CanRead);
        public override bool CanWrite => Read(Inner.CanWrite);
        public override bool CanSeek => Read(Inner.CanSeek);
        public override bool CanTimeout => Read(Inner.CanTimeout);

        public override long Length => Read(Inner.Length);

        public override long Position
        {
            get { return Read(Inner.Position); }
            set { Update(() => Inner.Position = value); }
        }

        public override int ReadTimeout
        {
            get { return Read(Inner.ReadTimeout); }
            set { Update(() => Inner.ReadTimeout = value); }
        }

        public override int WriteTimeout
        {
            get { return Read(Inner.WriteTimeout); }
            set { Update(() => Inner.WriteTimeout = value); }
        }

        // Arguments we record
        // We can just use regular, auto-implemented properties for these,
        // since we know none of them are going to be called by the framework

        public Stream CopyToAsyncDestination { get; private set; }
        public int CopyToAsyncBufferSize { get; private set; }
        public CancellationToken CopyToAsyncCancellationToken { get; private set; }

        public bool DisposeDisposing { get; private set; }

        public CancellationToken FlushAsyncCancellationToken { get; private set; }

        public byte[] ReadBuffer { get; private set; }
        public int ReadOffset { get; private set; }
        public int ReadCount { get; private set; }

        public byte[] ReadAsyncBuffer { get; private set; }
        public int ReadAsyncOffset { get; private set; }
        public int ReadAsyncCount { get; private set; }
        public CancellationToken ReadAsyncCancellationToken { get; private set; }

        public long SeekOffset { get; private set; }
        public SeekOrigin SeekOrigin { get; private set; }

        public long SetLengthValue { get; private set; }

        public byte[] WriteBuffer { get; private set; }
        public int WriteOffset { get; private set; }
        public int WriteCount { get; private set; }

        public byte[] WriteAsyncBuffer { get; private set; }
        public int WriteAsyncOffset { get; private set; }
        public int WriteAsyncCount { get; private set; }
        public CancellationToken WriteAsyncCancellationToken { get; private set; }

        public byte WriteByteValue { get; private set; }

        // Overridden methods

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            UpdateCallCount();
            CopyToAsyncDestination = destination;
            CopyToAsyncBufferSize = bufferSize;
            CopyToAsyncCancellationToken = cancellationToken;
            return Inner.CopyToAsync(destination, bufferSize, cancellationToken);
        }
        
        // Skip Dispose; it's not accessible to us since the virtual overload is protected

        public override void Flush()
        {
            UpdateCallCount();
            Inner.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            UpdateCallCount();
            FlushAsyncCancellationToken = cancellationToken;
            return Inner.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            UpdateCallCount();
            ReadBuffer = buffer;
            ReadOffset = offset;
            ReadCount = count;
            return Inner.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            UpdateCallCount();
            ReadAsyncBuffer = buffer;
            ReadAsyncOffset = offset;
            ReadAsyncCount = count;
            ReadAsyncCancellationToken = cancellationToken;
            return Inner.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int ReadByte()
        {
            UpdateCallCount();
            return Inner.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            UpdateCallCount();
            SeekOffset = offset;
            SeekOrigin = origin;
            return Inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            UpdateCallCount();
            SetLengthValue = value;
            Inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            UpdateCallCount();
            WriteBuffer = buffer;
            WriteOffset = offset;
            WriteCount = count;
            Inner.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            UpdateCallCount();
            WriteAsyncBuffer = buffer;
            WriteAsyncOffset = offset;
            WriteAsyncCount = count;
            WriteAsyncCancellationToken = cancellationToken;
            return Inner.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            UpdateCallCount();
            WriteByteValue = value;
            Inner.WriteByte(value);
        }

        // Bookkeeping logic

        public int TimesCalled(string member)
        {
            int result;
            _callCounts.TryGetValue(member, out result);
            return result; // not present means we haven't called it yet, so return 0
        }

        // [CallerMemberName] causes the member parameter to be set to the name
        // of the calling member if not specified, e.g. calling this method
        // from SetLength would pass in member with a value of "SetLength"
        private T Read<T>(T property, [CallerMemberName] string member = null)
        {
            UpdateCallCount(member);
            return property;
        }

        private void Update(Action setter, [CallerMemberName] string member = null)
        {
            UpdateCallCount(member);
            setter();
        }

        private void UpdateCallCount([CallerMemberName] string member = null)
        {
            _callCounts[member] = TimesCalled(member) + 1;
        }
    }
}
