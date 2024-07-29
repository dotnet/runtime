// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http
{
    internal struct RequestQueue<T>
        where T : HttpConnectionBase?
    {
        public struct QueueItem
        {
            public HttpRequestMessage Request;
            public HttpConnectionWaiter<T> Waiter;
        }

        // This implementation mimics that of Queue<T>, but without version checks and with an extra head pointer
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Queue.cs
        private QueueItem[] _array;
        private int _head; // The index from which to dequeue if the queue isn't empty.
        private int _tail; // The index at which to enqueue if the queue isn't full.
        private int _size; // Number of elements.
        private int _attemptedConnectionsOffset; // The offset from head where we should next peek for a request without a connection attempt

        public RequestQueue()
        {
            _array = Array.Empty<QueueItem>();
            _head = 0;
            _tail = 0;
            _size = 0;
            _attemptedConnectionsOffset = 0;
        }

        private void Enqueue(QueueItem queueItem)
        {
            if (_size == _array.Length)
            {
                Grow();
            }

            _array[_tail] = queueItem;
            MoveNext(ref _tail);

            _size++;
        }

        private QueueItem Dequeue()
        {
            Debug.Assert(_size > 0);

            int head = _head;
            QueueItem[] array = _array;

            QueueItem queueItem = array[head];
            array[head] = default;

            MoveNext(ref _head);

            if (_attemptedConnectionsOffset > 0)
            {
                _attemptedConnectionsOffset--;
            }

            _size--;
            return queueItem;
        }

        private bool TryPeek(out QueueItem queueItem)
        {
            if (_size == 0)
            {
                queueItem = default!;
                return false;
            }

            queueItem = _array[_head];
            return true;
        }

        private void MoveNext(ref int index)
        {
            int tmp = index + 1;
            if (tmp == _array.Length)
            {
                tmp = 0;
            }
            index = tmp;
        }

        private void Grow()
        {
            var newArray = new QueueItem[Math.Max(4, _array.Length * 2)];

            if (_size != 0)
            {
                if (_head < _tail)
                {
                    Array.Copy(_array, _head, newArray, 0, _size);
                }
                else
                {
                    Array.Copy(_array, _head, newArray, 0, _array.Length - _head);
                    Array.Copy(_array, 0, newArray, _array.Length - _head, _tail);
                }
            }

            _array = newArray;
            _head = 0;
            _tail = _size;
        }


        public HttpConnectionWaiter<T> EnqueueRequest(HttpRequestMessage request)
        {
            var waiter = new HttpConnectionWaiter<T>();
            EnqueueRequest(request, waiter);
            return waiter;
        }


        public void EnqueueRequest(HttpRequestMessage request, HttpConnectionWaiter<T> waiter)
        {
            Enqueue(new QueueItem { Request = request, Waiter = waiter });
        }

        public void PruneCompletedRequestsFromHeadOfQueue(HttpConnectionPool pool)
        {
            while (TryPeek(out QueueItem queueItem) && queueItem.Waiter.Task.IsCompleted)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    pool.Trace(queueItem.Waiter.Task.IsCanceled
                        ? "Discarding canceled request from queue."
                        : "Discarding signaled request waiter from queue.");
                }

                Dequeue();
            }
        }

        public bool TryDequeueWaiter(HttpConnectionPool pool, [MaybeNullWhen(false)] out HttpConnectionWaiter<T> waiter)
        {
            PruneCompletedRequestsFromHeadOfQueue(pool);

            if (Count != 0)
            {
                waiter = Dequeue().Waiter;
                return true;
            }

            waiter = null;
            return false;
        }

        public void TryDequeueSpecificWaiter(HttpConnectionWaiter<T> waiter)
        {
            if (TryPeek(out QueueItem queueItem) && queueItem.Waiter == waiter)
            {
                Dequeue();
            }
        }

        public QueueItem PeekNextRequestForConnectionAttempt()
        {
            Debug.Assert(_attemptedConnectionsOffset >= 0);
            Debug.Assert(_attemptedConnectionsOffset < _size, $"{_attemptedConnectionsOffset} < {_size}");

            int index = _head + _attemptedConnectionsOffset;
            _attemptedConnectionsOffset++;

            if (index >= _array.Length)
            {
                index -= _array.Length;
            }

            return _array[index];
        }

        public int Count => _size;

        public int RequestsWithoutAConnectionAttempt => _size - _attemptedConnectionsOffset;
    }
}
