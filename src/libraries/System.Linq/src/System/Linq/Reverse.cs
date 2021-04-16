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
        /// <summary>Inverts the order of the elements in a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values to reverse.</param>
        /// <returns>A sequence whose elements correspond to those of the input sequence in reverse order.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// Unlike <see cref="O:Enumerable.OrderBy" />, this sorting method does not consider the actual values themselves in determining the order. Rather, it just returns the elements in the reverse order from which they are produced by the underlying source.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="O:Enumerable.Reverse" /> to reverse the order of elements in an array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet74":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet74":::</example>
        public static IEnumerable<TSource> Reverse<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return new ReverseIterator<TSource>(source);
        }

        /// <summary>
        /// An iterator that yields the items of an <see cref="IEnumerable{TSource}"/> in reverse.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        private sealed partial class ReverseIterator<TSource> : Iterator<TSource>
        {
            private readonly IEnumerable<TSource> _source;
            private TSource[]? _buffer;

            public ReverseIterator(IEnumerable<TSource> source)
            {
                Debug.Assert(source != null);
                _source = source;
            }

            public override Iterator<TSource> Clone() => new ReverseIterator<TSource>(_source);

            public override bool MoveNext()
            {
                if (_state - 2 <= -2)
                {
                    // Either someone called a method and cast us to IEnumerable without calling GetEnumerator,
                    // or we were already disposed. In either case, iteration has ended, so return false.
                    // A comparison is made against -2 instead of _state <= 0 because we want to handle cases where
                    // the source is really large and adding the bias causes _state to overflow.
                    Debug.Assert(_state == -1 || _state == 0);
                    Dispose();
                    return false;
                }

                switch (_state)
                {
                    case 1:
                        // Iteration has just started. Capture the source into an array and set _state to 2 + the count.
                        // Having an extra field for the count would be more readable, but we save it into _state with a
                        // bias instead to minimize field size of the iterator.
                        Buffer<TSource> buffer = new Buffer<TSource>(_source);
                        _buffer = buffer._items;
                        _state = buffer._count + 2;
                        goto default;
                    default:
                        // At this stage, _state starts from 2 + the count. _state - 3 represents the current index into the
                        // buffer. It is continuously decremented until it hits 2, which means that we've run out of items to
                        // yield and should return false.
                        int index = _state - 3;
                        if (index != -1)
                        {
                            Debug.Assert(_buffer != null);
                            _current = _buffer[index];
                            --_state;
                            return true;
                        }

                        break;
                }

                Dispose();
                return false;
            }

            public override void Dispose()
            {
                _buffer = null; // Just in case this ends up being long-lived, allow the memory to be reclaimed.
                base.Dispose();
            }
        }
    }
}
