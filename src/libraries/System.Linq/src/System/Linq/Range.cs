// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

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
        /// <summary>Generates a sequence of integral numbers within a specified range.</summary>
        /// <param name="start">The value of the first integer in the sequence.</param>
        /// <param name="count">The number of sequential integers to generate.</param>
        /// <returns>An <c>IEnumerable&lt;Int32&gt;</c> in C# or <c>IEnumerable(Of Int32)</c> in Visual Basic that contains a range of sequential integral numbers.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="count" /> is less than 0.
        /// -or-
        /// <paramref name="start" /> + <paramref name="count" /> -1 is larger than <see cref="int.MaxValue" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="O:System.Linq.Enumerable.Range" /> to generate a sequence of values.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet72":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet72":::</example>
        public static IEnumerable<int> Range(int start, int count)
        {
            long max = ((long)start) + count - 1;
            if (count < 0 || max > int.MaxValue)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            }

            if (count == 0)
            {
                return Empty<int>();
            }

            return new RangeIterator(start, count);
        }

        /// <summary>
        /// An iterator that yields a range of consecutive integers.
        /// </summary>
        [DebuggerDisplay("Count = {CountForDebugger}")]
        private sealed partial class RangeIterator : Iterator<int>
        {
            private readonly int _start;
            private readonly int _end;

            public RangeIterator(int start, int count)
            {
                Debug.Assert(count > 0);
                _start = start;
                _end = unchecked(start + count);
            }

            private int CountForDebugger => _end - _start;

            public override Iterator<int> Clone() => new RangeIterator(_start, _end - _start);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        Debug.Assert(_start != _end);
                        _current = _start;
                        _state = 2;
                        return true;
                    case 2:
                        if (unchecked(++_current) == _end)
                        {
                            break;
                        }

                        return true;
                }

                _state = -1;
                return false;
            }

            public override void Dispose()
            {
                _state = -1; // Don't reset current
            }
        }
    }
}
