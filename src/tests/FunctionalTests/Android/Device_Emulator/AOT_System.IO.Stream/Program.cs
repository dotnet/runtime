// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable enable

using System;
using System.IO;

// https://github.com/dotnet/runtime/issues/67402

public static class Program
{
    public static int Main(string[] args)
    {
        var stream = new DummyStream();
        var buffer = new byte[stream.Length];
        int read = stream.ReadAsync(buffer, 0, buffer.Length).GetAwaiter().GetResult();
        return read + buffer[0];
    }

    private sealed class DummyStream : System.IO.Stream
    {
        protected override void Dispose (bool disposing) => throw new NotImplementedException ();

        public override int Read (byte[] buffer, int offset, int count)
        {
            buffer[0] = 41;
            return 1;
        }

        public override long Seek (long offset, SeekOrigin origin) => 0;
        public override void SetLength (long value) {}
        public override void Write (byte[] buffer, int offset, int count) {}
        public override void Flush () {}

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            Console.WriteLine("BeginRead");
            return base.BeginRead(buffer, offset, count, callback, state);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override long Length => 1;
        public override long Position { get; set; } = 0;
    }
}
