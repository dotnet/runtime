// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    internal sealed class AsyncReadLinesEnumerable : IAsyncEnumerable<string>
    {
        private bool _alreadyGetEnumerator;
        private readonly int _initialThreadId = Environment.CurrentManagedThreadId;
        private readonly string _path;
        private readonly Encoding _encoding;
        private readonly StreamReader _sr;
        private readonly CancellationToken _cancellationToken;

        public AsyncReadLinesEnumerable(string path, Encoding encoding, StreamReader sr, CancellationToken cancellationToken)
        {
            _path = path;
            _encoding = encoding;
            _sr = sr;
            _cancellationToken = cancellationToken;
        }

        public IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            StreamReader sr;
            if (_alreadyGetEnumerator || _initialThreadId != Environment.CurrentManagedThreadId)
            {
                sr = new StreamReader(_path, _encoding);
            }
            else
            {
                sr = _sr;
                _alreadyGetEnumerator = true;
            }

            return new AsyncReadLinesEnumerator(sr, CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, cancellationToken));
        }
    }

    internal sealed class AsyncReadLinesEnumerator : IAsyncEnumerator<string>
    {
        private readonly StreamReader _sr;
        private readonly CancellationTokenSource _cts;
        private string? _current;

        public AsyncReadLinesEnumerator(StreamReader sr, CancellationTokenSource cts)
        {
            _sr = sr;
            _cts = cts;
        }

        public ValueTask DisposeAsync()
        {
            _sr.Dispose();
            _cts.Dispose();
            return ValueTask.CompletedTask;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            _current = await _sr.ReadLineAsync(_cts.Token).ConfigureAwait(false);
            return _current is not null;
        }

        public string Current => _current!;
    }
}
