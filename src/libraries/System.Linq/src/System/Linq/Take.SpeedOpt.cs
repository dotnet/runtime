// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

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
        private static IEnumerable<TSource> TakeIterator<TSource>(IEnumerable<TSource> source, int count)
        {
            Debug.Assert(source != null);
            Debug.Assert(count > 0);

            return
                source is IPartition<TSource> partition ? partition.Take(count) :
                source is IList<TSource> sourceList ? new ListPartition<TSource>(sourceList, 0, count - 1) :
                new EnumerablePartition<TSource>(source, 0, count - 1);
        }

        private static IEnumerable<TSource> TakeRangeIterator<TSource>(IEnumerable<TSource> source, int startIndex, int endIndex)
        {
            Debug.Assert(source != null);
            Debug.Assert(startIndex >= 0 && startIndex < endIndex);

            return
                source is IPartition<TSource> partition ? TakePartitionRange(partition, startIndex, endIndex) :
                source is IList<TSource> sourceList ? new ListPartition<TSource>(sourceList, startIndex, endIndex - 1) :
                new EnumerablePartition<TSource>(source, startIndex, endIndex - 1);

            static IPartition<TSource> TakePartitionRange(IPartition<TSource> partition, int startIndex, int endIndex)
            {
                partition = endIndex == 0 ? EmptyPartition<TSource>.Instance : partition.Take(endIndex);
                return startIndex == 0 ? partition : partition.Skip(startIndex);
            }
        }
    }
}
