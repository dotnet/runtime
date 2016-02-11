// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> Repeat<TResult>(TResult element, int count)
        {
            if (count < 0) throw Error.ArgumentOutOfRange("count");
            if (count == 0) return new EmptyPartition<TResult>();
            return new RepeatIterator<TResult>(element, count);
        }

        private sealed class RepeatIterator<TResult> : Iterator<TResult>, IArrayProvider<TResult>, IListProvider<TResult>, IPartition<TResult>
        {
            private readonly int _count;
            private int _sent;

            public RepeatIterator(TResult element, int count)
            {
                Debug.Assert(count > 0);
                current = element;
                _count = count;
            }

            public override Iterator<TResult> Clone()
            {
                return new RepeatIterator<TResult>(current, _count);
            }

            public override void Dispose()
            {
                // Don't let base Dispose wipe current.
                state = -1;
            }

            public override bool MoveNext()
            {
                if (state == 1 & _sent != _count)
                {
                    ++_sent;
                    return true;
                }
                state = -1;
                return false;
            }

            public TResult[] ToArray()
            {
                TResult[] array = new TResult[_count];
                if (current != null)
                {
                    for (int i = 0; i != array.Length; ++i) array[i] = current;
                }

                return array;
            }

            public List<TResult> ToList()
            {
                List<TResult> list = new List<TResult>(_count);
                for (int i = 0; i != _count; ++i) list.Add(current);

                return list;
            }

            public IPartition<TResult> Skip(int count)
            {
                if (count >= _count) return new EmptyPartition<TResult>();
                return new RepeatIterator<TResult>(current, _count - count);
            }

            public IPartition<TResult> Take(int count)
            {
                if (count > _count) count = _count;
                return new RepeatIterator<TResult>(current, count);
            }

            public TResult ElementAt(int index)
            {
                if ((uint)index >= (uint)_count) throw Error.ArgumentOutOfRange("index");
                return current;
            }

            public TResult ElementAtOrDefault(int index)
            {
                return (uint)index >= (uint)_count ? default(TResult) : current;
            }

            public TResult First()
            {
                return current;
            }

            public TResult FirstOrDefault()
            {
                return current;
            }

            public TResult Last()
            {
                return current;
            }

            public TResult LastOrDefault()
            {
                return current;
            }
        }
    }
}
