// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>Projects each element of a sequence to an <see cref="IEnumerable{T}" /> and flattens the resulting sequences into one sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by <paramref name="selector" />.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> whose elements are the result of invoking the one-to-many transform function on each element of the input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>The <see cref="SelectMany{T1,T2}(IEnumerable{T1},Func{T1,IEnumerable{T2}})" /> method enumerates the input sequence, uses a transform function to map each element to an <see cref="IEnumerable{T}" />, and then enumerates and yields the elements of each such <see cref="IEnumerable{T}" /> object. That is, for each element of <paramref name="source" />, <paramref name="selector" /> is invoked and a sequence of values is returned. <see cref="SelectMany{T1,T2}(IEnumerable{T1},Func{T1,IEnumerable{T2}})" /> then flattens this two-dimensional collection of collections into a one-dimensional <see cref="IEnumerable{T}" /> and returns it. For example, if a query uses <see cref="SelectMany{T1,T2}(IEnumerable{T1},Func{T1,IEnumerable{T2}})" /> to obtain the orders (of type `Order`) for each customer in a database, the result is of type `IEnumerable&lt;Order&gt;` in C# or `IEnumerable(Of Order)` in Visual Basic. If instead the query uses <see cref="O:Enumerable.Select" /> to obtain the orders, the collection of collections of orders is not combined and the result is of type `IEnumerable&lt;List&lt;Order&gt;&gt;` in C# or `IEnumerable(Of List(Of Order))` in Visual Basic.</para>
        /// <para>In query expression syntax, each `from` clause (Visual C#) or `From` clause (Visual Basic) after the initial one translates to an invocation of <see cref="O:Enumerable.SelectMany" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="SelectMany{T1,T2}(IEnumerable{T1},Func{T1,IEnumerable{T2}})" /> to perform a one-to-many projection over an array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet77":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet77":::</example>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/from-clause">from clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/from-clause">From Clause (Visual Basic)</related>
        public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            return new SelectManySingleSelectorIterator<TSource, TResult>(source, selector);
        }

        /// <summary>Projects each element of a sequence to an <see cref="IEnumerable{T}" />, and flattens the resulting sequences into one sequence. The index of each source element is used in the projected form of that element.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by <paramref name="selector" />.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A transform function to apply to each source element; the second parameter of the function represents the index of the source element.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> whose elements are the result of invoking the one-to-many transform function on each element of an input sequence.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>The <see cref="SelectMany{T1,T2}(IEnumerable{T1},Func{T1,int,IEnumerable{T2}})" /> method enumerates the input sequence, uses a transform function to map each element to an <see cref="IEnumerable{T}" />, and then enumerates and yields the elements of each such <see cref="IEnumerable{T}" /> object. That is, for each element of <paramref name="source" />, <paramref name="selector" /> is invoked and a sequence of values is returned. <see cref="SelectMany{T1,T2}(IEnumerable{T1},Func{T1,int,IEnumerable{T2}})" /> then flattens this two-dimensional collection of collections into a one-dimensional <see cref="IEnumerable{T}" /> and returns it. For example, if a query uses <see cref="SelectMany{T1,T2}(IEnumerable{T1},Func{T1,int,IEnumerable{T2}})" /> to obtain the orders (of type `Order`) for each customer in a database, the result is of type `IEnumerable&lt;Order&gt;` in C# or `IEnumerable(Of Order)` in Visual Basic. If instead the query uses <see cref="O:Enumerable.Select" /> to obtain the orders, the collection of collections of orders is not combined and the result is of type `IEnumerable&lt;List&lt;Order&gt;&gt;` in C# or `IEnumerable(Of List(Of Order))` in Visual Basic.</para>
        /// <para>The first argument to <paramref name="selector" /> represents the element to process. The second argument to <paramref name="selector" /> represents the zero-based index of that element in the source sequence. This can be useful if the elements are in a known order and you want to do something with an element at a particular index, for example. It can also be useful if you want to retrieve the index of one or more elements.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="SelectMany{T1,T2}(IEnumerable{T1},Func{T1,int,IEnumerable{T2}})" /> to perform a one-to-many projection over an array and use the index of each outer element.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet78":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet78":::</example>
        public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            return SelectManyIterator(source, selector);
        }

        private static IEnumerable<TResult> SelectManyIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked
                {
                    index++;
                }

                foreach (TResult subElement in selector(element, index))
                {
                    yield return subElement;
                }
            }
        }

        /// <summary>Projects each element of a sequence to an <see cref="IEnumerable{T}" />, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein. The index of each source element is used in the intermediate projected form of that element.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector" />.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="collectionSelector">A transform function to apply to each source element; the second parameter of the function represents the index of the source element.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> whose elements are the result of invoking the one-to-many transform function <paramref name="collectionSelector" /> on each element of <paramref name="source" /> and then mapping each of those sequence elements and their corresponding source element to a result element.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="collectionSelector" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>The <see cref="SelectMany{T1,T2,T3}(IEnumerable{T1},Func{T1,int,IEnumerable{T2}},Func{T1,T2,T3})" /> method is useful when you have to keep the elements of <paramref name="source" /> in scope for query logic that occurs after the call to <see cref="SelectMany{T1,T2,T3}(IEnumerable{T1},Func{T1,int,IEnumerable{T2}},Func{T1,T2,T3})" />. See the Example section for a code example. If there is a bidirectional relationship between objects of type <typeparamref name="TSource" /> and objects of type <typeparamref name="TCollection" />, that is, if an object of type <typeparamref name="TCollection" /> provides a property to retrieve the <typeparamref name="TSource" /> object that produced it, you do not need this overload of <see cref="SelectMany{T1,T2,T3}(IEnumerable{T1},Func{T1,int,IEnumerable{T2}},Func{T1,T2,T3})" />. Instead, you can use <see cref="SelectMany{T1,T2}(IEnumerable{T1},Func{T1,int,IEnumerable{T2}})" /> and navigate back to the <typeparamref name="TSource" /> object through the <typeparamref name="TCollection" /> object.</para>
        /// </remarks>
        public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (collectionSelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collectionSelector);
            }

            if (resultSelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
            }

            return SelectManyIterator(source, collectionSelector, resultSelector);
        }

        private static IEnumerable<TResult> SelectManyIterator<TSource, TCollection, TResult>(IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked
                {
                    index++;
                }

                foreach (TCollection subElement in collectionSelector(element, index))
                {
                    yield return resultSelector(element, subElement);
                }
            }
        }

        /// <summary>Projects each element of a sequence to an <see cref="IEnumerable{T}" />, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector" />.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="collectionSelector">A transform function to apply to each element of the input sequence.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> whose elements are the result of invoking the one-to-many transform function <paramref name="collectionSelector" /> on each element of <paramref name="source" /> and then mapping each of those sequence elements and their corresponding source element to a result element.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> or <paramref name="collectionSelector" /> or <paramref name="resultSelector" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>The <see cref="SelectMany{T1,T2,T3}(IEnumerable{T1},Func{T1,IEnumerable{T2}},Func{T1,T2,T3})" /> method is useful when you have to keep the elements of <paramref name="source" /> in scope for query logic that occurs after the call to <see cref="SelectMany{T1,T2,T3}(IEnumerable{T1},Func{T1,IEnumerable{T2}},Func{T1,T2,T3})" />. See the Example section for a code example. If there is a bidirectional relationship between objects of type <typeparamref name="TSource" /> and objects of type <typeparamref name="TCollection" />, that is, if an object of type <typeparamref name="TCollection" /> provides a property to retrieve the <typeparamref name="TSource" /> object that produced it, you do not need this overload of <see cref="SelectMany{T1,T2,T3}(IEnumerable{T1},Func{T1,IEnumerable{T2}},Func{T1,T2,T3})" />. Instead, you can use <see cref="SelectMany{T1,T2}(IEnumerable{T1},Func{T1,IEnumerable{T2}})" /> and navigate back to the <typeparamref name="TSource" /> object through the <typeparamref name="TCollection" /> object.</para>
        /// <para>In query expression syntax, each `from` clause (Visual C#) or `From` clause (Visual Basic) after the initial one translates to an invocation of <see cref="O:Enumerable.SelectMany" />.</para>
        /// </remarks>
        /// <example>The following code example demonstrates how to use <see cref="SelectMany{T1,T2,T3}(IEnumerable{T1},Func{T1,IEnumerable{T2}},Func{T1,T2,T3})" /> to perform a one-to-many projection over an array and use a result selector function to keep each corresponding element from the source sequence in scope for the final call to `Select`.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" id="Snippet124":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet124":::</example>
        /// <related type="Article" href="/dotnet/csharp/language-reference/keywords/from-clause">from clause (C# Reference)</related>
        /// <related type="Article" href="/dotnet/visual-basic/language-reference/queries/from-clause">From Clause (Visual Basic)</related>
        public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (collectionSelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collectionSelector);
            }

            if (resultSelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
            }

            return SelectManyIterator(source, collectionSelector, resultSelector);
        }

        private static IEnumerable<TResult> SelectManyIterator<TSource, TCollection, TResult>(IEnumerable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            foreach (TSource element in source)
            {
                foreach (TCollection subElement in collectionSelector(element))
                {
                    yield return resultSelector(element, subElement);
                }
            }
        }

        private sealed partial class SelectManySingleSelectorIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, IEnumerable<TResult>> _selector;
            private IEnumerator<TSource>? _sourceEnumerator;
            private IEnumerator<TResult>? _subEnumerator;

            internal SelectManySingleSelectorIterator(IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);

                _source = source;
                _selector = selector;
            }

            public override Iterator<TResult> Clone()
            {
                return new SelectManySingleSelectorIterator<TSource, TResult>(_source, _selector);
            }

            public override void Dispose()
            {
                if (_subEnumerator != null)
                {
                    _subEnumerator.Dispose();
                    _subEnumerator = null;
                }

                if (_sourceEnumerator != null)
                {
                    _sourceEnumerator.Dispose();
                    _sourceEnumerator = null;
                }

                base.Dispose();
            }

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        // Retrieve the source enumerator.
                        _sourceEnumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        // Take the next element from the source enumerator.
                        Debug.Assert(_sourceEnumerator != null);
                        if (!_sourceEnumerator.MoveNext())
                        {
                            break;
                        }

                        TSource element = _sourceEnumerator.Current;

                        // Project it into a sub-collection and get its enumerator.
                        _subEnumerator = _selector(element).GetEnumerator();
                        _state = 3;
                        goto case 3;
                    case 3:
                        // Take the next element from the sub-collection and yield.
                        Debug.Assert(_subEnumerator != null);
                        if (!_subEnumerator.MoveNext())
                        {
                            _subEnumerator.Dispose();
                            _subEnumerator = null;
                            _state = 2;
                            goto case 2;
                        }

                        _current = _subEnumerator.Current;
                        return true;
                }

                Dispose();
                return false;
            }
        }
    }
}
