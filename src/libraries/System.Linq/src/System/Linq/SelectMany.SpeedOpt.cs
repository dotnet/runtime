// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    /// <summary>Provides a set of <see langword="static" /> (<see langword="Shared" /> in Visual Basic) methods for querying objects that implement <see cref="IEnumerable{T}" />.</summary>
    /// <remarks>The methods in this class provide an implementation of the standard query operators for querying data sources that implement <see cref="IEnumerable{T}" />. The standard query operators are general purpose methods that follow the LINQ pattern and enable you to express traversal, filter, and projection operations over data in any .NET-based programming language.
    /// The majority of the methods in this class are defined as extension methods that extend <see cref="IEnumerable{T}" />. This means they can be called like an instance method on any object that implements <see cref="IEnumerable{T}" />.
    /// Methods that are used in a query that returns a sequence of values do not consume the target data until the query object is enumerated. This is known as deferred execution. Methods that are used in a query that returns a singleton value execute and consume the target data immediately.</remarks>
    /// <related type="Article" href="https://msdn.microsoft.com/library/24cda21e-8af8-4632-b519-c404a839b9b2">Standard Query Operators Overview</related>
    /// <related type="Article" href="/dotnet/csharp/programming-guide/classes-and-structs/extension-methods">Extension Methods (C# Programming Guide)</related>
    /// <related type="Article" href="/dotnet/visual-basic/programming-guide/language-features/procedures/extension-methods">Extension Methods (Visual Basic)</related>
    public static partial class Enumerable
    {
        private sealed partial class SelectManySingleSelectorIterator<TSource, TResult> : IIListProvider<TResult>
        {
            public int GetCount(bool onlyIfCheap)
            {
                if (onlyIfCheap)
                {
                    return -1;
                }

                int count = 0;

                foreach (TSource element in _source)
                {
                    checked
                    {
                        count += _selector(element).Count();
                    }
                }

                return count;
            }

            public TResult[] ToArray()
            {
                var builder = new SparseArrayBuilder<TResult>(initialize: true);
                ArrayBuilder<IEnumerable<TResult>> deferredCopies = default;

                foreach (TSource element in _source)
                {
                    IEnumerable<TResult> enumerable = _selector(element);

                    if (builder.ReserveOrAdd(enumerable))
                    {
                        deferredCopies.Add(enumerable);
                    }
                }

                TResult[] array = builder.ToArray();

                ArrayBuilder<Marker> markers = builder.Markers;
                for (int i = 0; i < markers.Count; i++)
                {
                    Marker marker = markers[i];
                    IEnumerable<TResult> enumerable = deferredCopies[i];
                    EnumerableHelpers.Copy(enumerable, array, marker.Index, marker.Count);
                }

                return array;
            }

            public List<TResult> ToList()
            {
                var list = new List<TResult>();

                foreach (TSource element in _source)
                {
                    list.AddRange(_selector(element));
                }

                return list;
            }
        }
    }
}
