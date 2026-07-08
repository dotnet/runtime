// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Security.Cryptography.X509Certificates
{
    internal abstract class X509MruCache<T> where T : class
    {
        // The CRL cache only stores a SafeHandle, but that's backed by a non-trivial amount of native memory.
        // The HTTP cache might be holding a large response.
        // Either way, keeping the number of items in the cache small has goodness, but it should be large enough
        // that common scenarios aren't doing rapid eviction and repopulation of the same value.
        // Until we see a reason that they need different sizes, use a const instead of a field.
        private const int MaxItems = 30;

        private protected readonly Lock _lock = new();

        private int _count = -1;
        private Node? _head;
        private Node? _expire;

        private protected virtual void Pruned(Node? prunedNode, int countStart, int countEnd)
        {
        }

        private protected virtual bool OnConflictTakeNew(Node current, T newValue)
        {
            return false;
        }

        private protected static int GetHashCode(string key) => key.GetHashCode();

        private protected bool TryGetNode(int hashCode, string key, [NotNullWhen(true)] out Node? value)
        {
            Debug.Assert(_lock.IsHeldByCurrentThread);

            Node? previous = null;
            Node? current = _head;

            while (current is not null)
            {
                if (current.MatchesKey(hashCode, key))
                {
                    // If we find the expire node, move expiration to after it, so that promoting it to
                    // most recent doesn't prune the whole list.
                    //
                    // This might, of course, make _expire null.
                    if (current == _expire)
                    {
                        _expire = current.Next;
                    }

                    // Move the found node to the head of the list, maintaining MRU ordering.
                    if (previous != null)
                    {
                        previous.Next = current.Next;
                        current.Next = _head;
                        _head = current;
                    }

                    value = current;
                    return true;
                }

                previous = current;
                current = current.Next;
            }

            value = null;
            return false;
        }

        private protected T AddOrUpdate(int hashCode, string key, T value, out Node? evicted, out T? replaced)
        {
            Debug.Assert(key is not null);
            Debug.Assert(value is not null);
            Debug.Assert(_lock.IsHeldByCurrentThread);

            T ret = value;

            // The first time we add something, create the object to monitor for GC events.
            if (_count < 0)
            {
                new GCWatcher(this);
                _count = 0;
            }

            if (TryGetNode(hashCode, key, out Node? current))
            {
                Debug.Assert(current is not null);

                if (OnConflictTakeNew(current, value))
                {
                    replaced = current.Value;
                    current.Value = value;
                }
                else
                {
                    replaced = null;
                    ret = current.Value;
                }

                evicted = null;
            }
            else
            {
                replaced = null;

                Node node = new Node(hashCode, key, value);
                node.Next = _head;

                if (_count < MaxItems)
                {
                    _count++;
                    evicted = null;
                }
                else
                {
                    // Because MaxItems is small, it's better to just iterate from head
                    // instead of using a doubly-linked list.

                    Node? previous = null;
                    Node? cur = _head;
                    Node? next = cur?.Next;

                    while (next is not null)
                    {
                        previous = cur;
                        cur = next;
                        next = cur.Next;
                    }

                    Debug.Assert(previous is not null);
                    Debug.Assert(cur is not null);

                    previous.Next = null;
                    evicted = cur;

                    if (cur == _expire)
                    {
                        _expire = null;
                    }
                }

                _head = node;
            }

            return ret;
        }

        private void PruneForGC()
        {
            // The general flow:
            // * The current head is where we expire next time.
            // * Under the lock: If there is an expire node, determine the new count by walking to it,
            //   and unlink it from the previous node.
            // * After the lock: Dispose all the values from the prune node onward.

            Node? prune;
            int countStart;
            int countEnd;

            lock (_lock)
            {
                prune = _expire;
                _expire = _head;
                countStart = _count;

                if (prune is null)
                {
                    return;
                }

                if (prune == _head)
                {
                    _count = 0;
                    _head = null;
                    _expire = null;
                }
                else
                {
                    Debug.Assert(_head is not null);
                    int count = 1;
                    Node current = _head;

                    while (current.Next != prune && current.Next is not null)
                    {
                        count++;
                        current = current.Next;
                    }

                    Debug.Assert(current.Next == prune, "The prune node should be in the list");
                    current.Next = null;
                    _count = count;
                }

                countEnd = _count;
            }

            Pruned(prune, countStart, countEnd);
        }

        private protected sealed class Node
        {
            private readonly int _keyHashCode;

            internal string Key { get; }
            internal T Value { get; set; }
            internal Node? Next { get; set; }

            internal Node(int hashCode, string key, T value)
            {
                Debug.Assert(key.GetHashCode() == hashCode);

                Key = key;
                _keyHashCode = hashCode;
                Value = value;
            }

            internal bool MatchesKey(int hashCode, string key)
            {
                return _keyHashCode == hashCode && Key.Equals(key, StringComparison.Ordinal);
            }
        }

        private sealed class GCWatcher
        {
            private readonly X509MruCache<T> _owner;

            internal GCWatcher(X509MruCache<T> owner)
            {
                _owner = owner;
            }

            ~GCWatcher()
            {
                GC.ReRegisterForFinalize(this);

                if (GC.GetGeneration(this) == GC.MaxGeneration)
                {
                    try
                    {
                        _owner.PruneForGC();
                    }
                    catch
                    {
                        // Eat any exception so we don't terminate the finalizer thread.
#if DEBUG
                        // Except in DEBUG, as we really shouldn't be hitting any exceptions here.
                        throw;
#endif
                    }
                }
            }
        }
    }
}
