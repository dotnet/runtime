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
        /// <summary>Generates a sequence that contains one repeated value.</summary>
        /// <typeparam name="TResult">The type of the value to be repeated in the result sequence.</typeparam>
        /// <param name="element">The value to be repeated.</param>
        /// <param name="count">The number of times to repeat the value in the generated sequence.</param>
        /// <returns>An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains a repeated value.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="count" /> is less than 0.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="O:System.Linq.Enumerable.Repeat" /> to generate a sequence of a repeated value.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet73":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet73":::</example>
        public static IEnumerable<TResult> Repeat<TResult>(TResult element, int count)
        {
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            }

            if (count == 0)
            {
                return Empty<TResult>();
            }

            return new RepeatIterator<TResult>(element, count);
        }

        /// <summary>
        /// An iterator that yields the same item multiple times.
        /// </summary>
        /// <typeparam name="TResult">The type of the item.</typeparam>
        [DebuggerDisplay("Count = {_count}")]
        private sealed partial class RepeatIterator<TResult> : Iterator<TResult>
        {
            private readonly int _count;

            public RepeatIterator(TResult element, int count)
            {
                Debug.Assert(count > 0);
                _current = element;
                _count = count;
            }

            public override Iterator<TResult> Clone()
            {
                return new RepeatIterator<TResult>(_current, _count);
            }

            public override void Dispose()
            {
                // Don't let base.Dispose wipe Current.
                _state = -1;
            }

            public override bool MoveNext()
            {
                // Having a separate field for the number of sent items would be more readable.
                // However, we save it into _state with a bias to minimize field size of the iterator.
                int sent = _state - 1;

                // We can't have sent a negative number of items, obviously. However, if this iterator
                // was illegally casted to IEnumerator without GetEnumerator being called, or if we've
                // already been disposed, then `sent` will be negative.
                if (sent >= 0 && sent != _count)
                {
                    ++_state;
                    return true;
                }

                Dispose();
                return false;
            }
        }
    }
}
