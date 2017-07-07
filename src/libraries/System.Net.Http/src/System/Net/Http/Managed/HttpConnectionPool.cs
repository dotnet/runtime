// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace System.Net.Http
{
    internal sealed class HttpConnectionPool : IDisposable
    {
        private readonly ConcurrentConnectionSet _activeConnections;
        private readonly ConcurrentBag<HttpConnection> _idleConnections;
        private bool _disposed;

        public HttpConnectionPool()
        {
            _activeConnections = new ConcurrentConnectionSet();
            _idleConnections = new ConcurrentBag<HttpConnection>();
        }

        public HttpConnection GetConnection()
        {
            while (_idleConnections.TryTake(out HttpConnection connection))
            {
                if (connection.ReadAheadCompleted)
                {
                    // We got a connection, but it was either already closed by the server or the
                    // server sent unexpected data.  Either way, we can't use the connection.
                    connection.Dispose();
                    continue;
                }

                if (!_activeConnections.TryAdd(connection))
                {
                    throw new InvalidOperationException();
                }

                return connection;
            }

            return null;
        }

        public void AddConnection(HttpConnection connection)
        {
            if (!_activeConnections.TryAdd(connection))
            {
                throw new InvalidOperationException();
            }
        }

        public void PutConnection(HttpConnection connection)
        {
            if (!_activeConnections.TryRemove(connection))
            {
                throw new InvalidOperationException();
            }

            _idleConnections.Add(connection);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                foreach (HttpConnection connection in _activeConnections.ToArray())
                {
                    connection.Dispose();
                }

                foreach (HttpConnection connection in _idleConnections)
                {
                    connection.Dispose();
                }
            }
        }

        private sealed class ConcurrentConnectionSet
        {
            private static readonly int s_numSlots = Math.Max(4, RoundUpToPowerOf2(Environment.ProcessorCount));
            private static readonly int s_slotsMask = s_numSlots - 1;
            private readonly HashSet<HttpConnection>[] _sets;

            public ConcurrentConnectionSet()
            {
                _sets = new HashSet<HttpConnection>[s_numSlots];
                for (int i = 0; i < _sets.Length; i++)
                {
                    _sets[i] = new HashSet<HttpConnection>();
                }
            }

            public bool TryAdd(HttpConnection connection)
            {
                HashSet<HttpConnection> set = _sets[connection.GetHashCode() & s_slotsMask];
                lock (set) return set.Add(connection);
            }

            public bool TryRemove(HttpConnection connection)
            {
                HashSet<HttpConnection> set = _sets[connection.GetHashCode() & s_slotsMask];
                lock (set) return set.Remove(connection);
            }

            public HttpConnection[] ToArray()
            {
                var results = new List<HttpConnection>();
                foreach (HashSet<HttpConnection> set in _sets)
                {
                    lock (set) results.AddRange(set);
                }
                return results.ToArray();
            }

            /// <summary>Round the specified value up to the next power of 2, if it isn't one already.</summary>
            private static int RoundUpToPowerOf2(int i)
            {
                --i;
                i |= i >> 1;
                i |= i >> 2;
                i |= i >> 4;
                i |= i >> 8;
                i |= i >> 16;
                return i + 1;
            }
        }
    }
}
