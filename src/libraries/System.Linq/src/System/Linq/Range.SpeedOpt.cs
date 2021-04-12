// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    /// <summary>Provides a set of <see langword="static" /> (<see langword="Shared" /> in Visual Basic) methods for querying objects that implement <see cref="System.Collections.Generic.IEnumerable{T}" />.</summary>
    /// <remarks>The methods in this class provide an implementation of the standard query operators for querying data sources that implement <see cref="System.Collections.Generic.IEnumerable{T}" />. The standard query operators are general purpose methods that follow the LINQ pattern and enable you to express traversal, filter, and projection operations over data in any .NET-based programming language.
    /// The majority of the methods in this class are defined as extension methods that extend <see cref="System.Collections.Generic.IEnumerable{T}" />. This means they can be called like an instance method on any object that implements <see cref="System.Collections.Generic.IEnumerable{T}" />.
    /// Methods that are used in a query that returns a sequence of values do not consume the target data until the query object is enumerated. This is known as deferred execution. Methods that are used in a query that returns a singleton value execute and consume the target data immediately.</remarks>
    /// <related type="Article" href="https://msdn.microsoft.com/library/24cda21e-8af8-4632-b519-c404a839b9b2">Standard Query Operators Overview</related>
    /// <related type="Article" href="/dotnet/csharp/programming-guide/classes-and-structs/extension-methods">Extension Methods (C# Programming Guide)</related>
    /// <related type="Article" href="/dotnet/visual-basic/programming-guide/language-features/procedures/extension-methods">Extension Methods (Visual Basic)</related>
    public static partial class Enumerable
    {
        private sealed partial class RangeIterator : IPartition<int>
        {
            public override IEnumerable<TResult> Select<TResult>(Func<int, TResult> selector)
            {
                return new SelectRangeIterator<TResult>(_start, _end, selector);
            }

            public int[] ToArray()
            {
                int[] array = new int[_end - _start];
                int cur = _start;
                for (int i = 0; i != array.Length; ++i)
                {
                    array[i] = cur;
                    ++cur;
                }

                return array;
            }

            public List<int> ToList()
            {
                List<int> list = new List<int>(_end - _start);
                for (int cur = _start; cur != _end; cur++)
                {
                    list.Add(cur);
                }

                return list;
            }

            public int GetCount(bool onlyIfCheap) => unchecked(_end - _start);

            public IPartition<int> Skip(int count)
            {
                if (count >= _end - _start)
                {
                    return EmptyPartition<int>.Instance;
                }

                return new RangeIterator(_start + count, _end - _start - count);
            }

            public IPartition<int> Take(int count)
            {
                int curCount = _end - _start;
                if (count >= curCount)
                {
                    return this;
                }

                return new RangeIterator(_start, count);
            }

            public int TryGetElementAt(int index, out bool found)
            {
                if (unchecked((uint)index < (uint)(_end - _start)))
                {
                    found = true;
                    return _start + index;
                }

                found = false;
                return 0;
            }

            public int TryGetFirst(out bool found)
            {
                found = true;
                return _start;
            }

            public int TryGetLast(out bool found)
            {
                found = true;
                return _end - 1;
            }
        }
    }
}
