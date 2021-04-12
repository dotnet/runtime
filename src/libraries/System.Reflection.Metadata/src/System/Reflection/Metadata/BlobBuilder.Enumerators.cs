// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System;

namespace System.Reflection.Metadata
{
    public partial class BlobBuilder
    {
        // internal for testing
        internal struct Chunks : IEnumerable<BlobBuilder>, IEnumerator<BlobBuilder>, IEnumerator
        {
            private readonly BlobBuilder _head;
            private BlobBuilder _next;
            private BlobBuilder? _currentOpt;

            internal Chunks(BlobBuilder builder)
            {
                Debug.Assert(builder.IsHead);

                _head = builder;
                _next = builder.FirstChunk;
                _currentOpt = null;
            }

            object IEnumerator.Current => Current;
            public readonly BlobBuilder Current => _currentOpt!;

            public bool MoveNext()
            {
                if (_currentOpt == _head)
                {
                    return false;
                }

                if (_currentOpt == _head._nextOrPrevious)
                {
                    _currentOpt = _head;
                    return true;
                }

                _currentOpt = _next;
                _next = _next._nextOrPrevious;
                return true;
            }

            public void Reset()
            {
                _currentOpt = null;
                _next = _head.FirstChunk;
            }

            void IDisposable.Dispose() { }

            // IEnumerable:
            public readonly Chunks GetEnumerator() => this;
            readonly IEnumerator<BlobBuilder> IEnumerable<BlobBuilder>.GetEnumerator() => GetEnumerator();
            readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public struct Blobs : IEnumerable<Blob>, IEnumerator<Blob>, IEnumerator
        {
            private Chunks _chunks;

            internal Blobs(BlobBuilder builder)
            {
                _chunks = new Chunks(builder);
            }

            object IEnumerator.Current => Current;

            public readonly Blob Current
            {
                get
                {
                    var current = _chunks.Current;
                    if (current != null)
                    {
                        return new Blob(current._buffer, 0, current.Length);
                    }
                    else
                    {
                        return default(Blob);
                    }
                }
            }

            public bool MoveNext() => _chunks.MoveNext();
            public void Reset() => _chunks.Reset();

            void IDisposable.Dispose() { }

            // IEnumerable:
            public readonly Blobs GetEnumerator() => this;
            readonly IEnumerator<Blob> IEnumerable<Blob>.GetEnumerator() => GetEnumerator();
            readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
