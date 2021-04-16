// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static System.Linq.Utilities;

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
        /// <summary>Projects each element of a sequence into a new form.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector" />.</typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> whose elements are the result of invoking the transform function on each element of <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// This projection method requires the transform function, <paramref name="selector" />, to produce one value for each value in the source sequence, <paramref name="source" />. If <paramref name="selector" /> returns a value that is itself a collection, it is up to the consumer to traverse the subsequences manually. In such a situation, it might be better for your query to return a single coalesced sequence of values. To achieve this, use the <see cref="O:Enumerable.SelectMany" /> method instead of <see cref="O:Enumerable.Select" />. Although `SelectMany` works similarly to `Select`, it differs in that the transform function returns a collection that is then expanded by `SelectMany` before it is returned.
        /// In query expression syntax, a `select` (Visual C#) or `Select` (Visual Basic) clause translates to an invocation of <see cref="O:Enumerable.Select" />.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="Select{T1,T2}(IEnumerable{T1},Func{T1,T2})" /> to project over a sequence of values.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet75":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet75":::</example>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/select-clause">select clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/select-clause">Select Clause (Visual Basic)</related>
        public static IEnumerable<TResult> Select<TSource, TResult>(
            this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            if (source is Iterator<TSource> iterator)
            {
                return iterator.Select(selector);
            }

            if (source is IList<TSource> ilist)
            {
                if (source is TSource[] array)
                {
                    return array.Length == 0 ?
                        Empty<TResult>() :
                        new SelectArrayIterator<TSource, TResult>(array, selector);
                }

                if (source is List<TSource> list)
                {
                    return new SelectListIterator<TSource, TResult>(list, selector);
                }

                return new SelectIListIterator<TSource, TResult>(ilist, selector);
            }

            if (source is IPartition<TSource> partition)
            {
                IEnumerable<TResult>? result = null;
                CreateSelectIPartitionIterator(selector, partition, ref result);
                if (result != null)
                {
                    return result;
                }
            }

            return new SelectEnumerableIterator<TSource, TResult>(source, selector);
        }

        static partial void CreateSelectIPartitionIterator<TResult, TSource>(
            Func<TSource, TResult> selector, IPartition<TSource> partition, [NotNull] ref IEnumerable<TResult>? result);

        /// <summary>Projects each element of a sequence into a new form by incorporating the element's index.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector" />.</typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each source element; the second parameter of the function represents the index of the source element.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> whose elements are the result of invoking the transform function on each element of <paramref name="source" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.
        /// The first argument to <paramref name="selector" /> represents the element to process. The second argument to <paramref name="selector" /> represents the zero-based index of that element in the source sequence. This can be useful if the elements are in a known order and you want to do something with an element at a particular index, for example. It can also be useful if you want to retrieve the index of one or more elements.
        /// This projection method requires the transform function, <paramref name="selector" />, to produce one value for each value in the source sequence, <paramref name="source" />. If <paramref name="selector" /> returns a value that is itself a collection, it is up to the consumer to traverse the subsequences manually. In such a situation, it might be better for your query to return a single coalesced sequence of values. To achieve this, use the <see cref="O:Enumerable.SelectMany" /> method instead of <see cref="O:Enumerable.Select" />. Although `SelectMany` works similarly to `Select`, it differs in that the transform function returns a collection that is then expanded by `SelectMany` before it is returned.</remarks>
        /// <example>The following code example demonstrates how to use <see cref="Select{T1,T2}(IEnumerable{T1},Func{T1,int,T2})" /> to project over a sequence of values and use the index of each element.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet76":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet76":::</example>
        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            return SelectIterator(source, selector);
        }

        private static IEnumerable<TResult> SelectIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked
                {
                    index++;
                }

                yield return selector(element, index);
            }
        }

        /// <summary>
        /// An iterator that maps each item of an <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        private sealed partial class SelectEnumerableIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private IEnumerator<TSource>? _enumerator;

            public SelectEnumerableIterator(IEnumerable<TSource> source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                _source = source;
                _selector = selector;
            }

            public override Iterator<TResult> Clone() =>
                new SelectEnumerableIterator<TSource, TResult>(_source, _selector);

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
                        if (_enumerator.MoveNext())
                        {
                            _current = _selector(_enumerator.Current);
                            return true;
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new SelectEnumerableIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
        }

        /// <summary>
        /// An iterator that maps each item of a array.
        /// </summary>
        /// <typeparam name="TSource">The type of the source array.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        [DebuggerDisplay("Count = {CountForDebugger}")]
        private sealed partial class SelectArrayIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly TSource[] _source;
            private readonly Func<TSource, TResult> _selector;

            public SelectArrayIterator(TSource[] source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                Debug.Assert(source.Length > 0); // Caller should check this beforehand and return a cached result
                _source = source;
                _selector = selector;
            }

            private int CountForDebugger => _source.Length;

            public override Iterator<TResult> Clone() => new SelectArrayIterator<TSource, TResult>(_source, _selector);

            public override bool MoveNext()
            {
                if (_state < 1 | _state == _source.Length + 1)
                {
                    Dispose();
                    return false;
                }

                int index = _state++ - 1;
                _current = _selector(_source[index]);
                return true;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new SelectArrayIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
        }

        /// <summary>
        /// An iterator that maps each item of a <see cref="List{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source list.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        [DebuggerDisplay("Count = {CountForDebugger}")]
        private sealed partial class SelectListIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly List<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private List<TSource>.Enumerator _enumerator;

            public SelectListIterator(List<TSource> source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                _source = source;
                _selector = selector;
            }

            private int CountForDebugger => _source.Count;

            public override Iterator<TResult> Clone() => new SelectListIterator<TSource, TResult>(_source, _selector);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        if (_enumerator.MoveNext())
                        {
                            _current = _selector(_enumerator.Current);
                            return true;
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new SelectListIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
        }

        /// <summary>
        /// An iterator that maps each item of an <see cref="IList{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source list.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        [DebuggerDisplay("Count = {CountForDebugger}")]
        private sealed partial class SelectIListIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly IList<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private IEnumerator<TSource>? _enumerator;

            public SelectIListIterator(IList<TSource> source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                _source = source;
                _selector = selector;
            }

            private int CountForDebugger => _source.Count;

            public override Iterator<TResult> Clone() => new SelectIListIterator<TSource, TResult>(_source, _selector);

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
                        if (_enumerator.MoveNext())
                        {
                            _current = _selector(_enumerator.Current);
                            return true;
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new SelectIListIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
        }
    }
}
