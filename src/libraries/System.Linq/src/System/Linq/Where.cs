// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using static System.Linq.Utilities;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>Filters a sequence of values based on a predicate.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to filter.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>In query expression syntax, a `where` (Visual C#) or `Where` (Visual Basic) clause translates to an invocation of <see cref="Where{T}(IEnumerable{T},Func{T,bool})" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="Where{T}(IEnumerable{T},Func{T,bool})" /> to filter a sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet110":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet110":::</example>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/where-clause">where clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/where-clause">Where Clause (Visual Basic)</related>
        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            if (source is Iterator<TSource> iterator)
            {
                return iterator.Where(predicate);
            }

            if (source is TSource[] array)
            {
                return array.Length == 0 ?
                    Empty<TSource>() :
                    new WhereArrayIterator<TSource>(array, predicate);
            }

            if (source is List<TSource> list)
            {
                return new WhereListIterator<TSource>(list, predicate);
            }

            return new WhereEnumerableIterator<TSource>(source, predicate);
        }

        /// <summary>Filters a sequence of values based on a predicate. Each element's index is used in the logic of the predicate function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}" /> to filter.</param>
        /// <param name="predicate">A function to test each source element for a condition; the second parameter of the function represents the index of the source element.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>The first argument of <paramref name="predicate" /> represents the element to test. The second argument represents the zero-based index of the element within <paramref name="source" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="Where{T}(IEnumerable{T},Func{T,int,bool})" /> to filter a sequence based on a predicate that involves the index of each element.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet111":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet111":::</example>
        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            return WhereIterator(source, predicate);
        }

        private static IEnumerable<TSource> WhereIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked
                {
                    index++;
                }

                if (predicate(element, index))
                {
                    yield return element;
                }
            }
        }

        /// <summary>
        /// An iterator that filters each item of an <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        private sealed partial class WhereEnumerableIterator<TSource> : Iterator<TSource>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, bool> _predicate;
            private IEnumerator<TSource>? _enumerator;

            public WhereEnumerableIterator(IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                Debug.Assert(source != null);
                Debug.Assert(predicate != null);
                _source = source;
                _predicate = predicate;
            }

            public override Iterator<TSource> Clone() => new WhereEnumerableIterator<TSource>(_source, _predicate);

            public override void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        Debug.Assert(_enumerator != null);
                        while (_enumerator.MoveNext())
                        {
                            TSource item = _enumerator.Current;
                            if (_predicate(item))
                            {
                                _current = item;
                                return true;
                            }
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector) =>
                new WhereSelectEnumerableIterator<TSource, TResult>(_source, _predicate, selector);

            public override IEnumerable<TSource> Where(Func<TSource, bool> predicate) =>
                new WhereEnumerableIterator<TSource>(_source, CombinePredicates(_predicate, predicate));
        }

        /// <summary>
        /// An iterator that filters each item of an array.
        /// </summary>
        /// <typeparam name="TSource">The type of the source array.</typeparam>
        internal sealed partial class WhereArrayIterator<TSource> : Iterator<TSource>
        {
            private readonly TSource[] _source;
            private readonly Func<TSource, bool> _predicate;

            public WhereArrayIterator(TSource[] source, Func<TSource, bool> predicate)
            {
                Debug.Assert(source != null && source.Length > 0);
                Debug.Assert(predicate != null);
                _source = source;
                _predicate = predicate;
            }

            public override Iterator<TSource> Clone() =>
                new WhereArrayIterator<TSource>(_source, _predicate);

            public override bool MoveNext()
            {
                int index = _state - 1;
                TSource[] source = _source;

                while (unchecked((uint)index < (uint)source.Length))
                {
                    TSource item = source[index];
                    index = _state++;
                    if (_predicate(item))
                    {
                        _current = item;
                        return true;
                    }
                }

                Dispose();
                return false;
            }

            public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector) =>
                new WhereSelectArrayIterator<TSource, TResult>(_source, _predicate, selector);

            public override IEnumerable<TSource> Where(Func<TSource, bool> predicate) =>
                new WhereArrayIterator<TSource>(_source, CombinePredicates(_predicate, predicate));
        }

        /// <summary>
        /// An iterator that filters each item of a <see cref="List{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source list.</typeparam>
        private sealed partial class WhereListIterator<TSource> : Iterator<TSource>
        {
            private readonly List<TSource> _source;
            private readonly Func<TSource, bool> _predicate;
            private List<TSource>.Enumerator _enumerator;

            public WhereListIterator(List<TSource> source, Func<TSource, bool> predicate)
            {
                Debug.Assert(source != null);
                Debug.Assert(predicate != null);
                _source = source;
                _predicate = predicate;
            }

            public override Iterator<TSource> Clone() =>
                new WhereListIterator<TSource>(_source, _predicate);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        while (_enumerator.MoveNext())
                        {
                            TSource item = _enumerator.Current;
                            if (_predicate(item))
                            {
                                _current = item;
                                return true;
                            }
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector) =>
                new WhereSelectListIterator<TSource, TResult>(_source, _predicate, selector);

            public override IEnumerable<TSource> Where(Func<TSource, bool> predicate) =>
                new WhereListIterator<TSource>(_source, CombinePredicates(_predicate, predicate));
        }

        /// <summary>
        /// An iterator that filters, then maps, each item of an array.
        /// </summary>
        /// <typeparam name="TSource">The type of the source array.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        private sealed partial class WhereSelectArrayIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly TSource[] _source;
            private readonly Func<TSource, bool> _predicate;
            private readonly Func<TSource, TResult> _selector;

            public WhereSelectArrayIterator(TSource[] source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null && source.Length > 0);
                Debug.Assert(predicate != null);
                Debug.Assert(selector != null);
                _source = source;
                _predicate = predicate;
                _selector = selector;
            }

            public override Iterator<TResult> Clone() =>
                new WhereSelectArrayIterator<TSource, TResult>(_source, _predicate, _selector);

            public override bool MoveNext()
            {
                int index = _state - 1;
                TSource[] source = _source;

                while (unchecked((uint)index < (uint)source.Length))
                {
                    TSource item = source[index];
                    index = _state++;
                    if (_predicate(item))
                    {
                        _current = _selector(item);
                        return true;
                    }
                }

                Dispose();
                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new WhereSelectArrayIterator<TSource, TResult2>(_source, _predicate, CombineSelectors(_selector, selector));
        }

        /// <summary>
        /// An iterator that filters, then maps, each item of a <see cref="List{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source list.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        private sealed partial class WhereSelectListIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly List<TSource> _source;
            private readonly Func<TSource, bool> _predicate;
            private readonly Func<TSource, TResult> _selector;
            private List<TSource>.Enumerator _enumerator;

            public WhereSelectListIterator(List<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(predicate != null);
                Debug.Assert(selector != null);
                _source = source;
                _predicate = predicate;
                _selector = selector;
            }

            public override Iterator<TResult> Clone() =>
                new WhereSelectListIterator<TSource, TResult>(_source, _predicate, _selector);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        while (_enumerator.MoveNext())
                        {
                            TSource item = _enumerator.Current;
                            if (_predicate(item))
                            {
                                _current = _selector(item);
                                return true;
                            }
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new WhereSelectListIterator<TSource, TResult2>(_source, _predicate, CombineSelectors(_selector, selector));
        }

        /// <summary>
        /// An iterator that filters, then maps, each item of an <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        private sealed partial class WhereSelectEnumerableIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, bool> _predicate;
            private readonly Func<TSource, TResult> _selector;
            private IEnumerator<TSource>? _enumerator;

            public WhereSelectEnumerableIterator(IEnumerable<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(predicate != null);
                Debug.Assert(selector != null);
                _source = source;
                _predicate = predicate;
                _selector = selector;
            }

            public override Iterator<TResult> Clone() =>
                new WhereSelectEnumerableIterator<TSource, TResult>(_source, _predicate, _selector);

            public override void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        Debug.Assert(_enumerator != null);
                        while (_enumerator.MoveNext())
                        {
                            TSource item = _enumerator.Current;
                            if (_predicate(item))
                            {
                                _current = _selector(item);
                                return true;
                            }
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new WhereSelectEnumerableIterator<TSource, TResult2>(_source, _predicate, CombineSelectors(_selector, selector));
        }
    }
}
