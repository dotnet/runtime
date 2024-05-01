// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Tests
{
    public sealed class Utf8MemoryStream : MemoryStream
    {
        private readonly bool _ignoreCancellationTokenOnWriteAsync;

        public Utf8MemoryStream(bool ignoreCancellationTokenOnWriteAsync = false) : base()
        {
            _ignoreCancellationTokenOnWriteAsync = ignoreCancellationTokenOnWriteAsync;
        }

        public Utf8MemoryStream(string text) : base(Encoding.UTF8.GetBytes(text))
        {
        }

#if NET
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => base.WriteAsync(buffer, _ignoreCancellationTokenOnWriteAsync ? default : cancellationToken);
#endif
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => base.WriteAsync(buffer, offset, count, _ignoreCancellationTokenOnWriteAsync ? default : cancellationToken);

        public string AsString() => Encoding.UTF8.GetString(ToArray());
    }
}
